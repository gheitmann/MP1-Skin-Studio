using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ResetFixture(string root,string name)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);
            Write(folder,"references.xml","<controls><control><type>label</type><width>100</width><font>font22</font></control><style Name=\"Title\"><width>200</width><textcolor>ff112233</textcolor></style></controls>");
            Write(folder,"main.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id><width>300</width><label>Text</label><!--keep--><extension keep=\"yes\"/></control><import>part.xml</import></controls></window>");
            Write(folder,"part.xml","<window><controls><control><type>label</type><id>0</id><width>400</width></control></controls></window>");
            var project=new SkinProject(folder);project.OpenPage("main.xml");return project;
        }
        static void RunPropertyResetTests(string root)
        {
            Test("Rücksetzen entfernt nur den lokalen XML-Bereich samt seiner eigenen Leerzeile",()=>
            {
                string text="<window>\r\n  <controls>\r\n    <control>\r\n      <type>label</type>\r\n      <!--outside-->\r\n      <width>300</width>\r\n      <unknown value=\"keep\"/>\r\n    </control>\r\n  </controls>\r\n</window>";
                var source=SkinSource.FromText(text,"a.xml");source.ResetControlProperty(0,"width");Check(source.Text==text.Replace("      <width>300</width>\r\n",""),"Andere Quellbereiche geändert.");
            });
            Test("Kommentare und PIs bleiben beim Entfernen erhalten; CDATA-Inhalt wird nicht als Kommentar behandelt",()=>
            {
                string text="<window><controls><control><type>label</type><label><!--keep--><?studio keep?><![CDATA[<!--value-only-->]]>Text<!--last--></label><other>keep</other></control></controls></window>";
                var source=SkinSource.FromText(text,"a.xml");source.ResetControlProperty(0,"label");
                Check(source.Text=="<window><controls><control><type>label</type><!--keep--><?studio keep?><!--last--><other>keep</other></control></controls></window>","Kommentare, PI oder CDATA falsch behandelt.");
            });
            Test("Rücksetzvorschau verwendet Style vor Referenz und verändert die Sitzung nicht",()=>
            {
                var p=ResetFixture(root,"reset-style");string signature=ProjectFile.ContentSignature(p);var info=p.PreviewPropertyReset(p.Controls[0],"width");
                Check(info.HasLocal&&info.Allowed&&info.Inherited.Value=="200"&&info.Inherited.Origin=="Style Title"&&ProjectFile.ContentSignature(p)==signature&&!p.Dirty,"Vorschau falsch oder schreibend.");
                p.ResetProperty(new[]{p.Controls[0]},"width",false);Check(p.Controls[0].Get("width")=="200"&&!p.Controls[0].Properties["width"].Local&&p.Controls[0].Node.Child("extension")!=null,"Geerbter Style oder Erweiterung verloren.");
            });
            Test("Mehrere Quellen übernehmen Referenz und Style in einem exakten Undo-/Redo-Schritt",()=>
            {
                var p=ResetFixture(root,"reset-many");string[] before=p.Controls.Select(c=>c.Source.Text).ToArray();p.ResetProperty(p.Controls,"width",false);
                Check(p.Controls[0].Get("width")=="200"&&p.Controls[1].Get("width")=="100"&&p.Undo.Count==1,"Unterschiedliche Vererbungen oder Transaktion falsch.");
                string[] after=p.Controls.Select(c=>c.Source.Text).ToArray();p.UndoChange();Check(p.Controls.Select(c=>c.Source.Text).SequenceEqual(before)&&!p.Dirty,"Undo nicht exakt.");p.RedoChange();Check(p.Controls.Select(c=>c.Source.Text).SequenceEqual(after),"Redo nicht exakt.");
            });
            Test("Rücksetzen ohne geerbten Wert entfernt die Eigenschaft statt einen Ersatzwert zu schreiben",()=>
            {
                var p=ResetFixture(root,"reset-absent");var info=p.PreviewPropertyReset(p.Controls[0],"label");Check(info.Inherited==null,"Unerwartete Vererbung.");p.ResetProperty(new[]{p.Controls[0]},"label",false);
                Check(!p.Controls[0].Properties.ContainsKey("label")&&p.Controls[0].Node.Child("label")==null,"Künstlicher Ersatzwert eingetragen.");
            });
            Test("Nicht lokal gesetzte Werte sind ein No-op und erzeugen keinen Undo-Schritt",()=>
            {
                var p=ResetFixture(root,"reset-noop");string signature=ProjectFile.ContentSignature(p);p.ResetProperty(p.Controls,"font",false);
                Check(!p.PreviewPropertyReset(p.Controls[0],"font").HasLocal&&p.Undo.Count==0&&ProjectFile.ContentSignature(p)==signature,"Geerbten Wert verändert.");
            });
            Test("Attribute, verschachtelte und doppelte Eigenschaften sowie Identitäten bleiben geschützt",()=>
            {
                foreach(string extra in new[]{"<width mode=\"keep\">300</width>","<width><extension>300</extension></width>","<width>300</width><width>400</width>"})
                {var source=SkinSource.FromText("<window><controls><control><type>label</type><id>10</id>"+extra+"</control></controls></window>","a.xml");string before=source.Text;Reject(()=>source.ResetControlProperty(0,"width"));Reject(()=>source.ResetControlProperty(0,"id"));Reject(()=>source.ResetControlProperty(0,"type"));Check(source.Text==before,"Geschützte Struktur verändert.");}
            });
            Test("Geschützte Bindung blockiert gesamte Auswahl; Freigabe erlaubt Rücksetzen",()=>
            {
                var p=ResetFixture(root,"reset-bindings");p.Edit(p.Controls[1],Set("width","#plugin.width"),"Bindung");string signature=ProjectFile.ContentSignature(p);int steps=p.Undo.Count;
                Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==steps,"Teiländerung trotz Bindung.");
                p.ResetProperty(p.Controls,"width",true);Check(p.Controls[0].Get("width")=="200"&&p.Controls[1].Get("width")=="100","Freigabe wirkungslos.");
            });
            Test("Gesperrte oder strukturell geschützte Auswahl wird vor jeder Teiländerung abgewiesen",()=>
            {
                var p=ResetFixture(root,"reset-atomic");p.Editor.Locked.Add(p.Controls[1].Key);string before=ProjectFile.ContentSignature(p);Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(before==ProjectFile.ContentSignature(p)&&!p.Dirty,"Teiländerung trotz Sperre.");p.Editor.Locked.Clear();
                p.Controls[1].Source.Text=p.Controls[1].Source.Text.Replace("<width>","<width keep=\"yes\">");p.Controls[1].Source.Parse();p.OpenPage("main.xml");before=ProjectFile.ContentSignature(p);
                Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Teiländerung vor Strukturfehler.");
            });
            Test("UTF-16-Kodierung und BOM bleiben beim Rücksetzen unverändert",()=>
            {
                string path=Path.Combine(root,"reset-utf16.xml");File.WriteAllText(path,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><controls><control><type>label</type><width>300</width><label>Äöß</label></control></controls></window>",new UnicodeEncoding(false,true,true));
                var source=SkinSource.Read(path,"reset-utf16.xml");byte[] disk=File.ReadAllBytes(path);source.ResetControlProperty(0,"width");Check(source.Bytes()[0]==255&&source.Bytes()[1]==254&&source.Text.Contains("Äöß")&&disk.SequenceEqual(File.ReadAllBytes(path)),"BOM, Text oder Originaldatei verändert.");
            });
            Test("Editorprojekt und ZIP erhalten gezielte Rücksetzungen, ohne Style- oder Referenzdatei zu ändern",()=>
            {
                var p=ResetFixture(root,"reset-save");byte[] reference=File.ReadAllBytes(Path.Combine(p.Root,"references.xml"));p.ResetProperty(p.Controls,"width",false);
                string file=Path.Combine(root,"reset.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.Controls[0].Get("width")=="200","Projekt verliert Rücksetzung.");
                string zip=Path.Combine(root,"reset.zip");loaded.Export(zip);using(var archive=ZipFile.OpenRead(zip))Check(archive.GetEntry("references.xml")==null&&archive.GetEntry("main.xml")!=null&&archive.GetEntry("part.xml")!=null,"Falsche Quellen exportiert.");
                Check(reference.SequenceEqual(File.ReadAllBytes(Path.Combine(p.Root,"references.xml"))),"Referenzdatei überschrieben.");
            });
        }
    }
}
