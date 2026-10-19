using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;

class ArtworkPreviewProbe
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static string Report(SkinProject p){return String.Join("\n",PreviewProblems.Run(p,new PreviewState(p)).Issues.Select(i=>i.Code+"|"+i.ControlKey+"|"+i.Detail));}
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Directory.CreateDirectory(args[1]);var results=new List<string>();var originals=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);int workflows=0;
            foreach(string theme in new[]{"","Christmas"})foreach(string kind in new[]{"video","music"})foreach(string mode in new[]{"list","filmstrip"})
            {
                string page=kind=="video"?"myvideo.xml":"mymusicsongs.xml",name=(theme.Length==0?"Default":theme)+"-"+kind+"-"+mode;var p=new SkinProject(args[0]);p.Theme=theme;PreparedSkinSession.Create(p,page);p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;MediaFolderPreview.Apply(p,kind);p.Editor.Collection.Mode=mode;var state=new PreviewState(p);string xml=ProjectFile.XmlSignature(p);
                int baseline=p.Controls.Count(c=>state.Visible(c)==null);var runtime=p.Controls.Where(c=>c.Get("type")=="image"&&state.ArtworkAvailable(c)==null).ToArray();Check(runtime.Length>0,name+": no real runtime artwork exercised");
                foreach(var c in runtime)ArtworkPreview.Set(p.Editor.Preview,c.Get("texture"),"");
                int remaining=p.Controls.Count(c=>state.Visible(c)==null);if(remaining>0)foreach(var c in p.Controls.Where(c=>state.Visible(c)==null))Console.WriteLine("INFO "+name+": "+c.Name+" | "+c.Get("visible"));
                if(mode=="list")Check(remaining==(kind=="video"?0:2),name+": list availability or duplicate-ID uncertainty changed");Check(remaining<=baseline,name+": artwork samples introduced new unknown visibility");Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,name+": empty artwork changed XML");
                string empty=Path.Combine(args[1],name+"-Empty.mp1skinproj");ProjectFile.Save(p,empty);var restoredEmpty=ProjectFile.Load(empty);Check(restoredEmpty.Editor.Preview.SignatureText()==p.Editor.Preview.SignatureText(),name+": empty scenario did not roundtrip");
                int targetId=kind=="video"?159357:55555;var target=p.Controls.Single(c=>SkinProject.ControlId(c)==targetId);string expression=target.Get("texture"),sample=kind=="video"?"DefaultFolderBig.png":"DefaultFolderBigMusic.png";ArtworkPreview.Set(p.Editor.Preview,expression,sample);
                Check(state.ArtworkAvailable(target)==true&&state.Evaluate("control.hasthumb("+targetId+")",new HashSet<string>())==true,name+": selected image not reflected in HasThumb");Check(p.Controls.Count(c=>state.Visible(c)==null)==remaining,name+": selected sample changed unrelated unknown visibility");
                if(mode=="list")Check(state.Visible(target)==true,name+": target sample did not become visible");
                Check(!PreviewProblems.Run(p,state).Issues.Any(i=>i.ControlKey==target.Key&&i.Code=="PREVIEW_ARTWORK"),name+": selected sample still reported as unverified");
                string clean=Path.Combine(args[1],name+".mp1skinproj");ProjectFile.Save(p,clean);string signature=ProjectFile.ContentSignature(p),report=Report(p);var loaded=ProjectFile.Load(clean);Check(ProjectFile.ContentSignature(loaded)==signature&&Report(loaded)==report,name+": sample/report changed after reopen");
                ArtworkPreview.Set(loaded.Editor.Preview,expression,null);Check(new PreviewState(loaded).ArtworkAvailable(loaded.Controls.Single(c=>SkinProject.ControlId(c)==targetId))==null,name+": original runtime uncertainty not restored");Check(loaded.Controls.Single(c=>SkinProject.ControlId(c)==targetId).Get("texture")==expression,name+": original binding changed");ArtworkPreview.Set(loaded.Editor.Preview,expression,sample);
                var editable=loaded.Controls.Single(c=>SkinProject.ControlId(c)==targetId);string before=editable.Source.Text,relative=editable.Source.RelativePath;loaded.Edit(editable,new Dictionary<string,string>{{"width",((int)editable.Number("width",200)+11).ToString()}},"Artwork checkpoint resize");string after=editable.Source.Text,edited=Path.Combine(args[1],name+"-Edited.mp1skinproj"),zip=Path.Combine(args[1],name+".zip");ProjectFile.Save(loaded,edited);var saved=ProjectFile.Load(edited);Check(saved.Editor.Preview.SignatureText()==loaded.Editor.Preview.SignatureText(),name+": edited project lost artwork");saved.Export(zip);
                using(var archive=ZipFile.OpenRead(zip)){Check(archive.Entries.Count==2,name+": sample assets leaked into export");using(var reader=new StreamReader(archive.GetEntry(relative).Open()))Check(reader.ReadToEnd()==after,name+": export changed XML");}
                saved.UndoChange();Check(!saved.Dirty&&saved.Source(Path.Combine(saved.Root,relative)).Text==before&&saved.Editor.Preview.Artwork.Count==loaded.Editor.Preview.Artwork.Count,name+": restored undo changed samples");saved.RedoChange();Check(saved.Source(Path.Combine(saved.Root,relative)).Text==after,name+": restored redo mismatch");
                if(mode=="list")using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p),Grid=false,Size=new Size(1280,760)})using(var image=new Bitmap(1280,760)){canvas.DrawToBitmap(image,new Rectangle(0,0,1280,760));image.Save(Path.Combine(args[1],name+".png"));}
                foreach(var source in p.Sources.Values.Concat(saved.Sources.Values)){string hash=ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray());Check(ProjectFile.Digest(File.ReadAllBytes(source.Path))==hash,name+": original XML changed");originals[source.Path]=hash;}
                results.Add("PASS "+name+": "+runtime.Length+" runtime image controls explicitly sampled empty; "+baseline+" -> "+remaining+" unknown visibility; selected sample/HasThumb/save/reopen/report/export/undo/redo and original uncertainty restoration.");Console.WriteLine(results.Last());workflows++;
            }
            foreach(var pair in originals)Check(ProjectFile.Digest(File.ReadAllBytes(pair.Key))==pair.Value,"Original changed after matrix");results.Add("PASS "+workflows+" real Titan artwork workflows; "+originals.Count+" distinct loaded original XML hashes unchanged. Render output generated for list workflows only.");File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);Console.WriteLine(results.Last());return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
