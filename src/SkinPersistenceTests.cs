using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject SaveFixture(string root,string name)
        {
            string path=Path.Combine(root,name);Directory.CreateDirectory(path);Directory.CreateDirectory(Path.Combine(path,"Media"));File.WriteAllBytes(Path.Combine(path,"Media","unchanged.bin"),new byte[]{1,2,3,4});
            Write(path,"main.xml","<window><id>0</id><controls><control><type>label</type><id>1</id><label>Original</label></control></controls></window>");
            Write(path,"second.xml","<window><id>2</id><controls><control><type>label</type><id>2</id><label>Second</label></control></controls></window>");
            var p=new SkinProject(path);p.OpenPage("main.xml");return p;
        }
        static string SaveCopy(SkinProject p,string root,string name){return SkinSavePlan.Capture(p).CreateCopy(root,name,CancellationToken.None);}
        static void RunSkinPersistenceTests(string root)
        {
            Test("Saved skin first copy includes applied XML and all media without changing source or undo",()=>
            {
                var p=SaveFixture(root,"persist-first-source");string before=File.ReadAllText(Path.Combine(p.Root,"main.xml"));p.Edit(p.Controls[0],Set("label","Saved text"),"Edit");string signature=ProjectFile.ContentSignature(p);int undo=p.Undo.Count;string target=SaveCopy(p,root,"persist-first-output");
                Check(File.ReadAllText(Path.Combine(target,"main.xml")).Contains("Saved text")&&File.ReadAllBytes(Path.Combine(target,"Media","unchanged.bin")).SequenceEqual(new byte[]{1,2,3,4}),"Copy is incomplete.");
                Check(File.ReadAllText(Path.Combine(p.Root,"main.xml"))==before&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==undo,"Copy changed source or history.");Check(SkinSavePlan.Capture(p,target).ChangeCount==0,"First copy is not recorded as saved.");
            });
            Test("Saved skin repeated update replaces only reviewed XML and creates exact backups",()=>
            {
                var p=SaveFixture(root,"persist-repeat-source");string target=SaveCopy(p,root,"persist-repeat-output");byte[] before=File.ReadAllBytes(Path.Combine(target,"main.xml")),second=File.ReadAllBytes(Path.Combine(target,"second.xml"));p.Edit(p.Controls[0],Set("label","Updated"),"Edit");var plan=SkinSavePlan.Capture(p,target);string backup=plan.Save();
                Check(plan.ChangeCount==1&&File.ReadAllBytes(Path.Combine(target,"main.xml")).SequenceEqual(p.Controls[0].Source.Bytes())&&File.ReadAllBytes(Path.Combine(target,"second.xml")).SequenceEqual(second),"Unreviewed files changed.");
                using(var archive=ZipFile.OpenRead(backup))using(var input=archive.GetEntry("main.xml").Open())using(var memory=new MemoryStream()){input.CopyTo(memory);Check(memory.ToArray().SequenceEqual(before)&&archive.GetEntry(SkinSavePlan.ReceiptName)!=null,"Backup is not exact.");}
                Check(SkinSavePlan.Capture(p,target).Save()==null,"Unchanged update creates extra backups.");
            });
            Test("Saved skin Undo can restore a previously saved file to the original bytes",()=>
            {
                var p=SaveFixture(root,"persist-undo-source");string original=File.ReadAllText(Path.Combine(p.Root,"main.xml"));p.Edit(p.Controls[0],Set("label","First save"),"Edit");string target=SaveCopy(p,root,"persist-undo-output");p.UndoChange();var plan=SkinSavePlan.Capture(p,target);Check(plan.ChangeCount==1&&plan.Review().Length==1,"Undo was omitted from the target review.");plan.Save();Check(File.ReadAllText(Path.Combine(target,"main.xml"))==original&&!p.Dirty&&p.Redo.Count==1,"Saving Undo changed original bytes or history.");
            });
            Test("Saved skin conflicts block all files and preserve external target edits",()=>
            {
                var p=SaveFixture(root,"persist-conflict-source");string target=SaveCopy(p,root,"persist-conflict-output");p.Edit(p.Controls[0],Set("label","Pending"),"Edit");string file=Path.Combine(target,"main.xml");File.AppendAllText(file,"\n<!--external-->");byte[] external=File.ReadAllBytes(file);var plan=SkinSavePlan.Capture(p,target);Check(plan.HasConflicts&&!plan.Review()[0].SourceMatches,"Conflict missing from review.");Reject(()=>plan.Save());Check(File.ReadAllBytes(file).SequenceEqual(external),"External changes overwritten.");
            });
            Test("Saved skin drift after review is rejected without replacing any XML",()=>
            {
                var p=SaveFixture(root,"persist-late-source");string target=SaveCopy(p,root,"persist-late-output");p.Edit(p.Controls[0],Set("label","Pending"),"Edit");var plan=SkinSavePlan.Capture(p,target);string file=Path.Combine(target,"main.xml");File.AppendAllText(file,"\n<!--late-->");byte[] external=File.ReadAllBytes(file);Reject(()=>plan.Save());Check(File.ReadAllBytes(file).SequenceEqual(external),"Late target drift overwritten.");
            });
            Test("Saved skin failure on a later file rolls earlier replacements back byte for byte",()=>
            {
                var p=SaveFixture(root,"persist-rollback-source");string target=SaveCopy(p,root,"persist-rollback-output");p.Edit(p.Controls[0],Set("label","First edit"),"Edit");p.OpenPage("second.xml");p.Edit(p.Controls[0],Set("label","Second edit"),"Edit");var plan=SkinSavePlan.Capture(p,target);byte[] first=File.ReadAllBytes(Path.Combine(target,"main.xml")),receipt=File.ReadAllBytes(Path.Combine(target,SkinSavePlan.ReceiptName));
                using(var locked=new FileStream(Path.Combine(target,"second.xml"),FileMode.Open,FileAccess.Read,FileShare.Read))Reject(()=>plan.Save());
                Check(File.ReadAllBytes(Path.Combine(target,"main.xml")).SequenceEqual(first)&&File.ReadAllBytes(Path.Combine(target,SkinSavePlan.ReceiptName)).SequenceEqual(receipt),"Failed transaction did not restore prior bytes.");Check(!Directory.GetFiles(target,"*.tmp",SearchOption.AllDirectories).Any()&&!Directory.GetFiles(target,"*.rollback",SearchOption.AllDirectories).Any(),"Temporary transaction files remain.");
            });
            Test("Saved skin stale receipts prevent concurrent saves from overwriting one another",()=>
            {
                var p=SaveFixture(root,"persist-receipt-source");string target=SaveCopy(p,root,"persist-receipt-output");p.Edit(p.Controls[0],Set("label","A"),"Edit");var first=SkinSavePlan.Capture(p,target);p.Edit(p.Controls[0],Set("label","B"),"Edit");var second=SkinSavePlan.Capture(p,target);first.Save();Reject(()=>second.Save());Check(File.ReadAllText(Path.Combine(target,"main.xml")).Contains(">A<"),"Stale writer replaced the newer receipt.");
            });
            Test("Saved skin copy identity rejects sources, unrelated folders and changed source baselines",()=>
            {
                var p=SaveFixture(root,"persist-identity-source");string target=SaveCopy(p,root,"persist-identity-output");Reject(()=>SkinSavePlan.Capture(p,p.Root));Reject(()=>SkinSavePlan.Capture(p,root));var other=SaveFixture(root,"persist-other-source");Reject(()=>SkinSavePlan.Capture(other,target));
                p.Edit(p.Controls[0],Set("label","Saved"),"Edit");SkinSavePlan.Capture(p,target).Save();File.AppendAllText(Path.Combine(p.Root,"main.xml"),"\n<!--new base-->");var fresh=new SkinProject(p.Root);fresh.OpenPage("main.xml");Check(SkinSavePlan.Capture(fresh,target).HasConflicts,"Changed editor baseline accepted.");
            });
            Test("Saved skin snapshots keep reviewed bytes even if the editor changes afterwards",()=>
            {
                var p=SaveFixture(root,"persist-snapshot-source");string target=SaveCopy(p,root,"persist-snapshot-output");p.Edit(p.Controls[0],Set("label","Reviewed"),"Edit");var plan=SkinSavePlan.Capture(p,target);p.Edit(p.Controls[0],Set("label","Later"),"Edit");plan.Save();Check(File.ReadAllText(Path.Combine(target,"main.xml")).Contains(">Reviewed<")&&p.Controls[0].Get("label")=="Later","Save wrote unreviewed state.");
            });
            Test("Saved skin XML review identifies exact target and desired byte hashes",()=>
            {
                var p=SaveFixture(root,"persist-review-source");string target=SaveCopy(p,root,"persist-review-output");p.Edit(p.Controls[0],Set("label","Changed & text"),"Edit");var plan=SkinSavePlan.Capture(p,target);var review=plan.Review().Single();Check(review.OriginalHash==ProjectFile.Digest(File.ReadAllBytes(Path.Combine(target,"main.xml")))&&review.EditedHash==ProjectFile.Digest(p.Controls[0].Source.Bytes())&&review.SourceMatches,"Review hashes are inaccurate.");
            });
            Test("Saved skin UTF16 updates preserve encoding, BOM and unrelated comments",()=>
            {
                var p=SaveFixture(root,"persist-utf16-source");string path=Path.Combine(p.Root,"main.xml");File.WriteAllText(path,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><id>0</id><controls><!--retain--><control><type>label</type><id>1</id><label>Ä</label></control></controls></window>",new UnicodeEncoding(false,true));p=new SkinProject(p.Root);p.OpenPage("main.xml");string target=SaveCopy(p,root,"persist-utf16-output");p.Edit(p.Controls[0],Set("label","Ö"),"Edit");SkinSavePlan.Capture(p,target).Save();byte[] actual=File.ReadAllBytes(Path.Combine(target,"main.xml"));Check(actual[0]==255&&actual[1]==254&&actual.SequenceEqual(p.Controls[0].Source.Bytes())&&File.ReadAllText(Path.Combine(target,"main.xml")).Contains("<!--retain-->"),"Encoding or comment changed.");
            });
            Test("Saved skin locations round trip in project format 5 without XML changes",()=>
            {
                var p=SaveFixture(root,"persist-project-source");string target=SaveCopy(p,root,"persist-project-output"),before=ProjectFile.EditorSignature(p);p.Editor.SavedSkinRoot=target;Check(ProjectFile.EditorSignature(p)!=before,"Output location omitted from project signature.");string file=Path.Combine(root,"persist-project.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(loaded.Editor.SavedSkinRoot==target&&!loaded.Dirty&&ProjectFile.EditorSignature(loaded)==ProjectFile.EditorSignature(p),"Output location or signatures lost.");
            });
            Test("Saved skin backups and receipts do not appear as skin XML pages",()=>
            {
                var p=SaveFixture(root,"persist-pages-source");string target=SaveCopy(p,root,"persist-pages-output");p.Edit(p.Controls[0],Set("label","Edited"),"Edit");SkinSavePlan.Capture(p,target).Save();Check(Directory.GetFiles(target,"*.xml",SearchOption.AllDirectories).Length==2,"Save metadata creates extra XML pages.");
            });
            Test("Saved skin invalid receipt XML and traversal are rejected",()=>
            {
                var p=SaveFixture(root,"persist-invalid-source");string target=SaveCopy(p,root,"persist-invalid-output");string path=Path.Combine(target,SkinSavePlan.ReceiptName);string valid=File.ReadAllText(path);File.WriteAllText(path,"<!DOCTYPE skinCopy [<!ENTITY x SYSTEM 'file:///private'>]><skinCopy>&x;</skinCopy>");Reject(()=>SkinSavePlan.Capture(p,target));var doc=new XmlDocument();doc.LoadXml(valid);var node=doc.CreateElement("file");node.SetAttribute("path","../outside.xml");node.SetAttribute("original",new string('a',64));node.SetAttribute("saved",new string('a',64));doc.DocumentElement.AppendChild(node);doc.Save(path);Reject(()=>SkinSavePlan.Capture(p,target));
            });
            Test("Saved skin read-only target is reported before update",()=>
            {
                var p=SaveFixture(root,"persist-readonly-source");string target=SaveCopy(p,root,"persist-readonly-output"),path=Path.Combine(target,"main.xml");p.Edit(p.Controls[0],Set("label","New"),"Edit");File.SetAttributes(path,File.GetAttributes(path)|FileAttributes.ReadOnly);try{var plan=SkinSavePlan.Capture(p,target);Check(plan.HasConflicts&&plan.Files.Single().Status.Contains("read-only"),"Read-only target was not reported.");Reject(()=>plan.Save());}finally{File.SetAttributes(path,File.GetAttributes(path)&~FileAttributes.ReadOnly);}
            });
            Test("Saved skin output locations reject invalid project state before saving",()=>
            {
                var p=SaveFixture(root,"persist-bad-location-source");p.Editor.SavedSkinRoot="../outside";Reject(()=>ProjectFile.EditorSignature(p));Reject(()=>ProjectFile.Save(p,Path.Combine(root,"invalid-output-location.mp1skinproj")));
            });
        }
    }
}
