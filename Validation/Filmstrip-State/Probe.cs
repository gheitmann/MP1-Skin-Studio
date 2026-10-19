using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class FilmstripStateProbe
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 [STAThread] static int Main(string[] args){try{
 Directory.CreateDirectory(args[1]);var results=new List<string>();var hashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);int count=0;
 foreach(string theme in new[]{"","Christmas"})foreach(string scenario in new[]{"Locked","Unlocked","Unverified"}){
 string name=(theme.Length==0?"Default":theme)+"-"+scenario;var p=new SkinProject(args[0]);p.Theme=theme;PreparedSkinSession.Create(p,"myvideo.xml");p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;MediaFolderPreview.Apply(p,"video");p.Editor.Collection.Mode="filmstrip";p.Editor.Collection.Items.Clear();for(int i=0;i<9;i++)p.Editor.Collection.Items.Add(new PreviewItem{Title="Video sample "+(i+1),Image="DefaultFolderBig.png"});var state=new PreviewState(p);
 foreach(var c in p.Controls.Where(c=>c.Get("type")=="image"&&state.ArtworkAvailable(c)==null).ToArray())ArtworkPreview.Set(p.Editor.Preview,c.Get("texture"),"");
 var art=p.Controls.First(c=>c.Name=="ClearArt Filmstrip");ArtworkPreview.Set(p.Editor.Preview,art.Get("texture"),"DefaultFolderBig.png");VideoPinPreview.Set(p.Editor.Preview,scenario=="Unverified"?(bool?)null:scenario=="Locked");state=new PreviewState(p);
 var locked=p.Controls.Single(c=>c.Name=="Content locked");var unlocked=p.Controls.Single(c=>c.Name=="Content unlock");
 Check(state.Visible(locked)==(scenario=="Unverified"?(bool?)null:scenario=="Locked")&&state.Visible(unlocked)==(scenario=="Unverified"?(bool?)null:scenario=="Unlocked"),name+": PIN state was inferred or wrong");
 int unknown=p.Controls.Count(c=>state.Visible(c)==null);Check(unknown==(scenario=="Unverified"?2:0),name+": filmstrip visibility gap remains");string beforeSignature=ProjectFile.ContentSignature(p),xml=ProjectFile.XmlSignature(p);var start=new StackPreview(p,state).Bounds(art);Check(start.X==130,name+": real property position ignored");
 Check(p.TryMoveCollectionSelection(state,"onright")&&p.TryMoveCollectionSelection(state,"onright"),name+": sample navigation failed");var moved=new StackPreview(p,state).Bounds(art);float expected=Single.Parse(state.Values["#facadeview.focus.X"]);Check(moved.X==expected&&moved.X==610&&moved.X!=start.X,name+": overlay did not follow selected thumbnail");var icon=new StackPreview(p,state).Bounds(locked);Check(icon.X==expected+230-30,name+": PIN icon arithmetic not applied");Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0&&ProjectFile.ContentSignature(p)!=beforeSignature,name+": navigation changed XML or lost saved selection state");
 var report=PreviewProblems.Run(p,state);Check(!report.Issues.Any(i=>i.Code=="PREVIEW_GEOMETRY"&&(i.ControlKey==art.Key||i.ControlKey==locked.Key||i.ControlKey==unlocked.Key)),name+": supported geometry still unverified");
 string clean=Path.Combine(args[1],name+".mp1skinproj"),signature=ProjectFile.ContentSignature(p);ProjectFile.Save(p,clean);var loaded=ProjectFile.Load(clean);var ls=new PreviewState(loaded);Check(ProjectFile.ContentSignature(loaded)==signature&&new StackPreview(loaded,ls).Bounds(loaded.Controls.First(c=>c.Name=="ClearArt Filmstrip")).X==expected,name+": derived position changed after reopen");
 string key=art.Key,relative=art.Source.RelativePath,before=art.Source.Text;p.Edit(art,new Dictionary<string,string>{{"width",(art.Number("width",200)+9).ToString()}},"Filmstrip overlay width");string after=p.Controls.First(c=>c.Key==key).Source.Text,edited=Path.Combine(args[1],name+"-Edited.mp1skinproj"),zip=Path.Combine(args[1],name+".zip");ProjectFile.Save(p,edited);var restored=ProjectFile.Load(edited);restored.Export(zip);using(var archive=ZipFile.OpenRead(zip)){Check(archive.Entries.Count==2,name+": unexpected export entries");using(var reader=new StreamReader(archive.GetEntry(relative).Open()))Check(reader.ReadToEnd()==after,name+": exported source differs");}
 restored.UndoChange();Check(!restored.Dirty&&restored.Source(Path.Combine(restored.Root,relative)).Text==before,name+": restored undo failed");restored.RedoChange();Check(restored.Source(Path.Combine(restored.Root,relative)).Text==after,name+": restored redo failed");
 using(var canvas=new SkinCanvas{Project=loaded,State=ls,Grid=false,Size=new Size(1280,760)})using(var image=new Bitmap(1280,760)){canvas.DrawToBitmap(image,new Rectangle(0,0,1280,760));image.Save(Path.Combine(args[1],name+".png"));}
 foreach(var source in p.Sources.Values.Concat(restored.Sources.Values)){string hash=ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray());Check(ProjectFile.Digest(File.ReadAllBytes(source.Path))==hash,name+": original source changed");hashes[source.Path]=hash;}
 results.Add("PASS "+name+": "+unknown+" unverified visibility; thumbnail/overlay X "+start.X+" -> "+moved.X+", PIN icon X "+icon.X+"; scene/read-only geometry/navigation/save/reopen/export/undo/redo and original bindings preserved.");Console.WriteLine(results.Last());count++;
 }
 foreach(var pair in hashes)Check(ProjectFile.Digest(File.ReadAllBytes(pair.Key))==pair.Value,"Original source changed");results.Add("PASS "+count+" real Titan filmstrip workflows; "+hashes.Count+" distinct original source hashes unchanged. Six production canvas renders generated.");File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);Console.WriteLine(results.Last());return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
