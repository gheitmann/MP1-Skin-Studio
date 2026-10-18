using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class ProjectLocation
    {public string Root,Page,Theme;}

    // Editor projects are snapshots, not an alternative MediaPortal skin format.
    // Only changed XML and their original bytes are embedded; images stay in the skin.
    public static partial class ProjectFile
    {
        const long MaximumEntry=20L*1024*1024,MaximumTotal=100L*1024*1024;
        public static string Digest(byte[] bytes)
        {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static string XmlSignature(SkinProject project)
        {
            var text=new StringBuilder(project.Root);
            foreach(var file in project.Sources.Values.Where(s=>s.Changed).OrderBy(s=>s.RelativePath,StringComparer.OrdinalIgnoreCase))text.Append('\n').Append(file.RelativePath).Append(':').Append(Digest(file.Bytes()));
            return Digest(Encoding.UTF8.GetBytes(text.ToString()));
        }
        static byte[] OriginalBytes(SkinSource source)
        {return source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray();}
        static void WriteEntry(ZipArchive archive,string name,byte[] bytes)
        {var entry=archive.CreateEntry(name,CompressionLevel.Optimal);using(var output=entry.Open())output.Write(bytes,0,bytes.Length);}
        public static void Save(SkinProject project,string target)
        {
            if(!target.EndsWith(".mp1skinproj",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Editorprojekte mit der Endung .mp1skinproj speichern.");
            var changed=project.Sources.Values.Where(s=>s.Changed).OrderBy(s=>s.RelativePath,StringComparer.OrdinalIgnoreCase).ToArray();
            if(changed.Length>256)throw new InvalidOperationException("Das Projekt enthält mehr als 256 geänderte XML-Dateien.");
            var doc=new XmlDocument{XmlResolver=null};var root=doc.CreateElement("skinStudioProject");doc.AppendChild(root);
            root.SetAttribute("version","4");root.SetAttribute("root",project.Root);root.SetAttribute("page",project.Page??"");root.SetAttribute("theme",project.Theme??"");
            var files=doc.CreateElement("files");root.AppendChild(files);
            var entries=new Dictionary<string,byte[]>();long total=0;
            for(int i=0;i<changed.Length;i++)
            {
                var source=changed[i];source.Parse();byte[] original=OriginalBytes(source),edited=source.Bytes();
                if(source.OriginalOrdinals.Count!=source.Elements.Count(n=>n.Name=="control"))throw new InvalidDataException("Elementzuordnung passt nicht zur Quelldatei: "+source.RelativePath);
                if(original.Length>MaximumEntry||edited.Length>MaximumEntry)throw new InvalidOperationException("XML-Datei ist zu groß für ein Editorprojekt.");
                total+=original.Length+edited.Length;if(total>MaximumTotal)throw new InvalidOperationException("Editorprojekt überschreitet 100 MB unkomprimierte XML-Daten.");
                string before="snapshots/"+i.ToString("D3")+".original.xml",after="snapshots/"+i.ToString("D3")+".edited.xml";
                var file=doc.CreateElement("file");file.SetAttribute("path",source.RelativePath);file.SetAttribute("originalSha256",Digest(original));file.SetAttribute("editedSha256",Digest(edited));file.SetAttribute("original",before);file.SetAttribute("edited",after);files.AppendChild(file);
                file.SetAttribute("controlOrder",String.Join(",",source.OriginalOrdinals));
                entries[before]=original;entries[after]=edited;
            }
            SaveEditorState(project,doc);SaveLanguagePreview(project,doc,entries);
            if(Encoding.UTF8.GetByteCount(doc.OuterXml)>1024*1024)throw new InvalidDataException("Die Projektbeschreibung überschreitet 1 MB. Weniger Editor-Markierungen oder kürzere Beispielwerte verwenden.");
            if(entries.Values.Sum(v=>(long)v.Length)+Encoding.UTF8.GetByteCount(doc.OuterXml)>MaximumTotal)throw new InvalidDataException("Editorprojekt überschreitet 100 MB.");
            string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var archive=ZipFile.Open(temp,ZipArchiveMode.Create))
                {
                    WriteEntry(archive,"project.xml",Encoding.UTF8.GetBytes(doc.OuterXml));
                    foreach(var entry in entries)WriteEntry(archive,entry.Key,entry.Value);
                }
                if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        static void CheckArchive(ZipArchive archive)
        {
            if(archive.Entries.Count>515)throw new InvalidDataException("Zu viele Dateien im Editorprojekt.");
            var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
            foreach(var entry in archive.Entries)
            {
                if(!names.Add(entry.FullName))throw new InvalidDataException("Doppelter Eintrag im Editorprojekt: "+entry.FullName);
                if(entry.Length>MaximumEntry)throw new InvalidDataException("Projektdatei ist zu groß: "+entry.FullName);
                total+=entry.Length;if(total>MaximumTotal)throw new InvalidDataException("Editorprojekt überschreitet 100 MB.");
            }
        }
        static byte[] ReadEntry(ZipArchive archive,string name)
        {
            var entry=archive.GetEntry(name);if(entry==null)throw new InvalidDataException("Projektbestandteil fehlt: "+name);
            using(var input=entry.Open())using(var output=new MemoryStream())
            {var buffer=new byte[8192];int count;while((count=input.Read(buffer,0,buffer.Length))>0){if(output.Length+count>MaximumEntry)throw new InvalidDataException("Projektbestandteil ist zu groß.");output.Write(buffer,0,count);}return output.ToArray();}
        }
        static XmlDocument Manifest(ZipArchive archive)
        {
            CheckArchive(archive);var bytes=ReadEntry(archive,"project.xml");
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024};
            var doc=new XmlDocument{XmlResolver=null};using(var stream=new MemoryStream(bytes))using(var reader=XmlReader.Create(stream,settings))doc.Load(reader);
            if(doc.DocumentElement==null||doc.DocumentElement.Name!="skinStudioProject"||(doc.DocumentElement.GetAttribute("version")!="1"&&doc.DocumentElement.GetAttribute("version")!="2"&&doc.DocumentElement.GetAttribute("version")!="3"&&doc.DocumentElement.GetAttribute("version")!="4"))throw new InvalidDataException("Unbekanntes Editorprojekt-Format.");
            return doc;
        }
        static ProjectLocation Location(XmlDocument doc)
        {return new ProjectLocation{Root=doc.DocumentElement.GetAttribute("root"),Page=doc.DocumentElement.GetAttribute("page"),Theme=doc.DocumentElement.GetAttribute("theme")};}
        public static ProjectLocation Inspect(string path)
        {using(var archive=ZipFile.OpenRead(path))return Location(Manifest(archive));}
        public static bool LocalRoot(string root)
        {return !String.IsNullOrWhiteSpace(root)&&System.IO.Path.IsPathRooted(root)&&!root.StartsWith("\\",StringComparison.Ordinal)&&!root.StartsWith("//",StringComparison.Ordinal);}
        public static SkinProject Load(string path,string rootOverride=null)
        {
            using(var archive=ZipFile.OpenRead(path))
            {
                var doc=Manifest(archive);var location=Location(doc);string root=rootOverride??location.Root;
                if(!LocalRoot(root))throw new InvalidDataException("Bitte einen lokalen Skinordner ausdrücklich auswählen.");
                if(!Directory.Exists(root))throw new DirectoryNotFoundException("Der gespeicherte Skinordner fehlt: "+root);
                var project=new SkinProject(root){Theme=location.Theme};project.SafePath(location.Page);
                if(location.Theme.Length>0&&!Directory.Exists(project.SafePath("Themes/"+location.Theme)))throw new DirectoryNotFoundException("Themeordner fehlt: "+location.Theme);
                var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var changes=new List<FileChange>();var remap=new ControlKeyRemap();
                var nodes=doc.SelectNodes("/skinStudioProject/files/file");if(nodes.Count>256)throw new InvalidDataException("Zu viele geänderte XML-Dateien.");
                // Validate all snapshots and source hashes before installing any edited text.
                foreach(XmlElement file in nodes)
                {
                    string relative=file.GetAttribute("path"),physical=project.SafePath(relative);
                    if(!relative.EndsWith(".xml",StringComparison.OrdinalIgnoreCase)||!paths.Add(physical))throw new InvalidDataException("Ungültiger oder doppelter XML-Pfad im Projekt.");
                    byte[] original=ReadEntry(archive,file.GetAttribute("original")),edited=ReadEntry(archive,file.GetAttribute("edited"));
                    if(Digest(original)!=file.GetAttribute("originalSha256")||Digest(edited)!=file.GetAttribute("editedSha256"))throw new InvalidDataException("Beschädigter Projektschnappschuss: "+relative);
                    if(!File.Exists(physical)||new FileInfo(physical).Length!=original.Length||Digest(File.ReadAllBytes(physical))!=file.GetAttribute("originalSha256"))throw new InvalidDataException("Die Quelldatei hat sich seit dem Projektstand geändert oder fehlt: "+relative+". Es wurde nichts übernommen.");
                    var source=project.Source(physical);
                    // Encoding and BOM of source and edited snapshot must match. Use the
                    // source decoder; no temporary files and no extraction into the skin.
                    if(edited.Length<source.Preamble.Length||!edited.Take(source.Preamble.Length).SequenceEqual(source.Preamble))throw new InvalidDataException("Kodierung/BOM passt nicht zum Original: "+relative);
                    string after=source.Encoding.GetString(edited,source.Preamble.Length,edited.Length-source.Preamble.Length);
                    var validation=SkinSource.FromText(after,relative);
                    remap.Add(source,SnapshotControlOrder(source,validation.Elements.Count(n=>n.Name=="control"),file));
                    changes.Add(new FileChange{Source=source,Before=source.Text,After=validation.Text});
                }
                foreach(var file in changes){file.Source.Text=file.After;file.Source.Parse();file.Source.Bytes();}
                foreach(var pair in remap.IdentityOrders)pair.Key.OriginalOrdinals=pair.Value[1].ToList();
                project.OpenPage(location.Page);LoadEditorState(project,doc);LoadLanguagePreview(project,doc,archive);
                if(changes.Any(f=>f.Before!=f.After))project.Undo.Push(new Change{Description="Gespeichertes Projekt wiederherstellen",Files=changes,Remap=remap});
                return project;
            }
        }
    }
}
