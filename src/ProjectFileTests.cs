using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject SessionFixture(string root,string name)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);
            Write(folder,"main.xml","<window><id>0</id><controls><control><type>button</type><id>10</id><posX>50</posX></control><import>part.xml</import></controls></window>");
            Write(folder,"part.xml","<window><controls><control><type>label</type><id>0</id><posX>100</posX><label>Ä &amp; Ö</label><!--keep--><extra value=\"x\"/></control></controls></window>");
            var project=new SkinProject(folder);project.OpenPage("main.xml");return project;
        }
        static void RewriteManifest(string file,Func<string,string> change)
        {
            using(var archive=ZipFile.Open(file,ZipArchiveMode.Update))
            {
                var entry=archive.GetEntry("project.xml");string text;using(var reader=new StreamReader(entry.Open()))text=reader.ReadToEnd();entry.Delete();
                var replacement=archive.CreateEntry("project.xml");using(var writer=new StreamWriter(replacement.Open(),new UTF8Encoding(false)))writer.Write(change(text));
            }
        }
        static void RunProjectFileTests(string root)
        {
            Test("Active page changes are saved editor state without XML edits",()=>
            {
                var p=SessionFixture(root,"active-page");string initial=ProjectFile.ContentSignature(p),xml=ProjectFile.XmlSignature(p);
                PreparedSkinSession.Create(p,"part.xml");Check(ProjectFile.ContentSignature(p)!=initial,"Page switch was not detected as unsaved.");
                string selected=ProjectFile.ContentSignature(p),file=Path.Combine(root,"active-page.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);PreparedSkinSession.Create(loaded,loaded.Page);
                Check(loaded.Page=="part.xml"&&ProjectFile.ContentSignature(loaded)==selected,"Saved active page was not restored cleanly.");
                PreparedSkinSession.Create(p,"main.xml");Check(ProjectFile.ContentSignature(p)==initial,"Returning to the original page creates a spurious change.");
                Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Page selection edited XML or history.");
            });
            Test("Active theme changes are saved editor state without XML edits",()=>
            {
                var p=SessionFixture(root,"active-theme");string folder=Path.Combine(p.Root,"Themes","Blue");Directory.CreateDirectory(folder);Write(folder,"main.xml","<window><controls/></window>");
                string initial=ProjectFile.ContentSignature(p),xml=ProjectFile.XmlSignature(p);p.PrepareTheme("Blue");Check(ProjectFile.ContentSignature(p)!=initial,"Theme switch was not detected as unsaved.");
                string selected=ProjectFile.ContentSignature(p),file=Path.Combine(root,"active-theme.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);PreparedSkinSession.Create(loaded,loaded.Page);
                Check(loaded.Theme=="Blue"&&ProjectFile.ContentSignature(loaded)==selected,"Saved active theme was not restored cleanly.");
                p.PrepareTheme("");Check(ProjectFile.ContentSignature(p)==initial,"Returning to the original theme creates a spurious change.");
                Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Theme selection edited XML or history.");
            });
            Test("Preview focus is retained independently across pages and themes",()=>
            {
                var p=SessionFixture(root,"focus-page-theme");new PreviewState(p).SetFocus(10);
                Write(p.Root,"other.xml","<window><defaultcontrol>20</defaultcontrol><controls><control><type>button</type><id>20</id></control><control><type>button</type><id>21</id></control></controls></window>");p.OpenPage("other.xml");new PreviewState(p).SetFocus(21);
                string theme=Path.Combine(p.Root,"Themes","Blue");Directory.CreateDirectory(theme);Write(theme,"main.xml","<window><defaultcontrol>10</defaultcontrol><controls><control><type>button</type><id>10</id></control><control><type>button</type><id>11</id></control></controls></window>");
                PreparedSkinSession.Create(p,"main.xml");var themed=p.PrepareTheme("Blue");themed.Preview.SetFocus(11);Check(p.PrepareTheme("").Preview.FocusId==10,"Theme focus replaced default-page focus.");
                Check(PreparedSkinSession.Create(p,"other.xml").Preview.FocusId==21,"Second page focus was lost.");
                var restoredFocus=PreparedSkinSession.Create(p,"main.xml").Preview;string unchanged=ProjectFile.EditorSignature(p);restoredFocus.SetFocus(10);Check(ProjectFile.EditorSignature(p)==unchanged,"Restoring an existing focus creates a spurious unsaved change.");PreparedSkinSession.Create(p,"other.xml");
                string file=Path.Combine(root,"focus-map.mp1skinproj"),signature=ProjectFile.ContentSignature(p);ProjectFile.Save(p,file);p=ProjectFile.Load(file);Check(ProjectFile.ContentSignature(p)==signature,"Focus map changed during persistence.");
                Check(PreparedSkinSession.Create(p,"main.xml").Preview.FocusId==10&&p.PrepareTheme("Blue").Preview.FocusId==11,"Reopened page/theme focus was lost.");
                Check(!p.Dirty&&p.Undo.Count==0,"Focus transitions edited source XML.");
            });
            Test("Legacy single-focus projects migrate and duplicate focus slots are rejected",()=>
            {
                var p=SessionFixture(root,"focus-legacy");new PreviewState(p).SetFocus(10);string file=Path.Combine(root,"focus-legacy.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,t=>System.Text.RegularExpressions.Regex.Replace(t,@"<focus\b[^>]*/>","").Replace(" focusTheme=\"\"",""));var loaded=ProjectFile.Load(file);Check(new PreviewState(loaded).FocusId==10,"Legacy focus did not migrate.");
                ProjectFile.Save(loaded,file);RewriteManifest(file,t=>{var d=new XmlDocument();d.LoadXml(t);var slot=d.SelectSingleNode("/skinStudioProject/editorState/focus");slot.ParentNode.AppendChild(slot.CloneNode(true));return d.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
            Test("A full focus map rejects a new preset before modifying sample values",()=>
            {
                var p=SessionFixture(root,"focus-map-limit");Write(p.Root,"BasicHome.xml","<window><id>35</id><defaultcontrol>4</defaultcontrol><controls><control><type>button</type><id>4</id></control></controls></window>");p.OpenPage("BasicHome.xml");
                for(int i=0;i<128;i++)p.Editor.RememberFocus("page"+i+".xml","",1);
                string before=ProjectFile.ContentSignature(p);Reject(()=>BasicHomePreview.Apply(p));Check(ProjectFile.ContentSignature(p)==before,"Rejected focus capacity changed the preview.");
            });
            Test("Saved preview focus survives session preparation without changing source XML",()=>
            {
                var p=SessionFixture(root,"saved-focus");string xml=ProjectFile.XmlSignature(p),initial=ProjectFile.EditorSignature(p);
                new PreviewState(p).SetFocus(p.DefaultControlId);Check(ProjectFile.EditorSignature(p)==initial,"Initial default focus creates an unsaved change.");
                new PreviewState(p).SetFocus(10);string path=Path.Combine(root,"focus.mp1skinproj");ProjectFile.Save(p,path);
                var loaded=ProjectFile.Load(path);var prepared=PreparedSkinSession.Create(loaded,loaded.Page);
                Check(prepared.Preview.FocusId==10&&loaded.Editor.PreviewFocusPage=="main.xml"&&ProjectFile.XmlSignature(loaded)==xml&&!loaded.Dirty,"Focus did not survive save/reopen.");
                prepared=PreparedSkinSession.Create(loaded,"part.xml");Check(prepared.Preview.FocusId==loaded.DefaultControlId,"Focus leaked into another page.");
                prepared=PreparedSkinSession.Create(loaded,"main.xml");Check(prepared.Preview.FocusId==10,"Returning to the saved page lost focus.");
                RewriteManifest(path,t=>t.Replace("focusId=\"10\"","focusId=\"-2\""));Reject(()=>ProjectFile.Load(path));
            });
            Test("Session preparation loads requested pages and default fallbacks before publication",()=>
            {
                var p=SessionFixture(root,"prepare-session");var prepared=PreparedSkinSession.Create(p,"MAIN.XML");
                Check(prepared.Project==p&&p.Page=="main.xml"&&prepared.Pages.Contains("part.xml")&&prepared.Preview.Project==p&&prepared.Preview.FocusId==p.DefaultControlId,"Prepared session is incomplete.");
                Write(p.Root,"myHome.xml","<window><id>35</id><controls/></window>");prepared=PreparedSkinSession.Create(p,"not-listed.xml");Check(p.Page=="myHome.xml"&&p.WindowId==35,"Fallback page was not prepared.");
                string empty=Path.Combine(root,"prepare-empty");Directory.CreateDirectory(empty);Write(empty,"references.xml","<controls/>");Reject(()=>PreparedSkinSession.Create(new SkinProject(empty),null));
            });
            Test("Failed theme preparation preserves the edited active session",()=>
            {
                var p=SessionFixture(root,"prepare-theme-failure");p.Edit(p.Controls[0],Set("posX","60"),"Move");var control=p.Controls[0];string signature=ProjectFile.ContentSignature(p);int count=p.Sources.Count;
                string theme=Path.Combine(p.Root,"Themes","Broken");Directory.CreateDirectory(theme);Write(theme,"main.xml","<window><controls>");
                foreach(string target in new[]{"Missing","Broken"}){Reject(()=>p.PrepareTheme(target));Check(p.Theme==""&&p.Page=="main.xml"&&p.Controls[0]==control&&p.Sources.Count==count&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==1,"Failed theme preparation damaged the session.");}
                p.UndoChange();Check(!p.Dirty,"History stopped working after theme failure.");
            });
            Test("Successful theme switching retains base edits and history",()=>
            {
                var p=SessionFixture(root,"prepare-theme-success");p.Edit(p.Controls[0],Set("posX","60"),"Move");string theme=Path.Combine(p.Root,"Themes","Night");Directory.CreateDirectory(theme);Write(theme,"main.xml","<window><id>9</id><controls><control><type>button</type><id>10</id><posX>300</posX></control></controls></window>");
                var prepared=p.PrepareTheme("Night");Check(p.Theme=="Night"&&p.Controls[0].Get("posX")=="300"&&prepared.Preview.Project==p&&p.Undo.Count==1,"Theme was not prepared.");p.PrepareTheme("");Check(p.Controls[0].Get("posX")=="60"&&p.Undo.Count==1,"Base edit disappeared while switching themes.");p.UndoChange();Check(!p.Dirty,"Base edit history was lost.");
            });
            Test("Failed project replacement preserves the previous archive and cleans temporary files",()=>
            {
                var p=SessionFixture(root,"project-write-failure");p.Edit(p.Controls[0],Set("posX","60"),"First");string path=Path.Combine(p.Root,"saved.mp1skinproj");ProjectFile.Save(p,path);byte[] before=File.ReadAllBytes(path);p.Edit(p.Controls[0],Set("posX","80"),"Second");string signature=ProjectFile.ContentSignature(p);
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))Reject(()=>ProjectFile.Save(p,path));
                Check(File.ReadAllBytes(path).SequenceEqual(before)&&ProjectFile.ContentSignature(p)==signature&&Directory.GetFiles(p.Root,"saved.mp1skinproj.*.tmp").Length==0,"Failed save changed old archive, model or temporary-file state.");Check(ProjectFile.Load(path).Controls[0].Get("posX")=="60","Previous archive is no longer usable.");
            });
            Test("Failed ZIP replacement preserves the previous export and cleans temporary files",()=>
            {
                var p=SessionFixture(root,"export-write-failure");p.Edit(p.Controls[0],Set("posX","60"),"First");string path=Path.Combine(p.Root,"saved.zip");p.Export(path);byte[] before=File.ReadAllBytes(path);p.Edit(p.Controls[0],Set("posX","80"),"Second");string signature=ProjectFile.ContentSignature(p);
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))Reject(()=>p.Export(path));
                Check(File.ReadAllBytes(path).SequenceEqual(before)&&ProjectFile.ContentSignature(p)==signature&&Directory.GetFiles(p.Root,"saved.zip.*.tmp").Length==0,"Failed export changed old archive, model or temporary-file state.");using(var zip=ZipFile.OpenRead(path))using(var reader=new StreamReader(zip.GetEntry("main.xml").Open()))Check(reader.ReadToEnd().Contains("<posX>60</posX>"),"Previous export is no longer usable.");
            });
            Test("Failed page loads preserve the current edited page and history",()=>
            {
                var p=SessionFixture(root,"page-load-failure");p.Edit(p.Controls[0],Set("posX","60"),"Move");
                string signature=ProjectFile.ContentSignature(p);var selected=p.Controls[0];var sources=p.Sources.ToArray();int undo=p.Undo.Count;
                foreach(string page in new[]{"missing.xml","broken.xml","../outside.xml"})
                {
                    Write(p.Root,"broken.xml","<window><controls>");Reject(()=>p.OpenPage(page));
                    Check(p.Page=="main.xml"&&p.Controls[0]==selected&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==undo&&p.Redo.Count==0,"Failed page loading damaged the current session.");
                    Check(p.Sources.Count==sources.Length&&sources.All(pair=>p.Sources[pair.Key]==pair.Value),"Failed load polluted the source cache.");
                }
                p.UndoChange();Check(!p.Dirty&&p.Controls[0].Get("posX")=="50","Undo no longer works after failed page loading.");
            });
            Test("Editor projects restore multiple modified XML files and exact comments",()=>
            {
                var p=SessionFixture(root,"project-roundtrip");var disk=File.ReadAllBytes(Path.Combine(p.Root,"part.xml"));
                p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("posX","60")),new ControlEdit(p.Controls[1],Set("label","A new title"))},"gemeinsam");
                string signature=ProjectFile.ContentSignature(p),path=Path.Combine(root,"roundtrip.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);
                Check(loaded.Controls[0].Get("posX")=="60"&&loaded.Controls[1].Get("label")=="A new title"&&loaded.Controls[1].Source.Text.Contains("<!--keep--><extra value=\"x\"/>"),"Project content was modified.");
                Check(signature==ProjectFile.ContentSignature(loaded)&&File.ReadAllBytes(Path.Combine(p.Root,"part.xml")).SequenceEqual(disk),"Signature differs or source file was overwritten.");
            });
            Test("Restored projects provide one combined undo/redo step",()=>
            {
                var p=SessionFixture(root,"project-undo");p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("posX","60")),new ControlEdit(p.Controls[1],Set("posX","120"))},"gemeinsam");
                string path=Path.Combine(root,"undo.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);Check(loaded.Undo.Count==1,"No shared restoration.");
                loaded.UndoChange();Check(!loaded.Dirty&&loaded.Controls[0].Get("posX")=="50","Undo is incomplete.");loaded.RedoChange();Check(loaded.Controls[1].Get("posX")=="120","Redo is incomplete.");
            });
            Test("Unmodified editor projects save the page and skin directory",()=>
            {
                var p=SessionFixture(root,"project-clean");string path=Path.Combine(root,"clean.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);
                Check(loaded.Page=="main.xml"&&loaded.Root==p.Root&&!loaded.Dirty&&loaded.Undo.Count==0,"Clean project was modified.");
            });
            Test("Projects can move to another skin directory with identical originals",()=>
            {
                var p=SessionFixture(root,"project-move-original");p.Edit(p.Controls[1],Set("posX","500"),"verschieben");string path=Path.Combine(root,"moved.mp1skinproj");ProjectFile.Save(p,path);
                string target=Path.Combine(root,"project-move-new");Directory.CreateDirectory(target);foreach(string xml in Directory.GetFiles(p.Root,"*.xml"))File.Copy(xml,Path.Combine(target,Path.GetFileName(xml)),true);
                var loaded=ProjectFile.Load(path,target);Check(loaded.Root==target&&loaded.Controls[1].Get("posX")=="500","Project migration failed.");
            });
            Test("Recovery uses a matching backup even while the changed original directory exists",()=>
            {
                var p=SessionFixture(root,"recover-existing");string backup=Path.Combine(root,"recover-matching");Directory.CreateDirectory(backup);
                foreach(string xml in Directory.GetFiles(p.Root,"*.xml"))File.Copy(xml,Path.Combine(backup,Path.GetFileName(xml)));
                p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("posX","60")),new ControlEdit(p.Controls[1],Set("label","Recovered"))},"Recover");
                string file=Path.Combine(root,"recover.mp1skinproj");ProjectFile.Save(p,file);File.AppendAllText(Path.Combine(p.Root,"main.xml"),"<!--external-->");
                int prompts=0;var loaded=ProjectFile.OpenWithRecovery(file,reason=>{prompts++;Check(reason.Contains("main.xml")&&reason.Contains("backup"),"Recovery reason lost the source context.");return backup;});
                Check(prompts==1&&loaded.Root==backup&&loaded.Controls[1].Get("label")=="Recovered"&&loaded.Undo.Count==1,"Recovery lost changes or history.");
                loaded.UndoChange();Check(!loaded.Dirty,"Recovered Undo failed.");loaded.RedoChange();Check(loaded.Controls[0].Get("posX")=="60","Recovered Redo failed.");
                string saved=Path.Combine(root,"recovered-location.mp1skinproj");ProjectFile.Save(loaded,saved);Check(ProjectFile.Inspect(saved).Root==backup,"New location was not saved.");
                Check(ProjectFile.OpenWithRecovery(saved,reason=>{throw new Exception("Repeated recovery prompt");}).Root==backup,"New location did not reopen directly.");
                Check(File.ReadAllText(Path.Combine(p.Root,"main.xml")).EndsWith("<!--external-->")&&!File.ReadAllText(Path.Combine(backup,"part.xml")).Contains("Recovered"),"Recovery overwrote a skin.");
            });
            Test("Recovery cancellation returns no project and preserves the archive",()=>
            {
                var p=SessionFixture(root,"recover-cancel");p.Edit(p.Controls[1],Set("label","Saved"),"Edit");string file=Path.Combine(root,"recover-cancel.mp1skinproj");ProjectFile.Save(p,file);byte[] archive=File.ReadAllBytes(file);File.Delete(Path.Combine(p.Root,"part.xml"));
                int prompts=0;Check(ProjectFile.OpenWithRecovery(file,reason=>{prompts++;return null;})==null&&prompts==1,"Cancel did not stop opening.");Check(File.ReadAllBytes(file).SequenceEqual(archive),"Cancel changed the saved project.");
            });
            Test("A mismatching backup is rejected without repeated folder requests",()=>
            {
                var p=SessionFixture(root,"recover-wrong");p.Edit(p.Controls[1],Set("label","Saved"),"Edit");string file=Path.Combine(root,"recover-wrong.mp1skinproj");ProjectFile.Save(p,file);File.AppendAllText(Path.Combine(p.Root,"part.xml"),"<!--external-->");int prompts=0;bool typed=false;
                try{ProjectFile.OpenWithRecovery(file,reason=>{prompts++;return p.Root;});}catch(ProjectSourceConflictException ex){typed=ex.RelativePath=="part.xml";}
                Check(typed&&prompts==1,"Wrong backup bypassed hashes or repeated the chooser.");
            });
            Test("Missing saved roots use the same bounded recovery path",()=>
            {
                var p=SessionFixture(root,"recover-missing");p.Edit(p.Controls[0],Set("posX","70"),"Edit");string file=Path.Combine(root,"recover-missing.mp1skinproj");ProjectFile.Save(p,file);string target=p.Root+"-moved";Directory.Move(p.Root,target);int prompts=0;
                var loaded=ProjectFile.OpenWithRecovery(file,reason=>{prompts++;Check(reason.Contains("unavailable"),"Missing-root reason is incorrect.");return target;});Check(prompts==1&&loaded.Root==target&&loaded.Controls[0].Get("posX")=="70","Missing-root recovery failed.");
            });
            Test("Valid projects and corrupted archives never request conflict recovery",()=>
            {
                var p=SessionFixture(root,"recover-corrupt");p.Edit(p.Controls[0],Set("posX","70"),"Edit");string file=Path.Combine(root,"recover-corrupt.mp1skinproj");ProjectFile.Save(p,file);int prompts=0;Func<string,string> resolver=reason=>{prompts++;return p.Root;};
                Check(ProjectFile.OpenWithRecovery(file,resolver)!=null&&prompts==0,"Healthy project requested a folder.");RewriteManifest(file,t=>t.Replace("editedSha256=\"","editedSha256=\"bad"));Reject(()=>ProjectFile.OpenWithRecovery(file,resolver));Check(prompts==0,"Corruption was treated as a source conflict.");
            });
            Test("External source changes prevent silent project restoration",()=>
            {
                var p=SessionFixture(root,"project-conflict");p.Edit(p.Controls[1],Set("posX","500"),"verschieben");string path=Path.Combine(root,"conflict.mp1skinproj");ProjectFile.Save(p,path);
                string source=Path.Combine(p.Root,"part.xml");File.AppendAllText(source,"<!--extern-->");byte[] current=File.ReadAllBytes(source);Reject(()=>ProjectFile.Load(path));Check(current.SequenceEqual(File.ReadAllBytes(source)),"Conflict resolution overwrites the original.");
            });
            Test("Corrupted edited snapshots are detected",()=>
            {
                var p=SessionFixture(root,"project-corrupt");p.Edit(p.Controls[1],Set("posX","500"),"verschieben");string path=Path.Combine(root,"corrupt.mp1skinproj");ProjectFile.Save(p,path);
                using(var archive=ZipFile.Open(path,ZipArchiveMode.Update)){var entry=archive.GetEntry("snapshots/000.edited.xml");entry.Delete();var bad=archive.CreateEntry("snapshots/000.edited.xml");using(var writer=new StreamWriter(bad.Open()))writer.Write("<window/>");}
                Reject(()=>ProjectFile.Load(path));
            });
            Test("Project path traversal is rejected before file access",()=>
            {
                var p=SessionFixture(root,"project-paths");p.Edit(p.Controls[1],Set("posX","500"),"verschieben");string path=Path.Combine(root,"paths.mp1skinproj");ProjectFile.Save(p,path);
                RewriteManifest(path,text=>text.Replace("path=\"part.xml\"","path=\"../escape.xml\""));Reject(()=>ProjectFile.Load(path));
            });
            Test("Project format versions and duplicate ZIP entries are validated",()=>
            {
                var p=SessionFixture(root,"project-format");string path=Path.Combine(root,"format.mp1skinproj");ProjectFile.Save(p,path);RewriteManifest(path,text=>{var doc=new XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","999");return doc.OuterXml;});Reject(()=>ProjectFile.Load(path));
                ProjectFile.Save(p,path);using(var archive=ZipFile.Open(path,ZipArchiveMode.Update))archive.CreateEntry("project.xml");Reject(()=>ProjectFile.Load(path));
            });
            Test("Project manifests reject DTDs and external entities",()=>
            {
                var p=SessionFixture(root,"project-dtd");string path=Path.Combine(root,"dtd.mp1skinproj");ProjectFile.Save(p,path);
                RewriteManifest(path,text=>"<!DOCTYPE skinStudioProject [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]>"+text);Reject(()=>ProjectFile.Inspect(path));
            });
            Test("Editor projects preserve UTF-16 and BOM during restoration",()=>
            {
                string folder=Path.Combine(root,"project-utf16");Directory.CreateDirectory(folder);string source=Path.Combine(folder,"main.xml");
                File.WriteAllText(source,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><controls><control><type>label</type><id>0</id><label>Ä</label></control></controls></window>",new UnicodeEncoding(false,true,true));
                var p=new SkinProject(folder);p.OpenPage("main.xml");p.Edit(p.Controls[0],Set("label","Ö"),"Text");string path=Path.Combine(root,"utf16.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);var bytes=loaded.Controls[0].Source.Bytes();
                Check(bytes[0]==255&&bytes[1]==254&&loaded.Controls[0].Get("label")=="Ö","UTF-16/BOM was lost.");
            });
            Test("Editor projects preserve the selected theme and theme XML",()=>
            {
                var p=SessionFixture(root,"project-theme");string folder=Path.Combine(p.Root,"Themes","Night");Directory.CreateDirectory(folder);Write(folder,"part.xml","<window><controls><control><type>label</type><id>0</id><label>Theme</label></control></controls></window>");
                p.Theme="Night";p.OpenPage("main.xml");p.Edit(p.Controls[1],Set("label","Theme bearbeitet"),"Text");string path=Path.Combine(root,"theme.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);
                Check(loaded.Theme=="Night"&&loaded.Controls[1].Source.RelativePath=="Themes/Night/part.xml"&&loaded.Controls[1].Get("label")=="Theme bearbeitet","Theme state was lost.");
            });
            Test("Project saving atomically replaces existing projects and protects XML targets",()=>
            {
                var p=SessionFixture(root,"project-replace");string path=Path.Combine(root,"replace.mp1skinproj");ProjectFile.Save(p,path);p.Edit(p.Controls[0],Set("posX","640"),"verschieben");ProjectFile.Save(p,path);
                Check(ProjectFile.Load(path).Controls[0].Get("posX")=="640","Existing project was not updated.");Reject(()=>ProjectFile.Save(p,Path.Combine(p.Root,"main.xml")));
            });
            Test("Texture catalogs find subdirectories and prefer the selected theme",()=>
            {
                string folder=Path.Combine(root,"texture-catalog");DemoAssets.Generate(folder);string themed=Path.Combine(folder,"Themes","Night","Media","Studio");Directory.CreateDirectory(themed);
                File.Copy(Path.Combine(folder,"Media","Studio","Button-blue.png"),Path.Combine(themed,"Button-blue.png"),true);
                var p=new SkinProject(folder){Theme="Night"};var assets=TextureCatalog.List(p);var blue=assets.First(a=>a.RelativePath=="Studio/Button-blue.png");
                Check(assets.Count==7&&blue.FromTheme&&blue.PhysicalPath.StartsWith(Path.Combine(folder,"Themes")),"Theme does not override the logical image path.");
            });
            Test("PNG previews report dimensions without retaining source file handles",()=>
            {
                var p=new SkinProject(StudioForm.DemoRoot());var asset=TextureCatalog.List(p).First(a=>a.RelativePath.EndsWith("Icon-Movie.png"));string error;
                using(var thumb=TextureCatalog.Thumbnail(asset,100,70,out error)){Check(thumb!=null&&thumb.Width==100&&thumb.Height==70&&asset.Width==200&&asset.Height==200&&error==null,"Incorrect thumbnail or dimensions.");}
                using(var exclusive=new FileStream(asset.PhysicalPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Check(exclusive.Length>0,"Image file remains locked.");
            });
            Test("Corrupted images produce errors instead of previews or crashes",()=>
            {
                string folder=Path.Combine(root,"texture-bad","Media");Directory.CreateDirectory(folder);string file=Path.Combine(folder,"broken.png");File.WriteAllText(file,"no image");
                var asset=new TextureAsset{PhysicalPath=file,RelativePath="broken.png"};string error;using(var thumb=TextureCatalog.Thumbnail(asset,100,70,out error))Check(thumb==null&&!String.IsNullOrEmpty(error),"Corrupted image was accepted.");
            });
        }
    }
}
