using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        internal static LanguagePreview DemoLanguage(){return new LanguagePreview{Primary=LanguagePack.Load(Path.Combine(StudioForm.DemoRoot(),"Languages","strings_de.xml")),Fallback=LanguagePack.Load(Path.Combine(StudioForm.DemoRoot(),"Languages","strings_en.xml"))};}
        static LanguagePack Pack(string xml){return LanguagePack.FromBytes(Encoding.UTF8.GetBytes(xml),"strings_test.xml");}
        static void RunLanguageTests(string root)
        {
            Test("Sprachtexte verwenden Hauptsprache, Fallback und konfigurierbare Präfixe",()=>
            {var language=DemoLanguage();string text,origin;Check(language.TryGet(10003,out text,out origin)&&text=="Meine Filme"&&origin.Contains("Deutsch"),"Hauptsprache oder Präfix falsch.");language.Prefixes=false;Check(language.TryGet(10003,out text,out origin)&&text=="Filme"&&language.TryGet(10004,out text,out origin)&&text=="Settings"&&origin.Contains("Fallback"),"Fallback oder Präfixabschaltung falsch.");});
            Test("Leere primäre Übersetzung fällt nicht auf einen anderen Text zurück",()=>
            {var language=DemoLanguage();string text,origin;Check(language.TryGet(10005,out text,out origin)&&text==""&&origin.Contains("Deutsch"),"Absichtlich leerer Text ersetzt.");});
            Test("Sprachdateien lehnen DTD, doppelte IDs und fremde Formate ab",()=>
            {Reject(()=>Pack("<!DOCTYPE Language [<!ENTITY a 'x'>]><Language><Section name='unmapped'><String id='1'>&a;</String></Section></Language>"));Reject(()=>Pack("<Language><Section name='unmapped'><String id='1'>A</String><String id='01'>B</String></Section></Language>"));Reject(()=>Pack("<window/>"));Reject(()=>Pack("<Language><Section name='other'><String id='1'>A</String></Section></Language>"));});
            Test("Sprachvorschau verändert numerische XML-IDs nicht und lässt fehlende IDs erkennbar",()=>
            {var p=Demo();p.OpenPage("demo.sprache.xml");p.Editor.Language.SetFrom(DemoLanguage());var state=new PreviewState(p);var c=p.Controls.First(x=>x.Get("id")=="501");Check(state.Label(c,c.Get("label"))=="Meine Filme"&&c.Get("label")=="10003"&&!p.Dirty&&state.Label(c,"99999999")=="[99999999]","Sprach-ID überschrieben oder Verlust verschwiegen.");});
            Test("Numerische Beispieldaten werden nicht irrtümlich als Sprach-ID interpretiert",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());p.Editor.Preview.Values["#title"]="10003";var state=new PreviewState(p);var c=p.Controls[0];Check(state.Label(c,"#title")=="10003","Dynamischer Zahlenwert lokalisiert.");c.Defines["#code"]="10003";Check(state.Label(c,"#code")=="Meine Filme"&&state.Label(c,"-")=="","Statischer Define oder leeres Label falsch.");});
            Test("Sprachdatei-Schnappschüsse sind unveränderlich und Kopien unabhängig",()=>
            {var language=DemoLanguage();var copy=language.Copy();copy.Prefixes=false;copy.Primary=null;byte[] bytes=language.Primary.Snapshot();bytes[0]=0;string text,origin;Check(language.TryGet(10003,out text,out origin)&&text=="Meine Filme"&&language.Primary.Snapshot()[0]!=0,"Kopie verändert Originalzustand.");});
            Test("Editorprojekt stellt Sprach-Schnappschüsse ohne ursprüngliche Sprachdateien wieder her",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());string temporary=Path.Combine(root,Guid.NewGuid().ToString("N")+".xml");File.WriteAllBytes(temporary,p.Editor.Language.Primary.Snapshot());p.Editor.Language.Primary=LanguagePack.Load(temporary);p.Editor.ShowUnresolved=false;string before=ProjectFile.ContentSignature(p),path=Path.Combine(root,"language.mp1skinproj");ProjectFile.Save(p,path);File.Delete(temporary);var loaded=ProjectFile.Load(path);Check(ProjectFile.ContentSignature(loaded)==before&&!loaded.Editor.ShowUnresolved&&!loaded.Dirty&&loaded.Editor.Language.Primary.Count==4,"Sprachzustand nicht vollständig gespeichert.");using(var zip=ZipFile.OpenRead(path))Check(zip.GetEntry("language/primary.xml")!=null&&zip.GetEntry("language/fallback.xml")!=null,"Sprachdateien nicht im Archiv enthalten.");});
            Test("Beschädigter Sprach-Schnappschuss und doppelter Sprachzustand werden abgewiesen",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());string path=Path.Combine(root,"language-bad.mp1skinproj");ProjectFile.Save(p,path);using(var zip=ZipFile.Open(path,ZipArchiveMode.Update)){zip.GetEntry("language/primary.xml").Delete();var entry=zip.CreateEntry("language/primary.xml");using(var writer=new StreamWriter(entry.Open()))writer.Write("<Language/>");}Reject(()=>ProjectFile.Load(path));ProjectFile.Save(p,path);RewriteManifest(path,text=>text.Replace("</editorState>","<languagePreview prefixes='true'/></editorState>"));Reject(()=>ProjectFile.Load(path));});
            Test("Sprach-Einstellungen verändern Editor-Signatur aber keinen Skin-Export",()=>
            {var p=Demo();string before=ProjectFile.EditorSignature(p);p.Editor.Language.SetFrom(DemoLanguage());Check(before!=ProjectFile.EditorSignature(p)&&!p.Dirty&&p.Undo.Count==0,"Sprache nicht als Editorzustand getrennt.");Reject(()=>p.Export(Path.Combine(root,"language-only.zip")));});
            Test("Elementsuche findet übersetzte Beschriftungen bei unverändertem Rohwert",()=>
            {var p=Demo();p.OpenPage("demo.sprache.xml");p.Editor.Language.SetFrom(DemoLanguage());var c=p.Controls.First(x=>x.Get("id")=="501");var state=new PreviewState(p);Check(ElementSearch.Matches(c,"text:Meine Filme",state.Label(c,c.Get("label")))&&ElementSearch.Matches(c,"id:501",state.Label(c,c.Get("label")))&&c.Get("label")=="10003"&&LanguagePreview.UseLabelName(c),"Übersetzte Suche oder Rohwert verloren.");});
            Test("Fehlende Sprach-ID wird nur bei geladener Sprachvorschau als Fund gemeldet",()=>
            {var p=Demo();p.OpenPage("demo.sprache.xml");p.SetControlText(p.Controls.First(c=>c.Get("id")=="501"),"99999999",true);Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="LANG_MISSING"),"Ohne Sprachvorschau geraten.");p.Editor.Language.SetFrom(DemoLanguage());var issue=SkinAudit.Run(p).Issues.Single(i=>i.Code=="LANG_MISSING");Check(issue.Property=="label"&&issue.ControlKey==p.Controls.First(c=>c.Get("id")=="501").Key,"Fehlende ID nicht anspringbar zugeordnet.");});
            Test("UTF-16-Sprachschnappschuss behält BOM und Dateibytes",()=>
            {string xml="<?xml version='1.0' encoding='utf-16'?><Language name='Deutsch'><Section name='unmapped'><String id='1' prefix='Mein '>Gruß</String></Section></Language>";byte[] bytes=Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(xml)).ToArray();var pack=LanguagePack.FromBytes(bytes,"strings_de.xml");string value;Check(pack.Snapshot().SequenceEqual(bytes)&&pack.TryGet(1,true,out value)&&value=="Mein Gruß","Sprachbytes oder BOM verändert.");});
            Test("Bisheriges Projektformat 2 bleibt ohne Sprach-Schnappschüsse lesbar",()=>
            {var p=Demo();p.Edit(Named(p,"Filme"),Set("posX","200"),"Ändern");string path=Path.Combine(root,"language-format2.mp1skinproj");ProjectFile.Save(p,path);RewriteManifest(path,text=>{var doc=new System.Xml.XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","2");return doc.OuterXml;});var loaded=ProjectFile.Load(path);Check(!loaded.Editor.Language.Active&&Named(loaded,"Filme").Get("posX")=="200","Vorheriges Format nicht kompatibel.");});
        }
    }
    public sealed partial class StudioForm
    {
        void RunLanguageUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.sprache.xml");string source=selected.Source.Text;ApplyLanguage(SelfTests.DemoLanguage());
            verify(selected.Get("id")=="501"&&selectionName.Text=="Meine Filme"&&((SkinControl)tree.SelectedNode.Tag).Get("label")=="10003"&&tree.SelectedNode.Text=="Meine Filme","Sprachwechsel zeigt übersetzte Namen und wahrt die originale XML-ID");
            elementSearch.Text="text:Meine Filme";verify(SearchResults().Length==1&&VisibleTreeControls().Length==1,"Elementsuche findet den übersetzten Text in der echten Oberfläche");elementSearch.Clear();
            verify(!project.Dirty&&project.Undo.Count==0&&SessionNeedsSaving()&&selected.Source.Text==source,"Sprachwechsel markiert nur das Editorprojekt zum Sichern");
            using(var dialog=new LanguageDialog(project.Editor.Language)){dialog.Show(this);Application.DoEvents();dialog.Width=860;Application.DoEvents();verify(dialog.SearchFits,"Suchfeld im Sprachkatalog bleibt auch bei schmalem Fenster vollständig sichtbar");dialog.SearchForTest("10004");verify(dialog.VisibleCount==1,"Sprachkatalog filtert Fallback-ID");dialog.PrefixesForTest(false);dialog.SearchForTest("Meine Filme");verify(dialog.VisibleCount==0,"Präfixabschaltung wirkt im Sprachkatalog");dialog.Close();}verify(project.Editor.Language.Prefixes,"Abbrechen im Sprachdialog verändert keine Vorschauoption");
            using(var picker=new LanguageDialog(project.Editor.Language,true)){picker.Show(this);Application.DoEvents();picker.SearchForTest("Settings");picker.AcceptForTest();verify(picker.SelectedId==10004,"Sprachtext-Auswahl liefert die passende ID statt einen festgeschriebenen Text");}
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state)){dialog.Show(this);Application.DoEvents();verify(dialog.PreviewForTest=="Meine Filme","Texteditor zeigt Sprach-ID neben aufgelöster Vorschau");dialog.TextForTest("10004");verify(!dialog.CanAccept&&dialog.PreviewForTest=="Settings","Andere Sprach-ID zeigt Fallback und bleibt bis zur Freigabe geschützt");dialog.Close();}
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state))
            {dialog.Show(this);Application.DoEvents();bool picked=false;using(var timer=new Timer{Interval=30}){timer.Tick+=(s,e)=>{var picker=Application.OpenForms.OfType<LanguageDialog>().FirstOrDefault();if(picker!=null){timer.Stop();picker.SearchForTest("Settings");picker.AcceptForTest();picked=true;if(picker.DialogResult==DialogResult.None){picker.DialogResult=DialogResult.Cancel;picker.Close();}}};timer.Start();dialog.PickLanguageForTest();timer.Stop();}verify(picked&&!dialog.CanAccept&&dialog.PreviewForTest=="Settings","Sprachtext-Picker im Textdialog setzt eine ID und erhält den Ersetzungsschutz");dialog.Close();}
            string path=Path.Combine(directory,"Sprache.mp1skinproj");SaveSession(path);ApplyLanguage(new LanguagePreview());ResumeSession(path);verify(project.Editor.Language.Primary!=null&&selectionName.Text=="Meine Filme"&&!SessionNeedsSaving(),"Gespeichertes Projekt stellt Vorschau-Sprache und übersetzte Auswahl wieder her");
            LoadProject(DemoRoot(),"demo.xml");var c=project.Controls.First(x=>x.Name=="Filme");project.Edit(c,new Dictionary<string,string>{{"visible","unknown.condition()"}},"Unbekannte Bedingung");RefreshProject(c.Key);c=selected;verify(canvas.ExerciseDrawn(c),"Ungeprüfte Bedingungen bleiben standardmäßig gepunktet sichtbar");SetUnresolvedPreview(false);verify(!canvas.ExerciseDrawn(c)&&project.Undo.Count==1,"Ungeprüfte Zustände lassen sich ohne Änderung der Bedingung aus der Vorschau ausblenden");undo.PerformClick();verify(!project.Dirty&&!project.Editor.ShowUnresolved,"XML-Undo erhält die unabhängig gewählte Vorschauoption");
            LoadProject(DemoRoot(),"demo.xml");
        }
        public void PrepareLanguagePreview(){LoadProject(DemoRoot(),"demo.sprache.xml");ApplyLanguage(SelfTests.DemoLanguage());}
    }
}
