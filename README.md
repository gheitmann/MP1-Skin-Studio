# MP1 Skin Studio

A visual Windows skin editor for MediaPortal 1. This repository starts from the locally saved **0.9 prototype dated October 18, 2026**.

## Build and run

Requires Windows, .NET Framework 4.x and its C# compiler. From the repository directory:

```powershell
powershell -NoProfile -File .\build.ps1
.\MP1SkinStudio.exe
```

Demo assets are included under `Samples`. The inherited `README.txt` contains the original German user guide and version history. Application UI text and historical test reports retain their original language. New GitHub documentation and discussions use English by default.

## Features

Visual position, size, text and texture editing; multiple selection and alignment; layer ordering and deletion; nested groups; StackLayout previews; undo/redo; project persistence; and XML export. Preview tools include language resources with fallback, simulated states and sample data, plus approximate list, grid, filmstrip and static coverflow rendering.

## Validation

Historical reports under `Pruefungen` record 189 core checks, 185 UI checks, 602 Titan page/theme variants and ten direct checks against original MP1 StackLayout classes. These counts were imported from the saved release package and do not imply that all tests were rerun during import. A fresh build succeeded during import.

After building, run core checks with:

```powershell
$probe = Start-Process .\MP1SkinStudio.exe -ArgumentList '--selftest', "$env:TEMP\MP1SkinStudio-selftest" -Wait -PassThru -WindowStyle Hidden
if ($probe.ExitCode -ne 0) { throw 'Core checks failed' }
```

The full MediaPortal client and image comparison remains pending. `Runtime-Abgleich` contains the prepared test export. Preview rendering is approximate.

## Continuing development

See [STATUS.md](STATUS.md). This repository preserves the development baseline independently of chat history. Future changes must also be committed and uploaded to GitHub to be backed up there.

License: the existing GPLv3 license in [LICENSE](LICENSE).
