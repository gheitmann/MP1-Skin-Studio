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
            Test("Editorzustand bleibt beim Speichern unabhängig von XML und Quelldateien",()=>
            {
                var p=SessionFixture(root,"editor-state");string xml=ProjectFile.XmlSignature(p);byte[] disk=File.ReadAllBytes(Path.Combine(p.Root,"part.xml"));
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[1].Key);p.Editor.Zoom=3.25;p.Editor.PanX=-125.5;p.Editor.PanY=44.25;
                string file=Path.Combine(root,"editor-state.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.IsLocked(loaded.Controls[0])&&loaded.IsEditorHidden(loaded.Controls[1]),"Zustand oder Signatur verloren.");
                Check(!loaded.Dirty&&xml==ProjectFile.XmlSignature(loaded)&&disk.SequenceEqual(File.ReadAllBytes(Path.Combine(p.Root,"part.xml"))),"Editorzustand hat XML geändert.");
                using(var archive=ZipFile.OpenRead(file))Check(archive.Entries.Count==1,"Unverändertes XML wurde eingebettet.");
            });
            Test("Elementschutz blockiert gemeinsame Bearbeitung atomar",()=>
            {
                var p=SessionFixture(root,"editor-lock");p.Editor.Locked.Add(p.Controls[1].Key);string first=p.Controls[0].Source.Text;
                Reject(()=>p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("posX","250")),new ControlEdit(p.Controls[1],Set("posX","350"))},"gesperrt"));
                Check(!p.Dirty&&p.Controls[0].Source.Text==first&&p.Undo.Count==0,"Teiländerung trotz Sperre.");
                p.Editor.Locked.Clear();p.Edit(p.Controls[1],Set("posX","350"),"freigegeben");Check(p.Dirty,"Entsperren blieb unwirksam.");
            });
            Test("Gruppenschutz und Editor-Ausblendung gelten für enthaltene Controls",()=>
            {
                var p=SessionFixture(root,"editor-parent");Write(p.Root,"group.xml","<window><controls><control><type>group</type><control><type>label</type><label>Kind</label></control></control></controls></window>");p.OpenPage("group.xml");
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[0].Key);
                Check(p.Controls.Count==2&&p.IsLocked(p.Controls[1])&&p.IsEditorHidden(p.Controls[1]),"Vererbter Schutz fehlt.");Reject(()=>p.Edit(p.Controls[1],Set("label","Neu"),"Kind"));
            });
            Test("Alte Editorprojekte ohne editorState öffnen mit eingepasster Ansicht",()=>
            {
                var p=SessionFixture(root,"editor-legacy");p.Edit(p.Controls[0],Set("posX","75"),"alt");string file=Path.Combine(root,"editor-legacy.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var state=doc.SelectSingleNode("/skinStudioProject/editorState");state.ParentNode.RemoveChild(state);return doc.OuterXml;});
                var loaded=ProjectFile.Load(file);Check(loaded.Controls[0].Get("posX")=="75"&&loaded.Editor.Zoom==1&&loaded.Editor.Locked.Count==0,"Altes Projekt nicht kompatibel.");
            });
            Test("Geänderte Quelle einer reinen Sperrmarkierung wird erkannt",()=>
            {
                var p=SessionFixture(root,"editor-conflict");p.Editor.Locked.Add(p.Controls[0].Key);string file=Path.Combine(root,"editor-conflict.mp1skinproj");ProjectFile.Save(p,file);
                File.AppendAllText(Path.Combine(p.Root,"main.xml"),"<!--extern--> ");Reject(()=>ProjectFile.Load(file));
            });
            Test("Ungültige Ansichten und Editor-Pfade werden beim Öffnen abgewiesen",()=>
            {
                var p=SessionFixture(root,"editor-invalid");p.Editor.Locked.Add(p.Controls[0].Key);string file=Path.Combine(root,"editor-invalid.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>text.Replace("zoom=\"1\"","zoom=\"NaN\""));Reject(()=>ProjectFile.Load(file));
                ProjectFile.Save(p,file);RewriteManifest(file,text=>text.Replace("main.xml|0","../escape.xml|0"));Reject(()=>ProjectFile.Load(file));
            });
            Test("Editor-Markierungen fehlen vollständig im exportierten Skin-XML",()=>
            {
                var p=SessionFixture(root,"editor-export");p.Edit(p.Controls[0],Set("posX","77"),"ändern");string edited=p.Controls[0].Source.Text;
                p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Hidden.Add(p.Controls[0].Key);p.Editor.Zoom=2;
                string file=Path.Combine(root,"editor-export.zip");p.Export(file);
                using(var archive=ZipFile.OpenRead(file))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==edited&&archive.Entries.All(e=>e.FullName=="main.xml"||e.FullName=="_SkinStudio-Hinweise.txt"),"Editor-Metadaten im Skinexport.");
            });
            Test("Markierungen rückgängig gemachter neuer Controls werden nicht falsch gespeichert",()=>
            {
                var p=SessionFixture(root,"editor-orphan");string key=p.InsertControl("main.xml","label");p.Editor.Locked.Add(key);p.UndoChange();
                string file=Path.Combine(root,"editor-orphan.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(loaded.Editor.Locked.Count==0&&ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded),"Verwaiste Markierung wiederhergestellt.");
            });
        }
    }
}
