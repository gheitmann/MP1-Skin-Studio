using System;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class ReviewedXmlChange
    {
        public readonly string RelativePath,Original,Edited,OriginalHash,EditedHash,EncodingName,SourceStatus;
        public readonly int OriginalBytes,EditedBytes,FirstDifference;
        public readonly bool SourceMatches;
        internal ReviewedXmlChange(SkinSource source)
        {
            RelativePath=source.RelativePath;Original=source.Original;Edited=source.Text;
            byte[] before=source.Preamble.Concat(source.Encoding.GetBytes(Original)).ToArray(),after=source.Bytes();
            OriginalBytes=before.Length;EditedBytes=after.Length;
            OriginalHash=ProjectFile.Digest(before);EditedHash=ProjectFile.Digest(after);
            EncodingName=source.Encoding.WebName+"; BOM "+source.Preamble.Length+" byte(s)";
            int offset=0;while(offset<Original.Length&&offset<Edited.Length&&Original[offset]==Edited[offset])offset++;
            FirstDifference=offset;
            try
            {
                if(!File.Exists(source.Path))SourceStatus="Missing source";
                else if(new FileInfo(source.Path).Length!=before.Length)SourceStatus="Source changed";
                else if(ProjectFile.Digest(File.ReadAllBytes(source.Path))!=OriginalHash)SourceStatus="Source changed";
                else{SourceMatches=true;SourceStatus="Matches loaded original";}
            }
            catch(IOException){SourceStatus="Source unavailable";}
            catch(UnauthorizedAccessException){SourceStatus="Source access denied";}
        }
        public string Details
        {
            get{return RelativePath+" | "+EncodingName+" | "+SourceStatus+Environment.NewLine+
                "Original: "+OriginalBytes+" bytes | SHA256 "+OriginalHash+Environment.NewLine+
                "Edited: "+EditedBytes+" bytes | SHA256 "+EditedHash+Environment.NewLine+
                "First difference: character "+(FirstDifference+1)+", original line "+ChangeReview.LineAt(Original,FirstDifference)+", edited line "+ChangeReview.LineAt(Edited,FirstDifference);}
        }
    }
    public static class ChangeReview
    {
        public static ReviewedXmlChange[] Capture(SkinProject project)
        {
            if(project==null)throw new ArgumentNullException("project");
            return project.Sources.Values.Where(s=>s.Changed).OrderBy(s=>s.RelativePath,StringComparer.OrdinalIgnoreCase).Select(s=>new ReviewedXmlChange(s)).ToArray();
        }
        public static int LineAt(string text,int offset)
        {
            int line=1;for(int i=0;i<Math.Min(offset,text.Length);i++)
                if(text[i]=='\r'||(text[i]=='\n'&&(i==0||text[i-1]!='\r')))line++;
            return line;
        }
        public static ReviewedXmlChange[] Filter(ReviewedXmlChange[] files,string query)
        {query=(query??"").Trim();return files.Where(f=>f.RelativePath.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();}
    }
}
