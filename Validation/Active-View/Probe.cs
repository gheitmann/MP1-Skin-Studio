using System;
using System.IO;
using System.Linq;
using MP1SkinStudio;
class ActiveViewProbe {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static int Main(string[] args){try{
 Directory.CreateDirectory(args[1]);var p=new SkinProject(args[0]);PreparedSkinSession.Create(p,"BasicHome.xml");string baseline=ProjectFile.ContentSignature(p),xml=ProjectFile.XmlSignature(p);int count=0;
 foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","settings.xml"}){
 p.PrepareTheme(theme);PreparedSkinSession.Create(p,page);string expected=ProjectFile.ContentSignature(p);Check((page=="BasicHome.xml"&&theme=="")== (expected==baseline),"Active location not distinguished");
 string file=Path.Combine(args[1],"View"+count+".mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);PreparedSkinSession.Create(loaded,loaded.Page);
 Check(loaded.Page==page&&loaded.Theme==theme&&ProjectFile.ContentSignature(loaded)==expected,"Reopened location mismatch");Check(ProjectFile.XmlSignature(loaded)==xml&&!loaded.Dirty&&loaded.Undo.Count==0,"View transition edited XML");count++;
 }
 p.PrepareTheme("");PreparedSkinSession.Create(p,"BasicHome.xml");Check(ProjectFile.ContentSignature(p)==baseline,"Returning to initial location remained dirty");
 foreach(var s in p.Sources.Values)Check(ProjectFile.Digest(File.ReadAllBytes(s.Path))==ProjectFile.Digest(s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray()),"Original source changed");
 string result="PASS four real Titan active-view save/reopen combinations (BasicHome/Settings, Default/Christmas); distinct location signatures, exact return to baseline, unchanged XML/export signatures and empty undo history. All "+p.Sources.Count+" loaded source hashes unchanged.";File.WriteAllText(Path.Combine(args[1],"Results.txt"),result);Console.WriteLine(result);return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
