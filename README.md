# MP1 Skin Studio

Visueller Windows-Skineditor für MediaPortal 1. Ausgangspunkt dieses Repositorys ist der lokal gesicherte Prototyp **0.9 vom 18.10.2026**.

## Bauen und starten

Windows mit .NET Framework 4.x und dessen C#-Compiler ist erforderlich. Im Projektordner:

```powershell
powershell -NoProfile -File .\build.ps1
.\MP1SkinStudio.exe
```

Die Demo liegt unter `Samples`. Die ausführliche Bedienung und Versionshistorie stehen in [README.txt](README.txt).

## Funktionsumfang

Visuelle Geometrie- und Textbearbeitung, Texturen, Mehrfachauswahl, Ausrichtung, Ebenen und Löschen, verschachtelte Gruppen, StackLayout-Vorschau, Undo/Redo, Projektspeicherung und XML-Export. Dazu Sprachressourcen mit Fallback, simulierte Zustände und Beispieldaten sowie angenäherte Listen-, Raster-, Filmstreifen- und Coverflow-Darstellung.

## Prüfungen

Die übernommenen Berichte in `Pruefungen` dokumentieren den bisherigen Stand: 189 Kernprüfungen, 185 Oberflächenprüfungen, 602 Titan-Seiten-/Theme-Varianten und zehn direkte Prüfungen originaler MP1-StackLayout-Klassen. Diese Zahlen stammen aus dem gesicherten Paket; sie sind keine Behauptung eines neuen Testlaufs beim Repositoryimport.

Nach dem Build sind die Kernprüfungen beispielsweise so startbar:

```powershell
$probe = Start-Process .\MP1SkinStudio.exe -ArgumentList '--selftest', "$env:TEMP\MP1SkinStudio-selftest" -Wait -PassThru -WindowStyle Hidden
if ($probe.ExitCode -ne 0) { throw 'Kernprüfungen fehlgeschlagen' }
```

Der vollständige Client- und Bildvergleich in MediaPortal steht noch aus. `Runtime-Abgleich` enthält den vorbereiteten Testexport. Die Vorschau bleibt eine Annäherung.

## Weiterarbeit

Siehe [STATUS.md](STATUS.md). Das Repository dient als nachvollziehbarer Entwicklungsstand unabhängig vom Chatverlauf. Spätere Änderungen müssen ebenfalls committed und auf GitHub übertragen werden, um dort gesichert zu sein.

Lizenz: vorhandene GPLv3, siehe [LICENSE](LICENSE).
