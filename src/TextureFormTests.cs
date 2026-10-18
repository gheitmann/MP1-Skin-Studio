using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RunTextureUiSmoke(string directory,Action<bool,string> verify)
        {
            using(var browser=new TextureBrowser(project,"Studio/Schaltflaeche-dunkel.png"))
            {
                browser.Show();Application.DoEvents();verify(browser.MatchCount>=7&&browser.SelectionValid,"Texturbrowser lädt vorhandene Demo-Bilder mit Vorschauen");
                browser.Filter("Symbol-");verify(browser.MatchCount==3,"Suchfeld filtert Texturen nach Dateipfad");
                browser.Filter("kein-bild-dieses-namens");verify(browser.MatchCount==0&&!browser.SelectionValid,"Leere Suche deaktiviert die Bildübernahme");
                browser.Filter("tuerkis");browser.SelectRelative("Studio/Schaltflaeche-tuerkis.png");browser.AcceptForTest();
                verify(browser.DialogResult==DialogResult.OK&&browser.SelectedPath=="Studio/Schaltflaeche-tuerkis.png","Bildauswahl übernimmt relativen Media-Pfad");
                SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));fields["textureNoFocus"].Text=browser.SelectedPath;Apply();
                verify(selected.Get("textureNoFocus")=="Studio/Schaltflaeche-tuerkis.png","Gewählte Textur wird gezielt am Control gespeichert");undo.PerformClick();
                verify(!project.Dirty,"Bildauswahl ist vollständig rückgängig");
            }
            using(var browser=new TextureBrowser(project,null))
            {browser.Show();Application.DoEvents();browser.DialogResult=DialogResult.Cancel;browser.Close();verify(!project.Dirty,"Abbrechen des Texturbrowsers ändert keine XML-Datei");}
        }
    }
}
