using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MP1SkinStudio;
class GroupWorkflowProbe {
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static int Main(string[] args){try{
 Directory.CreateDirectory(args[1]);int passed=0;
 foreach(string theme in new[]{"","Christmas"}){
 var p=new SkinProject(args[0]);p.PrepareTheme(theme);PreparedSkinSession.Create(p,"settings.xml");var original=p.Controls.Single(c=>c.Get("type")=="group"&&c.Name=="buttons");string originalText=original.Source.Text,signature=ProjectFile.ContentSignature(p);
 string key=p.DuplicateControl(original,false);var clone=p.Controls.Single(c=>c.Key==key);var children=p.Controls.Where(c=>c.RuntimeParent==clone).ToArray();Check(children.Length==11,"Settings clone lost children");
 Check(children.All(c=>Int32.Parse(c.Get("id"))>=10000)&&children.Select(c=>c.Get("id")).Distinct().Count()==11,"Clone IDs collide");
 int[] expected=children.Select(c=>Int32.Parse(c.Get("id"))).ToArray();var state=new PreviewState(p);state.SetFocus(expected[0]);
 for(int i=1;i<=expected.Length;i++){var target=p.PreviewNavigationTarget(state,"ondown");Check(SkinProject.ControlId(target)==expected[i%expected.Length],"Cloned down loop left clone");state.SetFocus(SkinProject.ControlId(target).Value);}
 for(int i=expected.Length-1;i>=0;i--){var target=p.PreviewNavigationTarget(state,"onup");Check(SkinProject.ControlId(target)==expected[i],"Cloned up loop left clone");state.SetFocus(SkinProject.ControlId(target).Value);}
 string file=Path.Combine(args[1],(theme.Length==0?"Default":theme)+"-Group.mp1skinproj"),zip=Path.ChangeExtension(file,".zip");string changed=clone.Source.Text;ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(loaded.Controls.Count==p.Controls.Count&&loaded.Controls.First(c=>c.Key==key).Source.Text==changed,"Reopened group changed");loaded.Export(zip);
 using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry(original.Source.RelativePath).Open()))Check(reader.ReadToEnd()==changed,"Export differs");
 loaded.UndoChange();Check(!loaded.Dirty&&loaded.Source(loaded.Resolve("settings.xml")).Text==originalText,"Restored undo failed");loaded.RedoChange();Check(loaded.Source(loaded.Resolve("settings.xml")).Text==changed,"Restored redo failed");
 p.UndoChange();Check(!p.Dirty&&p.Source(p.Resolve("settings.xml")).Text==originalText,"Undo failed");p.RedoChange();Check(p.Source(p.Resolve("settings.xml")).Text==changed,"Redo failed");
 foreach(var source in p.Sources.Values)Check(ProjectFile.Digest(File.ReadAllBytes(source.Path))==ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray()),"Original modified");
 passed++;Console.WriteLine("PASS "+(theme.Length==0?"Default":theme)+": 12-control Settings clone, 11-button down/up loops, save/reopen/export and both undo/redo paths; "+p.Sources.Count+" source hashes unchanged.");
 }
 Console.WriteLine("PASS "+passed+" complete real Titan group workflows.");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}

