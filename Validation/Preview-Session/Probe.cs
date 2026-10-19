using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using MP1SkinStudio;
class ContentComparisonProbe
{
    static int Main(string[] args)
    {
        var p=new SkinProject(args[0]);p.OpenPage("myvideo.xml");
        var hashes=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));
        string xml=ProjectFile.XmlSignature(p);VideoFolderPreview.Apply(p);
        p.Editor.Language.Primary=LanguagePack.Load(args[1]);p.Editor.Language.Prefixes=false;
        var state=new PreviewState(p);
        var unknown=p.Controls.Where(c=>state.Visible(c)==null).ToArray();
        Console.WriteLine("Controls: "+p.Controls.Count+"; hidden: "+p.Controls.Count(c=>state.Visible(c)==false)+"; unresolved: "+unknown.Length);
        foreach(string expression in new[]{"control.hastext(7773)","control.hasthumb(60)","!control.hasthumb(60)","control.hasthumb(159357)"})
            Console.WriteLine(expression+" = "+(state.Evaluate(expression,new HashSet<string>()).HasValue?state.Evaluate(expression,new HashSet<string>()).ToString():"unknown"));
        foreach(var c in p.Controls.Where(c=>new[]{"Cover Art","Default Cover Art","ClearLogo","Selected item Label"}.Contains(c.Get("description"))))Console.WriteLine(c.Get("description")+": "+(state.Visible(c).ToString()??"unknown"));
        if(ProjectFile.XmlSignature(p)!=xml||p.Dirty||p.Undo.Count!=0||hashes.Any(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))!=h.Value))throw new Exception("Original source changed");
        Console.WriteLine("PASS unchanged XML, undo history and all "+hashes.Count+" loaded source hashes");
        if(args.Length>2){if(state.Evaluate("control.hastext(7773)",new HashSet<string>())!=false||state.Evaluate("control.hasthumb(60)",new HashSet<string>())!=true)throw new Exception("Known Titan content condition failed");string localized,origin;p.Editor.Language.TryGet(299,out localized,out origin);if(state.Text(p.Controls[0],"#(string.rtrim(L(299),' :'))")!=localized.TrimEnd(' ',':'))throw new Exception("Real language expression failed");Console.WriteLine("PASS nested localization against installed German snapshot");ProjectFile.Save(p,args[2]);var loaded=ProjectFile.Load(args[2]);var restored=new PreviewState(loaded);if(restored.Visible(loaded.Controls.First(c=>c.Get("id")=="7773"))!=state.Visible(p.Controls.First(c=>c.Get("id")=="7773")))throw new Exception("Roundtrip visibility changed");if(PreparedSkinSession.Create(loaded,loaded.Page).Preview.FocusId!=50)throw new Exception("Saved list focus lost"); Console.WriteLine("PASS project roundtrip retains list focus and content condition result");}
        if(args.Length>3){p.Editor.ShowUnresolved=false;using(var canvas=new SkinCanvas{Project=p,State=state,Grid=false,Size=new Size(1280,760)})using(var bitmap=new Bitmap(1280,760)){canvas.DrawToBitmap(bitmap,new Rectangle(0,0,1280,760));bitmap.Save(args[3],System.Drawing.Imaging.ImageFormat.Png);}Console.WriteLine("PASS offscreen production renderer completed");}
        return 0;
    }
}
