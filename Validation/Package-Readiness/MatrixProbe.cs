using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MP1SkinStudio;
class PackageMatrixProbe
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Move(SkinProject p,PreviewState state,string direction,int expected){var target=p.PreviewNavigationTarget(state,direction);Check(SkinProject.ControlId(target)==expected,"Route "+direction+" from "+state.FocusId+" expected "+expected);state.SetFocus(expected);}
 static int Main(string[] args){try{return Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static int Run(string[] args)
 {
  string package=Path.GetFullPath(args[0]),root=Path.Combine(package,"Samples/Titan-Sample"),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);var lines=new List<string>();var hashes=new Dictionary<string,string>();
  foreach(string row in File.ReadAllLines(Path.Combine(package,"MANIFEST.sha256"))){int split=row.IndexOf("  ",StringComparison.Ordinal);Check(split==64,"Invalid manifest record");string name=row.Substring(split+2),path=Path.Combine(package,name),hash=ProjectFile.Digest(File.ReadAllBytes(path));Check(hash==row.Substring(0,split),"Package hash mismatch: "+name);hashes.Add(path,hash);}lines.Add("PASS "+hashes.Count+" manifest payload hashes before acceptance.");
  string[] pages=Directory.GetFiles(root,"demo*.xml").Select(Path.GetFileName).Where(p=>p!="demo.menu.xml").OrderBy(p=>p).ToArray();Check(pages.Length==12,"Expected twelve complete demo pages");
  foreach(string page in pages)
  {
   var p=new SkinProject(root);var prepared=PreparedSkinSession.Create(p,page);var state=prepared.Preview;Check(p.Controls.Count>0&&!p.Dirty,"Unclean page load "+page);string before=ProjectFile.XmlSignature(p);
   if(page=="demo.xml"){state.SetFocus(10);Move(p,state,"onright",11);Move(p,state,"onleft",10);}
   if(page=="demo.grid.xml"){state.SetFocus(901);Move(p,state,"onright",902);Move(p,state,"ondown",904);Move(p,state,"onleft",903);Move(p,state,"onup",901);}
   if(page=="demo.stack.xml"){state.SetFocus(801);Move(p,state,"ondown",803);Move(p,state,"onup",801);}
   if(page=="demo.navigation.xml"||page=="demo.free.xml"){int a=page=="demo.free.xml"?851:801;state.SetFocus(a);Move(p,state,"onright",a+2);Move(p,state,"ondown",a+3);Move(p,state,"onleft",a+1);Move(p,state,"onup",a);}
   if(page=="demo.checkmark.xml"){state.SetFocus(881);foreach(int id in new[]{882,883,884})Move(p,state,"ondown",id);foreach(int id in new[]{883,882,881})Move(p,state,"onup",id);Check(PreviewProblems.Run(p,state).Issues.Count(i=>i.Code=="PREVIEW_CHECKMARK")==1,"Expected one deliberate unknown checkmark state");}
   if(page=="demo.filmstrip.xml"){Check(p.TryMoveCollectionSelection(state,"onright")&&p.TryMoveCollectionSelection(state,"onright"),"Filmstrip sample movement failed");var overlay=p.Controls.Single(c=>SkinProject.ControlId(c)==601);Check(new StackPreview(p,state).Bounds(overlay).X==630&&overlay.IsBound("posX"),"Filmstrip binding/focus coordinate regressed");}
   if(page=="demo.collections.xml"){p.Editor.Collection.Mode="list";state=new PreviewState(p);state.SetFocus(50);Check(p.TryMoveCollectionSelection(state,"ondown"),"Collection list sample movement failed");}
   Check(ProjectFile.XmlSignature(p)==before&&!p.Dirty&&p.Undo.Count==0,"Preview navigation mutated XML: "+page);
   var editable=p.Controls.First(c=>!LayoutTools.IsContainer(c)&&!c.IsBound("width")&&c.Number("width",0)>0&&p.Controls.Count(x=>x.Key==c.Key)==1);string key=editable.Key,sourcePath=editable.Source.RelativePath,original=editable.Source.Text;
   p.Edit(editable,new Dictionary<string,string>{{"width",(editable.Number("width",0)+7).ToString(CultureInfo.InvariantCulture)}},"Package acceptance width");string after=p.Controls.Single(c=>c.Key==key).Source.Text;
   string stem=Path.GetFileNameWithoutExtension(page),project=Path.Combine(output,stem+".mp1skinproj"),zip=Path.Combine(output,stem+".zip");ProjectFile.Save(p,project);var loaded=ProjectFile.Load(project);Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded),"Save/reopen signature changed: "+page);Check(new PreviewState(loaded).FocusId==state.FocusId,"Focus changed on reopening: "+page);loaded.Export(zip);
   using(var archive=ZipFile.OpenRead(zip)){Check(archive.Entries.Count==2,"Unexpected export entries: "+page);using(var reader=new StreamReader(archive.GetEntry(sourcePath).Open()))Check(reader.ReadToEnd()==after,"Export XML mismatch: "+page);}
   p.UndoChange();Check(p.Controls.Single(c=>c.Key==key).Source.Text==original&&!p.Dirty,"Normal undo failed: "+page);p.RedoChange();Check(p.Controls.Single(c=>c.Key==key).Source.Text==after,"Normal redo failed: "+page);loaded.UndoChange();Check(loaded.Controls.Single(c=>c.Key==key).Source.Text==original&&!loaded.Dirty,"Restored undo failed: "+page);loaded.RedoChange();Check(loaded.Controls.Single(c=>c.Key==key).Source.Text==after,"Restored redo failed: "+page);
   lines.Add("PASS "+page+": load/preview/width edit/save/reopen/exact export/normal and restored Undo/Redo; "+p.Controls.Count+" expanded controls.");
  }
  foreach(var hash in hashes)Check(ProjectFile.Digest(File.ReadAllBytes(hash.Key))==hash.Value,"Payload changed during workflows: "+Path.GetFileName(hash.Key));lines.Add("PASS all "+hashes.Count+" payload originals unchanged. No installed skin, language or client required.");
  File.WriteAllLines(Path.Combine(output,"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);return 0;
 }
}
