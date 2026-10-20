using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using MP1SkinStudio;
class VerifiedExportTitanProbe
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static int Main(string[] args){try{Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        Directory.CreateDirectory(args[1]);var lines=new List<string>();var originals=new Dictionary<string,string>();int workflows=0,exported=0;
        foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","myvideo.xml","SettingsGUI.xml"})
        {
            var p=new SkinProject(args[0]){Theme=theme};p.OpenPage(page);string originalSignature=ProjectFile.XmlSignature(p);
            foreach(var source in p.Sources.Values)originals[source.Path]=ProjectFile.Digest(File.ReadAllBytes(source.Path));
            // Edit one control in each of up to three concrete page/include files.
            var controls=p.Controls.Where(c=>c.Get("type")!="group"&&!c.IsBound("width")&&c.Number("width",0)>0&&p.Controls.Count(x=>x.Key==c.Key)==1).GroupBy(c=>c.Source).Take(3).Select(g=>g.First()).ToArray();
            Check(controls.Length>0,"No editable real-page controls");
            p.EditMany(controls.Select(c=>new ControlEdit(c,new Dictionary<string,string>{{"width",(c.Number("width",0)+9).ToString(CultureInfo.InvariantCulture)}})),"Verify multi-file export");
            string stem=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page),file=Path.Combine(args[1],stem+".mp1skinproj"),zip=Path.Combine(args[1],stem+".zip"),signature=ProjectFile.ContentSignature(p);
            ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(ProjectFile.ContentSignature(loaded)==signature,"Project roundtrip changed state");loaded.Export(zip);Check(ProjectFile.ContentSignature(loaded)==signature,"Export changed project state");
            var changed=loaded.Sources.Values.Where(s=>s.Changed).ToArray();
            using(var archive=ZipFile.OpenRead(zip))
            {
                Check(archive.Entries.Count==changed.Length+1,"Unexpected export entries");string instructions;using(var reader=new StreamReader(archive.GetEntry("_SkinStudio-Instructions.txt").Open()))instructions=reader.ReadToEnd();
                Check(!instructions.Contains(p.Root)&&instructions.Contains("Active page: "+page)&&instructions.Contains("Active theme: "+(theme.Length==0?"Default skin":theme)),"Export context is incorrect");
                foreach(var source in changed)
                {
                    byte[] bytes;using(var input=archive.GetEntry(source.RelativePath).Open())using(var memory=new MemoryStream()){input.CopyTo(memory);bytes=memory.ToArray();}
                    Check(bytes.SequenceEqual(source.Bytes())&&instructions.Contains("Original SHA256: "+originals[source.Path])&&instructions.Contains("Exported SHA256: "+ProjectFile.Digest(bytes)),"Payload/hash mismatch");exported++;
                }
            }
            loaded.UndoChange();Check(!loaded.Dirty&&ProjectFile.XmlSignature(loaded)==originalSignature,"Restored Undo failed");loaded.RedoChange();Check(ProjectFile.ContentSignature(loaded)==signature,"Restored Redo failed");
            p.UndoChange();Check(!p.Dirty&&ProjectFile.XmlSignature(p)==originalSignature,"Undo failed");p.RedoChange();Check(ProjectFile.ContentSignature(p)==signature,"Redo failed");
            lines.Add("PASS "+stem+": "+changed.Length+" changed XML files; project/save/reopen/verified exact-byte export/hash records/normal and restored Undo/Redo.");workflows++;
        }
        foreach(var source in originals)Check(ProjectFile.Digest(File.ReadAllBytes(source.Key))==source.Value,"Original skin changed");
        lines.Add("PASS "+workflows+" complete workflows; "+exported+" exported XML payload checks; "+originals.Count+" loaded original XML hashes unchanged. No installed-client run.");
        File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);
    }
}
