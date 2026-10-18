using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunEditorStateTests(string root)
        {
            Test("Saved editor state remains independent of XML and source files",()=>
            {
                var p=SessionFixture(root,"editor-state");string xml=ProjectFile.XmlSignature(p);byte[] disk=File.ReadAllBytes(Path.Combine(p.Root,"part.xml"));
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[1].Key);p.Editor.Zoom=3.25;p.Editor.PanX=-125.5;p.Editor.PanY=44.25;
                string file=Path.Combine(root,"editor-state.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.IsLocked(loaded.Controls[0])&&loaded.IsEditorHidden(loaded.Controls[1]),"State or signature was lost.");
                Check(!loaded.Dirty&&xml==ProjectFile.XmlSignature(loaded)&&disk.SequenceEqual(File.ReadAllBytes(Path.Combine(p.Root,"part.xml"))),"Editor state changed XML.");
                using(var archive=ZipFile.OpenRead(file))Check(archive.Entries.Count==1,"Unmodified XML was embedded.");
            });
            Test("Element protection blocks batch edits atomically",()=>
            {
                var p=SessionFixture(root,"editor-lock");p.Editor.Locked.Add(p.Controls[1].Key);string first=p.Controls[0].Source.Text;
                Reject(()=>p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("posX","250")),new ControlEdit(p.Controls[1],Set("posX","350"))},"locked"));
                Check(!p.Dirty&&p.Controls[0].Source.Text==first&&p.Undo.Count==0,"Partial change occurred despite a lock.");
                p.Editor.Locked.Clear();p.Edit(p.Controls[1],Set("posX","350"),"freigegeben");Check(p.Dirty,"Unlocking had no effect.");
            });
            Test("Group protection and editor hiding apply to child controls",()=>
            {
                var p=SessionFixture(root,"editor-parent");Write(p.Root,"group.xml","<window><controls><control><type>group</type><control><type>label</type><label>Kind</label></control></control></controls></window>");p.OpenPage("group.xml");
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[0].Key);
                Check(p.Controls.Count==2&&p.IsLocked(p.Controls[1])&&p.IsEditorHidden(p.Controls[1]),"Inherited protection is missing.");Reject(()=>p.Edit(p.Controls[1],Set("label","Neu"),"Kind"));
            });
            Test("Legacy projects without editorState open with a fitted view",()=>
            {
                var p=SessionFixture(root,"editor-legacy");p.Edit(p.Controls[0],Set("posX","75"),"alt");string file=Path.Combine(root,"editor-legacy.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var state=doc.SelectSingleNode("/skinStudioProject/editorState");state.ParentNode.RemoveChild(state);return doc.OuterXml;});
                var loaded=ProjectFile.Load(file);Check(loaded.Controls[0].Get("posX")=="75"&&loaded.Editor.Zoom==1&&loaded.Editor.Locked.Count==0,"Legacy project is incompatible.");
            });
            Test("Source changes for lock-only markers are detected",()=>
            {
                var p=SessionFixture(root,"editor-conflict");p.Editor.Locked.Add(p.Controls[0].Key);string file=Path.Combine(root,"editor-conflict.mp1skinproj");ProjectFile.Save(p,file);
                File.AppendAllText(Path.Combine(p.Root,"main.xml"),"<!--extern--> ");Reject(()=>ProjectFile.Load(file));
            });
            Test("Invalid views and editor paths are rejected on opening",()=>
            {
                var p=SessionFixture(root,"editor-invalid");p.Editor.Locked.Add(p.Controls[0].Key);string file=Path.Combine(root,"editor-invalid.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>text.Replace("zoom=\"1\"","zoom=\"NaN\""));Reject(()=>ProjectFile.Load(file));
                ProjectFile.Save(p,file);RewriteManifest(file,text=>text.Replace("main.xml|0","../escape.xml|0"));Reject(()=>ProjectFile.Load(file));
            });
            Test("Editor markers are absent from exported skin XML",()=>
            {
                var p=SessionFixture(root,"editor-export");p.Edit(p.Controls[0],Set("posX","77"),"change");string edited=p.Controls[0].Source.Text;
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[0].Key);p.Editor.Zoom=2;
                string file=Path.Combine(root,"editor-export.zip");p.Export(file);
                using(var archive=ZipFile.OpenRead(file))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==edited&&archive.Entries.All(e=>e.FullName=="main.xml"||e.FullName=="_SkinStudio-Instructions.txt"),"Editor metadata leaked into the skin export.");
            });
            Test("Markers for undone inserted controls are not saved incorrectly",()=>
            {
                var p=SessionFixture(root,"editor-orphan");string key=p.InsertControl("main.xml","label");p.Editor.Locked.Add(key);p.UndoChange();
                string file=Path.Combine(root,"editor-orphan.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(loaded.Editor.Locked.Count==0&&ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded),"Orphaned marker was restored.");
            });
        }
    }
}
