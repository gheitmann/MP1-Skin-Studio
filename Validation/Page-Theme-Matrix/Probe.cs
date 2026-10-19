using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;

class PageThemeMatrix
{
    static int Main(string[] args)
    {
        string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
        var report=new List<string>();int failures=0;
        foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","myHome.xml","settings.xml","myvideo.xml","mymusicgenres.xml","mypics.xml"})
        {
            string name=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page);
            try
            {
                var p=new SkinProject(args[0]){Theme=theme};PreparedSkinSession.Create(p,page);
                var hashes=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));
                string baseline=ProjectFile.XmlSignature(p);float position;
                var control=p.Controls.First(c=>!LayoutTools.IsContainer(c)&&!c.AutomaticLayout&&!c.IsBound("posX")&&Single.TryParse(c.Get("posX"),NumberStyles.Float,CultureInfo.InvariantCulture,out position));
                string key=control.Key;string value=(control.Number("posX",0)+3).ToString(CultureInfo.InvariantCulture);
                p.Edit(control,new Dictionary<string,string>{{"posX",value}},"Page/theme matrix position");
                PlaybackPreview.Stopped(p.Editor.Preview);p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;p.Editor.ShowUnresolved=false;
                if(page=="myvideo.xml")VideoFolderPreview.Apply(p);
                string signature=ProjectFile.ContentSignature(p),project=Path.Combine(output,name+".mp1skinproj");
                ProjectFile.Save(p,project);p=ProjectFile.Load(project);var prepared=PreparedSkinSession.Create(p,page);
                if(signature!=ProjectFile.ContentSignature(p)||p.Controls.First(c=>c.Key==key).Get("posX")!=value)throw new Exception("Project/session roundtrip changed state");
                var changes=p.Sources.Values.Where(s=>s.Changed).ToDictionary(s=>s.RelativePath,s=>s.Text);
                string export=Path.Combine(output,name+".zip");p.Export(export);
                using(var zip=ZipFile.OpenRead(export))
                {
                    if(zip.Entries.Count!=changes.Count+1)throw new Exception("Unexpected export entries");
                    foreach(var pair in changes)using(var reader=new StreamReader(zip.GetEntry(pair.Key).Open()))if(reader.ReadToEnd()!=pair.Value)throw new Exception("Export differs from reopened snapshot");
                }
                p.UndoChange();if(p.Dirty||ProjectFile.XmlSignature(p)!=baseline)throw new Exception("Snapshot undo differs from baseline");
                p.RedoChange();if(ProjectFile.ContentSignature(p)!=signature)throw new Exception("Snapshot redo changed state");
                if(page=="settings.xml"||page=="BasicHome.xml")
                    using(var canvas=new SkinCanvas{Project=p,State=prepared.Preview,Grid=false,Size=new Size(1280,760)})using(var bitmap=new Bitmap(1280,760))
                    {canvas.DrawToBitmap(bitmap,new Rectangle(0,0,1280,760));bitmap.Save(Path.Combine(output,name+".png"));}
                if(hashes.Any(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))!=h.Value))throw new Exception("Original source changed");
                report.Add("PASS "+name+": "+p.Controls.Count+" controls; edit, project/session roundtrip, exact export, undo/redo, "+hashes.Count+" original hashes unchanged");
            }
            catch(Exception ex){failures++;report.Add("FAIL "+name+": "+ex);}
        }
        report.Add((12-failures)+" page/theme workflows passed; "+failures+" failed. No MP1 client run.");
        File.WriteAllLines(Path.Combine(output,"Results.txt"),report);foreach(string line in report)Console.WriteLine(line);return failures==0?0:1;
    }
}
