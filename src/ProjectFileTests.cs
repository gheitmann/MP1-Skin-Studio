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
