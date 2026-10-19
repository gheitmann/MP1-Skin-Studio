using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class CollectionWorkflowProbe {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static int Main(string[] args){try{
 Directory.CreateDirectory(args[1]);var results=new List<string>();int count=0;var hashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"myvideo.xml","myvideoplaylist.xml","mymusicsongs.xml","mymusicplaylist.xml","mypics.xml"})foreach(string mode in new[]{"list","smallicons","largeicons","filmstrip","coverflow","album","playlist"}){
 string label=(theme.Length==0?"Default":theme)+"/"+page+"/"+mode;var p=new SkinProject(args[0]);p.Theme=theme;PreparedSkinSession.Create(p,page);VideoFolderPreview.Apply(p);p.Editor.Collection.Mode=mode;p.Editor.Collection.Items.Clear();
 for(int i=0;i<16;i++)p.Editor.Collection.Items.Add(new PreviewItem{Title="Sample "+(i+1),Detail="Detail "+(i+1),Image=i%2==0?"DefaultFolderBig.png":"defaultAudioBig.png"});
 var state=new PreviewState(p);state.SetFocus(50);var facade=p.LogicalControls().Single(c=>SkinProject.ControlId(c)==50);var control=facade.Get("type")=="facadeview"?CollectionSettings.Template(p,facade,mode):facade;
 Check(control!=null&&state.Visible(control)==true,label+": active collection is not proven visible");string kind=CollectionSettings.Kind(control,mode),xml=ProjectFile.XmlSignature(p),before=control.Source.Text;
 var layout=new StackPreview(p,state);var bounds=layout.Bounds(control);Check(CollectionGeometry.ValidBounds(bounds),label+": invalid viewport");
 for(int i=0;i<16;i++){p.Editor.Collection.Focus=i;state.UpdateCollectionValues();var tiles=CollectionGeometry.Tiles(control,bounds,p.Editor.Collection);Check(tiles.Count(t=>t.Selected)==1&&tiles.Single(t=>t.Selected).Bounds.IntersectsWith(bounds),label+": selected item disappeared at "+i);Check(state.Values["#selectedthumb"]==p.Editor.Collection.Items[i].Image,label+": thumbnail stale");}
 p.Editor.Collection.Focus=0;state.UpdateCollectionValues();string direction=kind=="list"||kind=="playlist"||kind=="album"?"ondown":kind=="smallicons"||kind=="largeicons"?CollectionGeometry.Columns(control,bounds,p.Editor.Collection)>1?"onright":"ondown":"onright";
 Check(p.TryMoveCollectionSelection(state,direction),label+": sample arrow failed");string reverse=direction=="onright"?"onleft":"onup";Check(p.TryMoveCollectionSelection(state,reverse)&&p.Editor.Collection.Focus==0,label+": reverse sample arrow failed");
 Check(!p.Dirty&&p.Undo.Count==0&&ProjectFile.XmlSignature(p)==xml,label+": preview edited XML");p.Editor.Collection.Focus=5;state.UpdateCollectionValues();string key=control.Key,relative=control.Source.RelativePath;int width=(int)control.Number("width",500)+17;p.Edit(control,new Dictionary<string,string>{{"width",width.ToString()}},"Collection width");
 string after=p.Controls.First(c=>c.Key==key).Source.Text,file=Path.Combine(args[1],"Case"+count+".mp1skinproj"),zip=Path.ChangeExtension(file,".zip"),signature=ProjectFile.ContentSignature(p);ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var reopened=PreparedSkinSession.Create(loaded,loaded.Page);
 Check(ProjectFile.ContentSignature(loaded)==signature&&loaded.Editor.Collection.Mode==mode&&loaded.Editor.Collection.Focus==5&&reopened.Preview.Values["#selecteditem"]=="Sample 6",label+": project state lost");loaded.Export(zip);
 using(var archive=ZipFile.OpenRead(zip)){Check(archive.Entries.Count==2,label+": unexpected export files");using(var reader=new StreamReader(archive.GetEntry(relative).Open()))Check(reader.ReadToEnd()==after,label+": export mismatch");}
 loaded.UndoChange();Check(!loaded.Dirty&&loaded.Source(Path.Combine(loaded.Root,relative)).Text==before,label+": restored undo failed");loaded.RedoChange();Check(loaded.Source(Path.Combine(loaded.Root,relative)).Text==after,label+": restored redo failed");p.UndoChange();Check(!p.Dirty&&p.Source(Path.Combine(p.Root,relative)).Text==before,label+": undo failed");p.RedoChange();Check(p.Source(Path.Combine(p.Root,relative)).Text==after,label+": redo failed");
 foreach(var source in p.Sources.Values){string original=ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray());Check(ProjectFile.Digest(File.ReadAllBytes(source.Path))==original,label+": source overwritten");hashes[source.Path]=original;}
 string result="PASS "+label+" -> "+kind+": 16 visible selections, bidirectional sample arrows, exact edit/save/reopen/export/undo/redo";results.Add(result);Console.WriteLine(result);count++;
 }
 foreach(var entry in hashes)Check(ProjectFile.Digest(File.ReadAllBytes(entry.Key))==entry.Value,"Final source hash changed");results.Add("PASS "+count+" workflows; "+hashes.Count+" distinct original source hashes unchanged.");File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);Console.WriteLine(results.Last());return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
