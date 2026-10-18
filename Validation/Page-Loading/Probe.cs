using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;

public static class PageLoadingProbe
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS: "+name);}
    static SkinControl List(SkinProject p){return p.Controls.Single(c=>c.Get("type")=="listcontrol"&&c.Source.RelativePath=="common.facade.video.default.xml");}
    public static int Main(string[] args)
    {
        try
        {
            string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
            var p=new SkinProject(args[0]);var prepared=PreparedSkinSession.Create(p,"myvideo.xml");
            Check(prepared.Project==p&&prepared.Preview.Project==p&&prepared.Pages.Contains("myvideo.xml")&&p.Page=="myvideo.xml","Real Titan session is fully prepared before publication");
            string theme=prepared.Themes.OrderBy(name=>name,StringComparer.Ordinal).FirstOrDefault();
            if(theme!=null)
            {
                p.PrepareTheme(theme);Check(p.Theme==theme&&p.Page=="myvideo.xml","Installed theme prepares successfully: "+theme);
                p.PrepareTheme("");Check(p.Theme==""&&p.Page=="myvideo.xml","Returning from the installed theme prepares the default page");
            }
            p.Edit(List(p),new Dictionary<string,string>{{"textureHeight","72"}},"Shared row height");
            p.OpenPage("myvideoTitle.xml");Check(List(p).Get("textureHeight")=="72","Shared video list edit survives switching to another importing page");
            var icon=p.Controls.Single(c=>c.Source.RelativePath=="myvideoTitle.xml"&&c.Get("type")=="image"&&c.Get("texture")=="icon_movies.png");
            p.Edit(icon,new Dictionary<string,string>{{"posX","90"}},"Title-page icon");
            p.OpenPage("settings.xml");var heading=p.Controls.Single(c=>c.Source.RelativePath=="settings.xml"&&c.Get("label")=="924");
            p.Edit(heading,new Dictionary<string,string>{{"label","MULTIPAGE TEST"}},"Settings heading");
            Check(p.Sources.Values.Count(s=>s.Changed)==3,"Three edits remain pending across three XML sources");
            string signature=ProjectFile.ContentSignature(p);p.UndoChange();p.UndoChange();
            Check(p.Page=="settings.xml"&&p.Sources.Values.Count(s=>s.Changed)==1,"Undo across page boundaries preserves the current page and earlier shared edit");
            p.RedoChange();p.RedoChange();Check(ProjectFile.ContentSignature(p)==signature,"Cross-page redo restores the complete edited snapshot");
            string project=Path.Combine(output,"Multi-Page.mp1skinproj");ProjectFile.Save(p,project);p=ProjectFile.Load(project);
            Check(ProjectFile.ContentSignature(p)==signature,"Saving while Settings is active retains edits belonging to other pages");
            p.OpenPage("myvideoTitle.xml");Check(List(p).Get("textureHeight")=="72"&&p.Controls.Single(c=>c.Source.RelativePath=="myvideoTitle.xml"&&c.Get("texture")=="icon_movies.png").Get("posX")=="90","Reopened project exposes both shared and page-local edits after switching pages");
            p.OpenPage("myvideo.xml");signature=ProjectFile.ContentSignature(p);var selected=List(p);bool rejected=false;
            try{p.OpenPage("missing-page-probe.xml");}catch(FileNotFoundException){rejected=true;}
            Check(rejected&&p.Page=="myvideo.xml"&&p.Controls.Contains(selected)&&ProjectFile.ContentSignature(p)==signature,"A missing page leaves the reopened edited session intact");
            var changed=p.Sources.Values.Where(s=>s.Changed).ToDictionary(s=>s.RelativePath,s=>s.Text);
            string zipPath=Path.Combine(output,"Multi-Page.zip");p.Export(zipPath);
            using(var zip=ZipFile.OpenRead(zipPath))
            {
                Check(zip.Entries.Count==4&&changed.All(pair=>zip.GetEntry(pair.Key)!=null),"Export contains all three edited XML sources and instructions");
                foreach(var pair in changed)using(var reader=new StreamReader(zip.GetEntry(pair.Key).Open()))if(reader.ReadToEnd()!=pair.Value)throw new Exception("Export changed source content.");
            }
            Check(p.Sources.Values.All(s=>ProjectFile.Digest(File.ReadAllBytes(s.Path))==ProjectFile.Digest(s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray())),"All loaded original source files remain unchanged");
            Console.WriteLine(checks+" multi-page checks passed.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
