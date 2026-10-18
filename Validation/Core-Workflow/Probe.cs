using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MP1SkinStudio;

// Build against the editor executable. This exercises the production model,
// project persistence and export against an installed real skin, not a fixture.
public static class CoreWorkflowProbe
{
    public static int Main(string[] args)
    {
        try
        {
            string root=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]),languages=Path.GetFullPath(args[2]);
            if(output.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase)||output==root)throw new Exception("Output must be outside the source skin.");
            Directory.CreateDirectory(output);var report=new List<string>();
            var p=new SkinProject(root);p.OpenPage("settings.xml");
            var original=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));
            var group=p.Controls.Single(c=>c.Get("type")=="group"&&c.Get("description")=="buttons");
            var menu=p.Controls.Single(c=>c.Source.RelativePath=="settings.xml"&&c.Get("type")=="label"&&c.Get("label")=="924");
            var icon=p.Controls.Single(c=>c.Source.RelativePath=="settings.xml"&&c.Get("type")=="image"&&c.Get("texture")=="icon_settings.png");
            string menuKey=menu.Key,iconKey=icon.Key,groupKey=group.Key;
            if(!File.Exists(Path.Combine(root,"Media","icon_music.png")))throw new Exception("Replacement texture is unavailable.");
            report.Add("PASS  Loaded installed Titan settings.xml and uniquely selected menu label, icon and button group.");
            p.SetStackLayout(group,"StackLayout(11, Vertical, true)",false);
            p.Edit(p.Controls.Single(c=>c.Key==menuKey),new Dictionary<string,string>{{"posX","172"},{"posY","235"},{"width","440"},{"label","STUDIO TEST"}},"Core workflow: menu position, size and text");
            p.Edit(p.Controls.Single(c=>c.Key==iconKey),new Dictionary<string,string>{{"width","70"},{"height","70"},{"texture","icon_music.png"}},"Core workflow: icon size and texture");
            p.Editor.Language.Primary=LanguagePack.Load(Path.Combine(languages,"strings_de.xml"));p.Editor.Language.Fallback=LanguagePack.Load(Path.Combine(languages,"strings_en.xml"));p.Editor.ShowUnresolved=false;
            var edited=p.Sources.Values.Where(s=>s.Changed).ToArray();if(edited.Length!=1||edited[0].RelativePath!="settings.xml")throw new Exception("Unexpected modified sources.");
            report.Add("PASS  Edited position, size, text, texture and stack spacing through production editor APIs; only settings.xml changed in memory.");
            string expected=ProjectFile.ContentSignature(p),expectedXml=edited[0].Text;
            string project=Path.Combine(output,"Core-Workflow.mp1skinproj");ProjectFile.Save(p,project);p=null;
            p=ProjectFile.Load(project);
            if(ProjectFile.ContentSignature(p)!=expected||p.Controls.Single(c=>c.Key==menuKey).Get("label")!="STUDIO TEST"||p.Controls.Single(c=>c.Key==iconKey).Get("texture")!="icon_music.png"||p.Controls.Single(c=>c.Key==groupKey).Get("layout")!="StackLayout(11, Vertical, true)")throw new Exception("Project round trip lost edits.");
            report.Add("PASS  Saved, discarded the model and reopened the project; all edits, identity mappings and language state survived exactly.");
            string zip=Path.Combine(output,"Core-Workflow-Export.zip");p.Export(zip);
            using(var archive=ZipFile.OpenRead(zip))
            {
                if(archive.Entries.Count!=2||archive.GetEntry("settings.xml")==null||archive.GetEntry("_SkinStudio-Instructions.txt")==null)throw new Exception("Unexpected export entries.");
                using(var reader=new StreamReader(archive.GetEntry("settings.xml").Open()))if(reader.ReadToEnd()!=expectedXml)throw new Exception("Export differs from reopened project.");
            }
            report.Add("PASS  Exported the reopened project; ZIP contains exactly the edited settings.xml and export instructions.");
            p.UndoChange();if(p.Dirty)throw new Exception("Reopened snapshot cannot return to original baseline.");p.RedoChange();if(ProjectFile.ContentSignature(p)!=expected)throw new Exception("Snapshot redo lost edits.");
            report.Add("PASS  Snapshot undo restored the original XML baseline and redo restored the exported state.");
            if(original.Any(pair=>ProjectFile.Digest(File.ReadAllBytes(pair.Key))!=pair.Value))throw new Exception("An original skin source changed.");
            report.Add("PASS  All loaded original skin source hashes remain unchanged.");
            report.Add("");report.Add("Client rendering and keyboard navigation must be observed separately; the API checks do not establish a successful MP1 runtime test.");
            File.WriteAllLines(Path.Combine(output,"Core-Workflow-API.txt"),report,new UTF8Encoding(false));return 0;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetFullPath(args[1]),"Probe-Failure.txt"),ex.ToString());return 1;}
    }
}
