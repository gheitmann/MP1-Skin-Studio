using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class SavedSkinFile
    {
        public string RelativePath,Status;
        internal string OriginalHash,ExpectedHash,EditedHash,SourcePath;
        internal byte[] Bytes,Before;
        internal string OriginalText,EditedText,EncodingName;
        public bool Conflict,Changed;
        public override string ToString(){return RelativePath+" — "+Status;}
    }
    // A separate output copy keeps the editor/MP baseline intact, including Undo.
    // Only copies carrying our source-bound receipt can be updated in place.
    public sealed class SkinSavePlan
    {
        public const string ReceiptName=".mp1skinstudio-copy";
        sealed class Record {public string Path,Original,Saved;}
        readonly SkinProject source;
        readonly SkinCopyPlan copy;
        readonly string sourceRoot,markerHash,copyId;
        readonly Dictionary<string,Record> records=new Dictionary<string,Record>(StringComparer.OrdinalIgnoreCase);
        public readonly string Target;
        public readonly SavedSkinFile[] Files;
        public int ChangeCount {get{return Files.Count(f=>f.Changed);}}
        public bool HasConflicts {get{return Files.Any(f=>f.Conflict);}}
        static string Root(string path){return Path.GetFullPath(path).TrimEnd('\\','/');}
        static string Hash(byte[] bytes){return ProjectFile.Digest(bytes);}
        static string Original(SkinSource s){return Hash(s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray());}
        static void NoLinks(string path)
        {
            for(string current=path;!String.IsNullOrEmpty(current);current=Path.GetDirectoryName(current))
                if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked files or folders cannot be updated: "+current);
        }
        static string Inside(string root,string relative)
        {
            if(String.IsNullOrEmpty(relative)||relative.Length>500||Path.IsPathRooted(relative)||relative.Replace('\\','/').Split('/').Any(p=>p==".."||p.Length==0))throw new IOException("Invalid saved skin path.");
            string path=Path.GetFullPath(Path.Combine(root,relative));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Saved XML is outside its skin copy.");
            NoLinks(path);return path;
        }
        public static SkinSavePlan Capture(SkinProject project,string target=null){return new SkinSavePlan(project,target);}
        SkinSavePlan(SkinProject project,string target)
        {
            if(project==null)throw new ArgumentNullException("project");source=project;sourceRoot=Root(project.Root);copy=SkinCopyPlan.Capture(project,true);
            if(!String.IsNullOrWhiteSpace(target))
            {
                Target=Root(target);NoLinks(Target);
                if(Target.Equals(sourceRoot,StringComparison.OrdinalIgnoreCase)||Target.StartsWith(sourceRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||sourceRoot.StartsWith(Target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Save to a separate output copy, outside the source skin.");
                string marker=Inside(Target,ReceiptName);if(!File.Exists(marker)||new FileInfo(marker).Length>2*1024*1024)throw new IOException("Choose a skin created by Save skin copy. Existing unrelated skins cannot be overwritten.");
                byte[] data=File.ReadAllBytes(marker);markerHash=Hash(data);
                var doc=new XmlDocument{XmlResolver=null};using(var input=new MemoryStream(data))using(var reader=XmlReader.Create(input,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024}))doc.Load(reader);
                var root=doc.DocumentElement;Guid id;
                if(root==null||root.Name!="skinCopy"||root.GetAttribute("version")!="1"||!Guid.TryParse(root.GetAttribute("id"),out id)||!String.Equals(Root(root.GetAttribute("source")),sourceRoot,StringComparison.OrdinalIgnoreCase))throw new IOException("This saved copy belongs to a different source skin.");
                copyId=id.ToString("N");if(root.ChildNodes.OfType<XmlElement>().Count()>4096)throw new IOException("Too many saved XML records.");
                foreach(XmlElement node in root.ChildNodes.OfType<XmlElement>())
                {
                    string path=node.GetAttribute("path");Inside(Target,path);
                    if(node.Name!="file"||!path.EndsWith(".xml",StringComparison.OrdinalIgnoreCase)||records.ContainsKey(path)||!Digest(node.GetAttribute("original"))||!Digest(node.GetAttribute("saved")))throw new IOException("Invalid saved copy receipt.");
                    records.Add(path,new Record{Path=path,Original=node.GetAttribute("original"),Saved=node.GetAttribute("saved")});
                }
            }
            else copyId=Guid.NewGuid().ToString("N");
            Files=project.Sources.Values.Where(s=>s.Changed||records.ContainsKey(s.RelativePath)).OrderBy(s=>s.RelativePath,StringComparer.OrdinalIgnoreCase).Select(s=>
            {
                if(!s.RelativePath.EndsWith(".xml",StringComparison.OrdinalIgnoreCase))throw new IOException("Only skin XML can be saved.");
                Inside(sourceRoot,s.RelativePath);var file=new SavedSkinFile{RelativePath=s.RelativePath,OriginalHash=Original(s),Bytes=s.Bytes(),Before=s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray(),SourcePath=s.Path,EditedText=s.Text,EncodingName=s.Encoding.WebName};file.EditedHash=Hash(file.Bytes);
                Record prior;bool known=records.TryGetValue(s.RelativePath,out prior);file.ExpectedHash=known?prior.Saved:file.OriginalHash;
                if(known&&file.OriginalHash!=prior.Original){file.Conflict=true;file.Status="Source baseline differs from this copy";}
                else if(Target!=null)
                {
                    try{string path=Inside(Target,s.RelativePath);if(!File.Exists(path)||new FileInfo(path).Length>8*1024*1024){file.Conflict=true;file.Status="Target missing or exceeds size limit";}else{file.Before=File.ReadAllBytes(path);if(Hash(file.Before)!=file.ExpectedHash){file.Conflict=true;file.Status="Target changed outside Studio";}else if((File.GetAttributes(path)&FileAttributes.ReadOnly)!=0){file.Conflict=true;file.Status="Target is read-only";}}}
                    catch(IOException){file.Conflict=true;file.Status="Target unavailable";}catch(UnauthorizedAccessException){file.Conflict=true;file.Status="Target access denied";}
                }
                using(var input=new MemoryStream(file.Before))using(var reader=new StreamReader(input,s.Encoding,true))file.OriginalText=reader.ReadToEnd();
                file.Changed=file.EditedHash!=file.ExpectedHash;if(!file.Conflict)file.Status=file.Changed?"Ready to save":"Already saved";return file;
            }).ToArray();
            if(Files.Length>4096||Files.Any(f=>f.Bytes.Length>8*1024*1024)||Files.Sum(f=>(long)f.Bytes.Length)>64*1024*1024)throw new IOException("The saved XML set exceeds its size limit.");
        }
        static bool Digest(string text){return text.Length==64&&text.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');}
        public ReviewedXmlChange[] Review(){return Files.Where(f=>f.Changed||f.Conflict).Select(f=>new ReviewedXmlChange(f.RelativePath,f.OriginalText,f.EditedText,f.Before,f.Bytes,f.EncodingName,!f.Conflict,f.Status)).ToArray();}
        byte[] Receipt()
        {
            var all=records.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.OrdinalIgnoreCase);
            foreach(var file in Files)all[file.RelativePath]=new Record{Path=file.RelativePath,Original=file.OriginalHash,Saved=file.EditedHash};
            var doc=new XmlDocument{XmlResolver=null};var root=doc.CreateElement("skinCopy");doc.AppendChild(root);root.SetAttribute("version","1");root.SetAttribute("id",copyId);root.SetAttribute("source",sourceRoot);
            foreach(var record in all.Values.OrderBy(r=>r.Path,StringComparer.OrdinalIgnoreCase)){var n=doc.CreateElement("file");n.SetAttribute("path",record.Path);n.SetAttribute("original",record.Original);n.SetAttribute("saved",record.Saved);root.AppendChild(n);}
            return new UTF8Encoding(false).GetBytes(doc.OuterXml);
        }
        public string CreateCopy(string parent,string name,CancellationToken cancellation)
        {
            if(Target!=null)throw new InvalidOperationException("Capture a new-copy plan first.");
            string target=copy.CopyTo(parent,name,cancellation);
            // An inherited receipt is replaced only in this newly created copy.
            try{string marker=Inside(Root(target),ReceiptName);File.WriteAllBytes(marker,Receipt());return target;}
            catch(Exception ex){throw new IOException("The new skin exists at "+target+", but its save receipt could not be completed. The source is unchanged.",ex);}
        }
        sealed class Write {public string Path,Temp,Rollback,Expected,After;public byte[] Before,Bytes;public bool Replaced;}
        void CheckMarker(){if(Hash(File.ReadAllBytes(Inside(Target,ReceiptName)))!=markerHash)throw new IOException("This saved copy was updated by another operation. Refresh the review.");}
        public string Save()
        {
            if(Target==null)throw new InvalidOperationException("Create an output copy first.");
            if(HasConflicts)throw new IOException("Resolve the target conflicts or create a new copy. No XML was saved.");
            if(ChangeCount==0)return null;
            string lockPath=Inside(Target,".mp1skinstudio-copy.lock");
            using(var gate=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                CheckMarker();string transaction=Guid.NewGuid().ToString("N");var writes=new List<Write>();string backup=null;bool complete=false;
                try
                {
                    foreach(var file in Files.Where(f=>f.Changed))
                    {
                        string path=Inside(Target,file.RelativePath);byte[] before=File.ReadAllBytes(path);
                        if(Hash(before)!=file.ExpectedHash)throw new IOException("Target changed after review: "+file.RelativePath);
                        if((File.GetAttributes(path)&FileAttributes.ReadOnly)!=0)throw new IOException("Read-only target: "+file.RelativePath);
                        writes.Add(new Write{Path=path,Before=before,Bytes=file.Bytes,Expected=file.ExpectedHash,After=file.EditedHash});
                    }
                    string marker=Inside(Target,ReceiptName);writes.Add(new Write{Path=marker,Before=File.ReadAllBytes(marker),Bytes=Receipt(),Expected=markerHash});
                    foreach(var write in writes){write.After=Hash(write.Bytes);write.Temp=write.Path+"."+transaction+".tmp";write.Rollback=write.Path+"."+transaction+".rollback";using(var stream=new FileStream(write.Temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(write.Bytes,0,write.Bytes.Length);stream.Flush(true);}}
                    string backups=Inside(Target,".MP1SkinStudio/backups");Directory.CreateDirectory(backups);backup=Inside(Target,".MP1SkinStudio/backups/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+transaction+".zip");
                    using(var archive=ZipFile.Open(backup,ZipArchiveMode.Create))foreach(var write in writes){var entry=archive.CreateEntry(write.Path.Substring(Target.Length+1).Replace('\\','/'));using(var output=entry.Open())output.Write(write.Before,0,write.Before.Length);}
                    foreach(var write in writes)
                    {
                        NoLinks(write.Path);if(Hash(File.ReadAllBytes(write.Path))!=write.Expected)throw new IOException("Target changed during save. The transaction will be rolled back.");
                        File.Replace(write.Temp,write.Path,write.Rollback);write.Replaced=true;
                        if(Hash(File.ReadAllBytes(write.Rollback))!=write.Expected)throw new IOException("A concurrent update was detected during replacement.");
                    }
                    foreach(var write in writes)if(Hash(File.ReadAllBytes(write.Path))!=write.After)throw new IOException("Saved output could not be verified.");
                    complete=true;return backup;
                }
                catch(Exception error)
                {
                    bool restored=true;
                    foreach(var write in writes.Where(w=>w.Replaced).Reverse())
                    {try{if(Hash(File.ReadAllBytes(write.Path))!=write.After){restored=false;continue;}File.Replace(write.Rollback,write.Path,null);write.Replaced=false;}catch{restored=false;}}
                    if(!restored)throw new IOException("Save interrupted by another writer. Recovery files were retained; inspect the backup: "+backup,error);
                    throw;
                }
                finally
                {
                    foreach(var write in writes){try{if(write.Temp!=null&&File.Exists(write.Temp))File.Delete(write.Temp);if(write.Rollback!=null&&File.Exists(write.Rollback)&&(complete||!write.Replaced))File.Delete(write.Rollback);}catch(IOException){}catch(UnauthorizedAccessException){}}
                }
            }
        }
    }
}
