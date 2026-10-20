using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class CurrentRuntimeProbe
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static int Main(string[] args){try{Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        Directory.CreateDirectory(args[1]);string saved=Path.Combine(args[1],"Current-Video.mp1skinproj");SkinProject p;
        if(args[0]=="--resume")p=ProjectFile.Load(saved);
        else
        {
            p=new SkinProject(args[0]);p.OpenPage("myvideo.xml");
            var list=p.Controls.Single(c=>c.Get("type")=="listcontrol"&&c.Source.RelativePath=="common.facade.video.default.xml"&&c.Get("subtype","")=="");
            var header=p.Controls.Single(c=>c.Source.RelativePath=="common.time.xml"&&c.Get("type")=="label"&&c.Get("label")=="#header.label");
            p.EditMany(new[]{new ControlEdit(list,new Dictionary<string,string>{{"width","578"}}),new ControlEdit(header,new Dictionary<string,string>{{"label","STUDIO REVIEW READY"}})},"Current runtime review");
            VideoFolderPreview.Apply(p);p.Editor.ShowUnresolved=false;p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;ProjectFile.Save(p,saved);p=ProjectFile.Load(saved);
        }
        var originals=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));string signature=ProjectFile.ContentSignature(p),zipFile=Path.Combine(args[1],"Current-Video.zip");p.Export(zipFile);
        var changed=p.Sources.Values.Where(s=>s.Changed).ToArray();Check(changed.Length==2,"Expected two changed sources");
        using(var zip=ZipFile.OpenRead(zipFile))
        {
            Check(zip.Entries.Count==3,"Unexpected export entries");string instructions;using(var reader=new StreamReader(zip.GetEntry("_SkinStudio-Instructions.txt").Open()))instructions=reader.ReadToEnd();
            foreach(var s in changed)using(var input=zip.GetEntry(s.RelativePath).Open())using(var memory=new MemoryStream())
            {input.CopyTo(memory);Check(memory.ToArray().SequenceEqual(s.Bytes()),"Export bytes differ");Check(instructions.Contains("Original SHA256: "+originals[s.Path])&&instructions.Contains("Exported SHA256: "+ProjectFile.Digest(s.Bytes())),"Export identities differ");}
        }
        p.UndoChange();Check(!p.Dirty,"Restored Undo did not restore originals");p.RedoChange();Check(ProjectFile.ContentSignature(p)==signature,"Restored Redo differs");
        Check(originals.All(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))==h.Value),"Original source changed");
        string width=p.Controls.Single(c=>c.Get("type")=="listcontrol"&&c.Source.RelativePath=="common.facade.video.default.xml"&&c.Get("subtype","")=="").Get("width");
        string result="PASS "+(args[0]=="--resume"?"saved editor session":"fresh saved project")+": width="+width+"; header STUDIO REVIEW READY; exact two XML payloads and original/export SHA256; restored Undo/Redo; "+originals.Count+" loaded original XML hashes unchanged.";
        File.WriteAllText(Path.Combine(args[1],args[0]=="--resume"?"Resumed-Results.txt":"Prepared-Results.txt"),result);Console.WriteLine(result);
    }
}
