using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MP1SkinStudio;
class BasicHomeProbe
{
    static int Main(string[] args)
    {
        Directory.CreateDirectory(args[1]);var lines=new List<string>();
        foreach(string theme in new[]{"","Christmas"})
        {
            var p=new SkinProject(args[0]){Theme=theme};p.OpenPage("BasicHome.xml");
            var hashes=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));string xml=ProjectFile.XmlSignature(p);
            BasicHomePreview.Apply(p);p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;var state=new PreviewState(p);
            var buttons=p.Controls.Where(c=>c.Get("type")=="button"&&SkinProject.ControlId(c)>=2&&SkinProject.ControlId(c)<=7).ToArray();
            foreach(var c in buttons)if(state.Visible(c)!=true)throw new Exception("Tile not visible: "+c.Get("description")+" "+state.Visible(c));
            if(buttons.Length!=6||state.FocusId!=4)throw new Exception("Expected six tiles and default focus 4");
            string name=theme.Length==0?"Default":theme;string snapshot=Path.Combine(args[1],name+"-BasicHome.mp1skinproj");ProjectFile.Save(p,snapshot);
            var loaded=ProjectFile.Load(snapshot);var prepared=PreparedSkinSession.Create(loaded,loaded.Page);if(ProjectFile.ContentSignature(loaded)!=ProjectFile.ContentSignature(p)||prepared.Preview.FocusId!=4)throw new Exception("Reopened state changed");
            using(var canvas=new SkinCanvas{Project=loaded,State=prepared.Preview,Grid=false,Size=new Size(1280,760)})using(var bitmap=new Bitmap(1280,760))
            {canvas.DrawToBitmap(bitmap,new Rectangle(0,0,1280,760));bitmap.Save(Path.Combine(args[1],name+"-BasicHome.png"));}
            if(p.Dirty||p.Undo.Count!=0||ProjectFile.XmlSignature(p)!=xml||hashes.Any(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))!=h.Value))throw new Exception("Original sources or history changed");
            lines.Add("PASS "+name+": six visible menu buttons, focus 4, exact project/session roundtrip, production render; "+p.Controls.Count+" controls, "+p.Controls.Count(c=>state.Visible(c)==false)+" hidden, "+p.Controls.Count(c=>state.Visible(c)==null)+" unresolved; "+hashes.Count+" original hashes unchanged");
        }
        File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);return 0;
    }
}
