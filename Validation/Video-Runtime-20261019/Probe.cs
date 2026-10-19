using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class RuntimeVideoProbe
{
    static int Main(string[] args)
    {
        var p=new SkinProject(args[0]);p.OpenPage("myvideo.xml");Directory.CreateDirectory(args[1]);
        var before=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));
        var list=p.Controls.Single(c=>c.Get("type")=="listcontrol"&&c.Source.RelativePath=="common.facade.video.default.xml"&&c.Get("subtype","")=="");
        var header=p.Controls.Single(c=>c.Source.RelativePath=="common.time.xml"&&c.Get("type")=="label"&&c.Get("label")=="#header.label");
        p.EditMany(new[]{new ControlEdit(list,new Dictionary<string,string>{{"width","578"}}),new ControlEdit(header,new Dictionary<string,string>{{"label","STUDIO EXPORT TEST"}})},"Isolated runtime verification");
        VideoFolderPreview.Apply(p);p.Editor.ShowUnresolved=false;
        p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;
        string file=Path.Combine(args[1],"Video-Export.zip");p.Export(file);ProjectFile.Save(p,Path.Combine(args[1],"Video-Export.mp1skinproj"));
        using(var zip=ZipFile.OpenRead(file))
        {if(zip.Entries.Count!=3)throw new Exception("Unexpected export entries");foreach(string source in new[]{"common.time.xml","common.facade.video.default.xml"})using(var reader=new StreamReader(zip.GetEntry(source).Open()))if(reader.ReadToEnd()!=p.Sources.Values.Single(s=>s.RelativePath==source).Text)throw new Exception("Export mismatch");}
        if(before.Any(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))!=h.Value))throw new Exception("Original skin changed");
        Console.WriteLine("PASS isolated candidate: list width 698 to 578, header STUDIO EXPORT TEST; exactly two XML files and instructions; export matches project; all "+before.Count+" original source hashes unchanged.");return 0;
    }
}
