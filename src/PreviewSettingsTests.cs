using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunPreviewSettingsTests(string root)
        {
            Test("Zu große Projektbeschreibung wird abgewiesen und erhält die letzte gespeicherte Datei",()=>
            {
                var p=ResetFixture(root,"preview-manifest-limit");string file=Path.Combine(root,"preview-limit.mp1skinproj");ProjectFile.Save(p,file);byte[] original=File.ReadAllBytes(file);
                for(int index=0;index<128;index++)p.Editor.Preview.Values.Add("#v"+index.ToString("D3"),new string('界',2048));
                Reject(()=>ProjectFile.Save(p,file));Check(original.SequenceEqual(File.ReadAllBytes(file)),"Letztes lesbares Projekt wurde ersetzt.");
            });
            Test("Vorschaudaten ersetzen vollständige Variablen ohne Präfixfehler oder rekursive Ersetzung",()=>
            {
                var p=ResetFixture(root,"preview-tokens");p.Editor.Preview.Values["#title"]="Titel #date";var state=new PreviewState(p);
                Check(state.Text(p.Controls[0],"#title / #title.extra / #date")=="Titel #date / #title.extra / Samstag, 17. Oktober","Teilvariable oder eingesetzter Text erneut ersetzt.");
                p.Controls[0].Defines["#local"]="#alias";p.Controls[0].Defines["#alias"]="#title";Check(state.Text(p.Controls[0],"#local")=="Titel #date","Define-Kette falsch aufgelöst.");
            });
            Test("Ausdrückliche Beispielwerte haben Vorschauvorrang und ändern keine lokale Bindung",()=>
            {
                var p=ResetFixture(root,"preview-priority");p.Controls[0].Defines["#header.label"]="Lokaler Titel";string original=p.Controls[0].Source.Text;
                Check(new PreviewState(p).Text(p.Controls[0],"#header.label")=="Lokaler Titel","Lokaler Define ohne Override verloren.");p.Editor.Preview.Values["#header.label"]="Vorschau";
                Check(new PreviewState(p).Text(p.Controls[0],"#header.label")=="Vorschau"&&p.Controls[0].Source.Text==original&&!p.Dirty,"Beispielwert im XML gelandet.");
            });
            Test("Plugin- und Datenzustände schalten unterstützte Bedingungen; unbekannte bleiben unbekannt",()=>
            {
                var p=ResetFixture(root,"preview-conditions");p.Editor.Preview.SetFrom(PreviewSettings.Preset("Wetter"));p.Editor.Preview.Values["#myNotifier.Messages.Count.Unread"]="12";var state=new PreviewState(p);
                Check(state.Evaluate("plugin.isenabled(World Weather)",new HashSet<string>())==true&&state.Evaluate("!string.equals(#myNotifier.Messages.Count.Unread,0)",new HashSet<string>())==true,"Simulierte Zustände wirken nicht.");
                Check(state.Evaluate("plugin.isenabled(World Weather)+unsupported.test()",new HashSet<string>())==null,"Unbekannte Bedingung wurde geraten.");
            });
            Test("Ungültige Vorschaudaten werden vor jeder Übernahme abgewiesen",()=>
            {
                var settings=PreviewSettings.Preset("Nachrichten");string before=settings.SignatureText();var invalid=new PreviewSettings();invalid.Values["kein-hash"]="Wert";Reject(()=>settings.SetFrom(invalid));Check(settings.SignatureText()==before,"Ungültiger Stand teilweise übernommen.");
                invalid.Values.Clear();invalid.Values["#valid"]="\u0001";Reject(()=>settings.SetFrom(invalid));invalid.Values["#valid"]=new string('x',2049);Reject(()=>settings.SetFrom(invalid));
                var copy=settings.Copy();copy.Values["#myNotifier.Messages.Count.Unread"]="99";Check(settings.Values["#myNotifier.Messages.Count.Unread"]=="12","Arbeitskopie verändert Originalzustand.");
            });
            Test("Vorschauzustand bewahrt Leerwerte, Unicode, Leerraum und Plugin-Namen im Projekt",()=>
            {
                var p=ResetFixture(root,"preview-roundtrip");p.Editor.Preview.Values["#empty"]="";p.Editor.Preview.Values["#space"]="  \t\r\nÄ & <Wert>  ";p.Editor.Preview.Plugins.Add("World Weather");
                string signature=ProjectFile.ContentSignature(p),file=Path.Combine(root,"preview-roundtrip.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(loaded)==signature&&loaded.Editor.Preview.Values["#space"]==p.Editor.Preview.Values["#space"]&&loaded.Editor.Preview.Values["#empty"]==""&&new PreviewState(loaded).Plugins.Contains("World Weather")&&!loaded.Dirty,"Vorschauzustand verändert oder nicht wiederhergestellt.");
            });
            Test("Alte Projekte ohne Vorschauabschnitt laden die bisherigen Basisdaten",()=>
            {
                var p=ResetFixture(root,"preview-legacy");p.Editor.Preview.SetFrom(PreviewSettings.Preset("Wetter"));string file=Path.Combine(root,"preview-legacy.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var node=doc.SelectSingleNode("/skinStudioProject/editorState/preview");node.ParentNode.RemoveChild(node);return doc.OuterXml;});
                var loaded=ProjectFile.Load(file);var state=new PreviewState(loaded);Check(loaded.Editor.Preview.Values.Count==0&&state.Plugins.Count==0&&state.Values["#time"]=="18:42","Alte Projekte verlieren Standardansicht.");
            });
            Test("Doppelte Vorschauvariablen im Editorprojekt werden abgewiesen",()=>
            {
                var p=ResetFixture(root,"preview-invalid");p.Editor.Preview.Values["#title"]="A";string file=Path.Combine(root,"preview-invalid.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var node=doc.SelectSingleNode("/skinStudioProject/editorState/preview/value");node.ParentNode.AppendChild(node.CloneNode(true));return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
            Test("Vorschaudaten ändern Editor-Signatur, aber weder XML-Signatur noch Skinexport",()=>
            {
                var p=ResetFixture(root,"preview-export");p.Edit(p.Controls[0],Set("label","Bearbeitet"),"Test");string xml=ProjectFile.XmlSignature(p),editor=ProjectFile.EditorSignature(p),source=p.Controls[0].Source.Text;
                p.Editor.Preview.SetFrom(PreviewSettings.Preset("Wetter"));Check(xml==ProjectFile.XmlSignature(p)&&editor!=ProjectFile.EditorSignature(p),"Falsche Änderungsmarkierung.");
                string file=Path.Combine(root,"preview-export.zip");p.Export(file);using(var zip=ZipFile.OpenRead(file))using(var reader=new StreamReader(zip.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==source,"Vorschauwerte im Skinexport.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunPreviewSettingsUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");string initial=ProjectFile.ContentSignature(project);
            using(var dialog=new PreviewSettingsDialog(project))
            {
                dialog.Show();Application.DoEvents();verify(dialog.CollectForTest().Values.Count==0,"Testdaten-Dialog setzt unbekannte Bindungen nicht automatisch auf leer");
                dialog.PresetForTest("Langer Titel");verify(dialog.CollectForTest().Values["#title"].Length>80&&ProjectFile.ContentSignature(project)==initial,"Vorlagenauswahl bleibt bis zur Übernahme im Dialog");dialog.Close();
            }
            verify(ProjectFile.ContentSignature(project)==initial,"Abbrechen des Testdaten-Dialogs erhält den vollständigen Projektzustand");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));string key=selected.Key;fields["posY"].Text="640";
            var notifier=project.Controls.First(c=>c.Get("description")=="MyNotifier message icon");verify(state.Visible(notifier)==false,"Nachrichten-Szene beginnt mit ausgeblendeter Benachrichtigung");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.Show();Application.DoEvents();dialog.PresetForTest("Nachrichten");dialog.AcceptForTest();ApplyPreviewSettings(dialog.Result);}
            verify(state.Visible(notifier)==true&&selected.Key==key&&fields["posY"].Text=="640"&&!project.Dirty&&project.Undo.Count==0,"Beispieldaten schalten sichtbares Control um und erhalten offene Eigenschaftseingaben");
            verify(SessionNeedsSaving(),"Vorschauzustand wird ohne XML-Änderung als ungesichert erkannt");SelectControl(project.Controls.First(c=>c.Key==key));
            string file=Path.Combine(directory,"Vorschau.mp1skinproj");SaveSession(file);ResumeSession(file);
            verify(state.Values["#myNotifier.Messages.Count.Unread"]=="12"&&!SessionNeedsSaving()&&!project.Dirty,"Projektöffnung stellt gespeicherte Vorschauwerte und Sicherungsstatus wieder her");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.Show();Application.DoEvents();dialog.PresetForTest("Wetter");dialog.AcceptForTest();ApplyPreviewSettings(dialog.Result);}
            verify(state.Plugins.Contains("World Weather")&&state.Values["#WorldWeather.TodayTemperature"]=="−4 °C","Wettervorlage übernimmt Plugin-Zustand und Temperatur");
            ApplyPreviewSettings(new PreviewSettings());verify(state.Plugins.Count==0&&state.Values["#myNotifier.Messages.Count.Unread"]=="0"&&project.Undo.Count==0,"Basiszustand stellt Standardwerte ohne XML-Undo-Schritt wieder her");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.SetValueForTest("ungültig","Test");bool rejected=false;try{dialog.CollectForTest();}catch{rejected=true;}verify(rejected&&project.Editor.Preview.Values.Count==0,"Ungültige Variablen werden vor Änderung der laufenden Vorschau erkannt");}
            var dependent=project.Controls.First(c=>c.Get("description")=="Beispiel-Nachrichten");string dependentKey=dependent.Key;project.Edit(dependent,new Dictionary<string,string>{{"visible","control.hasfocus(10)"}},"Fokusprüfung");RefreshProject(key);
            Func<string> mark=()=>TreeControlNodes().First(n=>((SkinControl)n.Tag).Key==dependentKey).Text;
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));bool shown=!mark().StartsWith("○ ",StringComparison.Ordinal);SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"));
            verify(shown&&mark().StartsWith("○ ",StringComparison.Ordinal),"Fokuswechsel aktualisiert Sichtbarkeitsmarkierung im Elementbaum sofort");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
