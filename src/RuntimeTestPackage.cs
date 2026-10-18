using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    // Produces a reversible export probe; never installs to ProgramData or edits the source skin.
    public static class RuntimeTestPackage
    {
        public static int Run(string root,string output)
        {
            root=Path.GetFullPath(root);output=Path.GetFullPath(output);
            if(output.Equals(root,StringComparison.OrdinalIgnoreCase)||output.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Testausgabe außerhalb des Original-Skinordners wählen.");
            if(Directory.Exists(output)&&Directory.GetFileSystemEntries(output).Length>0)throw new InvalidOperationException("Für die Laufzeitprobe einen leeren Ausgabeordner verwenden.");
            Directory.CreateDirectory(output);var project=new SkinProject(root);project.OpenPage("settings.xml");
            var group=project.Controls.FirstOrDefault(c=>c.Get("type")=="group"&&c.Get("description")=="buttons");StackPreview.Rule rule;
            if(group==null||!StackPreview.TryRead(group.Resolved("layout"),out rule))throw new InvalidDataException("Die erwartete Titan-Einstellungsgruppe wurde nicht gefunden.");
            var before=project.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)),StringComparer.OrdinalIgnoreCase);
            string old=group.Get("layout");project.SetStackLayout(group,"StackLayout("+(rule.Gap+8)+", "+(rule.Horizontal?"Horizontal":"Vertical")+", "+(rule.Collapse?"true":"false")+")",false);
            string zip=Path.Combine(output,"Titan-Einstellungen-Testexport.zip");project.Export(zip);ProjectFile.Save(project,Path.Combine(output,"Titan-Einstellungen.mp1skinproj"));
            using(var archive=ZipFile.OpenRead(zip))
            {
                if(archive.Entries.Count!=2||archive.GetEntry("settings.xml")==null)throw new InvalidDataException("Export enthält unerwartete Dateien.");
                using(var reader=new StreamReader(archive.GetEntry("settings.xml").Open()))if(reader.ReadToEnd()!=project.Controls.First(c=>c.Key==group.Key).Source.Text)throw new InvalidDataException("Export und bearbeitetes XML stimmen nicht überein.");
            }
            project.UndoChange();if(project.Dirty||project.Controls.First(c=>c.Key==group.Key).Get("layout")!=old)throw new InvalidDataException("Exportprobe lässt sich nicht exakt rückgängig machen.");
            if(before.Any(p=>ProjectFile.Digest(File.ReadAllBytes(p.Key))!=p.Value))throw new InvalidDataException("Original-Skin wurde verändert.");
            var lines=new[]{"MP1 Skin Studio - vorbereiteter praktischer Laufzeittest","","Automatisch geprüft:","PASS  Titan settings.xml geladen und Gruppenabstand um 8 Pixel erhöht.","PASS  Export enthält nur settings.xml und die Exporthinweise.","PASS  Exportiertes XML stimmt exakt mit dem Editorstand überein.","PASS  Editorprojekt gespeichert und Änderung vollständig rückgängig gemacht.","PASS  Keine geladene Originaldatei verändert.","","NOCH NICHT DURCHGEFÜHRT: vollständiger Client-/Direct3D-Bildvergleich.","","Praktischer Ablauf nach vollständiger Einrichtung von MediaPortal 1:","1. Den Titan-Skinordner kopieren und die Kopie SkinStudio-Test nennen.","2. Nur settings.xml aus Titan-Einstellungen-Testexport.zip in dieser Kopie ersetzen.","3. In MP1 den kopierten Skin wählen oder mit /windowed /skin=SkinStudio-Test starten.","4. Einstellungen öffnen: Die Schaltflächen müssen 8 Skin-Pixel mehr Abstand haben.","5. Texte, Fokus und Navigation mit dem Editorbild vergleichen.","6. Zum ursprünglichen Titan-Skin zurückwechseln; die Originaldateien bleiben erhalten.","","Wichtig: Der TV-Server ist für diese Probe nicht erforderlich.","Die lokale Dateikopie ist keine vollständig registrierte Systeminstallation.","Das offizielle Setup und Configuration.exe verlangen Administratorrechte.","Die isolierte Kopie darf erst nach abgeschlossener Erstkonfiguration als funktionsfähig gelten."};
            File.WriteAllLines(Path.Combine(output,"Laufzeitpruefung.txt"),lines,new UTF8Encoding(false));return 0;
        }
    }
}
