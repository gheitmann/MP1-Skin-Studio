using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class PreviewReliabilityProbe {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static string ReportText(SkinAuditReport report){return String.Join("\n",report.Issues.Select(i=>i.Code+"|"+i.ControlKey+"|"+i.Property+"|"+i.Detail));}
 static int Main(string[] args){try{
 Directory.CreateDirectory(args[1]);var results=new List<string>();var hashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);int count=0,idleChecks=0;
 foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","settings.xml","myvideo.xml","mymusicsongs.xml","mypics.xml"})foreach(string mode in page=="BasicHome.xml"||page=="settings.xml"?new[]{"auto"}:new[]{"list","filmstrip","coverflow"}){
 string label=(theme.Length==0?"Default":theme)+"/"+page+"/"+mode;var p=new SkinProject(args[0]);p.Theme=theme;PreparedSkinSession.Create(p,page);p.Editor.Language.Primary=LanguagePack.Load(args[2]);p.Editor.Language.Prefixes=false;
 if(page=="BasicHome.xml")BasicHomePreview.Apply(p);else if(page=="settings.xml")PlaybackPreview.Stopped(p.Editor.Preview);else MediaFolderPreview.Apply(p,page=="myvideo.xml"?"video":page=="mymusicsongs.xml"?"music":"pictures");
 p.Editor.Collection.Mode=mode;var state=new PreviewState(p);string signature=ProjectFile.ContentSignature(p);var report=PreviewProblems.Run(p,state);Check(ProjectFile.ContentSignature(p)==signature&&!p.Dirty&&p.Undo.Count==0,label+": report changed session");
 var unknown=p.Controls.Where(c=>state.Visible(c)==null).ToArray();Check(unknown.All(c=>report.Issues.Any(i=>i.Code=="PREVIEW_CONDITION"&&i.ControlKey==c.Key)),label+": unexplained unknown condition");Check(report.Warnings==0,label+": preview uncertainty reported as runtime error");
 if(page=="BasicHome.xml")Check(report.Issues.Any(i=>i.Detail.Contains("Check the spelling of Control.IsVisible")),label+": actual skin typo was not identified");
 if(mode=="list"){int expected=page=="myvideo.xml"?1:page=="mymusicsongs.xml"?4:0;Check(unknown.Length==expected,label+": unexpected known/unknown visibility count");}
 foreach(var control in p.Controls.Where(c=>c.Get("visible").IndexOf("system.idletime",StringComparison.OrdinalIgnoreCase)>=0)){
 bool? before=state.Visible(control);p.Editor.Preview.Values[IdlePreview.Key]="60";bool? idle=state.Visible(control);p.Editor.Preview.Values[IdlePreview.Key]="0";if(mode=="list"&&page=="myvideo.xml"){if(before==false&&idle==true)idleChecks++;else Console.WriteLine("INFO "+label+": idle control "+control.Get("id")+" remains gated ("+before+" -> "+idle+"): "+control.Get("visible"));}
 }
 if(mode=="list"&&page=="myvideo.xml")Check(p.Controls.Any(c=>c.Get("visible").IndexOf("system.idletime",StringComparison.OrdinalIgnoreCase)>=0),label+": no real idle condition exercised");Check(ProjectFile.ContentSignature(p)==signature,label+": idle inspection leaked sample changes");
 string basename=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page)+"-"+mode;string clean=Path.Combine(args[1],basename+".mp1skinproj");ProjectFile.Save(p,clean);
 File.WriteAllText(Path.Combine(args[1],basename+"-Problems.txt"),ReportText(report));
 var selected=p.LogicalControls().First(c=>SkinProject.Focusable(c)&&SkinProject.ControlId(c)==state.FocusId);if(selected.Get("type")=="facadeview")selected=CollectionSettings.Template(p,selected,mode);string key=selected.Key,relative=selected.Source.RelativePath,beforeXml=selected.Source.Text;
 p.Edit(selected,new Dictionary<string,string>{{"width",((int)selected.Number("width",500)+13).ToString()}},"Preview checkpoint width");string after=p.Controls.First(c=>c.Key==key).Source.Text;string expectedReport=ReportText(PreviewProblems.Run(p,state)),file=Path.Combine(args[1],basename+"-Edited.mp1skinproj"),zip=Path.ChangeExtension(file,".zip");signature=ProjectFile.ContentSignature(p);ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var restored=PreparedSkinSession.Create(loaded,loaded.Page);
 Check(ProjectFile.ContentSignature(loaded)==signature&&ReportText(PreviewProblems.Run(loaded,restored.Preview))==expectedReport,label+": report/sample changed after reopen");loaded.Export(zip);
 using(var archive=ZipFile.OpenRead(zip)){Check(archive.Entries.Count==2,label+": unexpected export entries");using(var reader=new StreamReader(archive.GetEntry(relative).Open()))Check(reader.ReadToEnd()==after,label+": export mismatch");}
 loaded.UndoChange();Check(!loaded.Dirty&&loaded.Source(Path.Combine(loaded.Root,relative)).Text==beforeXml,label+": restored undo mismatch");loaded.RedoChange();var redoText=loaded.Source(Path.Combine(loaded.Root,relative)).Text;if(redoText!=after){File.WriteAllText(Path.Combine(args[1],"Expected.xml"),after);File.WriteAllText(Path.Combine(args[1],"Actual.xml"),redoText);}Check(redoText==after,label+": restored redo mismatch ("+relative+", expected "+after.Length+", actual "+redoText.Length+")");p.UndoChange();Check(!p.Dirty&&p.Source(Path.Combine(p.Root,relative)).Text==beforeXml,label+": undo mismatch");p.RedoChange();Check(p.Source(Path.Combine(p.Root,relative)).Text==after,label+": redo mismatch");
 foreach(var source in p.Sources.Values){string hash=ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray());Check(ProjectFile.Digest(File.ReadAllBytes(source.Path))==hash,label+": original modified");hashes[source.Path]=hash;}
 results.Add("PASS "+label+": "+unknown.Length+" unverified visibility, "+report.Issues.Count+" explained preview findings; read-only report, exact profile/report/edit/save/reopen/export/undo/redo.");Console.WriteLine(results.Last());count++;
 }
 Check(idleChecks>=2,"No visible real idle transition in both themes");foreach(var hash in hashes)Check(ProjectFile.Digest(File.ReadAllBytes(hash.Key))==hash.Value,"Final original hash mismatch");results.Add("PASS "+count+" real Titan workflows, "+idleChecks+" explicit idle transitions; "+hashes.Count+" distinct original source hashes unchanged.");File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);Console.WriteLine(results.Last());return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}


