using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static class SkinExport
    {
        const string Instructions="_SkinStudio-Instructions.txt";
        sealed class ExportFile
        {
            public string RelativePath,Path,OriginalHash;
            public byte[] Original,Edited;
        }
        static void CheckSources(ExportFile[] files)
        {
            foreach(var file in files)
            {
                if(!File.Exists(file.Path)||new FileInfo(file.Path).Length!=file.Original.Length||ProjectFile.Digest(File.ReadAllBytes(file.Path))!=file.OriginalHash)
                    throw new InvalidOperationException("The source file has changed or is missing since it was loaded: "+file.RelativePath+". Export was stopped; the existing ZIP and editor changes are preserved. Save an editor project for recovery and compare the source with your matching backup before continuing.");
            }
        }
        static byte[] InstructionsFor(SkinProject project,ExportFile[] files)
        {
            var text=new StringBuilder("MP1 Skin Studio — XML export\r\n\r\nOnly modified XML files, relative to the selected skin directory.\r\nBack up or copy the complete skin directory first, then apply these files to the copy.\r\nUnmodified images and XML files remain in the existing skin directory.\r\nShared include edits can affect every page that uses that file.\r\nThe preview is approximate and is not an MP1 engine test. Validate in MP1.\r\n\r\n");
            text.Append("Active page: ").Append(project.Page).Append("\r\nActive theme: ").Append(String.IsNullOrEmpty(project.Theme)?"Default skin":project.Theme).Append("\r\nModified XML files: ").Append(files.Length).Append("\r\n\r\n");
            text.Append("Before applying, compare the destination copy's current file with Original SHA256.\r\nAfter applying, compare it with Exported SHA256. Hashes cover exact bytes, including encoding and BOM.\r\nThese hashes identify the files; they are not a digital signature or a runtime compatibility check.\r\nOnly the modified source files were checked for drift. Recheck your destination before applying.\r\n\r\n");
            foreach(var file in files)text.Append("File: ").Append(file.RelativePath).Append("\r\nOriginal bytes: ").Append(file.Original.Length).Append("\r\nOriginal SHA256: ").Append(file.OriginalHash).Append("\r\nExported bytes: ").Append(file.Edited.Length).Append("\r\nExported SHA256: ").Append(ProjectFile.Digest(file.Edited)).Append("\r\n\r\n");
            return new UTF8Encoding(false).GetBytes(text.ToString());
        }
        static void WriteEntry(ZipArchive archive,string name,byte[] bytes)
        {var entry=archive.CreateEntry(name,CompressionLevel.Optimal);using(var output=entry.Open())output.Write(bytes,0,bytes.Length);}
        static void VerifyEntry(ZipArchive archive,string name,byte[] bytes)
        {
            var entry=archive.GetEntry(name);if(entry==null||entry.Length!=bytes.Length)throw new InvalidDataException("Export verification failed: "+name);
            using(var input=entry.Open())using(var memory=new MemoryStream())
            {input.CopyTo(memory);if(!memory.ToArray().SequenceEqual(bytes))throw new InvalidDataException("Export verification failed: "+name);}
        }
        public static void Write(SkinProject project,string target)
        {
            if(!target.EndsWith(".zip",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Export as a ZIP file. Skin XML files are not overwritten directly.");
            var files=project.Sources.Values.Where(s=>s.Changed).OrderBy(s=>s.RelativePath,StringComparer.OrdinalIgnoreCase).Select(s=>
            {s.Parse();return new ExportFile{RelativePath=s.RelativePath,Path=s.Path,Original=s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray(),Edited=s.Bytes()};}).ToArray();
            if(files.Length==0)throw new InvalidOperationException("No changes yet.");
            foreach(var file in files)file.OriginalHash=ProjectFile.Digest(file.Original);
            CheckSources(files);byte[] instructions=InstructionsFor(project,files);string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var archive=ZipFile.Open(temp,ZipArchiveMode.Create)){foreach(var file in files)WriteEntry(archive,file.RelativePath,file.Edited);WriteEntry(archive,Instructions,instructions);}
                using(var archive=ZipFile.OpenRead(temp))
                {if(archive.Entries.Count!=files.Length+1)throw new InvalidDataException("Unexpected XML export entries.");foreach(var file in files)VerifyEntry(archive,file.RelativePath,file.Edited);VerifyEntry(archive,Instructions,instructions);}
                CheckSources(files);
                if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
