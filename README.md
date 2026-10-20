# MP1 Skin Studio

A visual Windows skin editor for MediaPortal 1. This repository starts from the locally saved **0.9 prototype dated October 18, 2026**.

## Build and run

The review ZIP runs directly after complete extraction: see [Quick start](docs/QUICK_START.md). Building from source requires Windows, .NET Framework 4.x and its C# compiler. From the repository directory:

```powershell
powershell -NoProfile -File .\build.ps1
.\MP1SkinStudio.exe
```

Demo assets are included under `Samples`. See [User Guide](docs/USER_GUIDE.md) for workflows and limitations. English is the default for documentation, code comments, application text and GitHub discussions.

## Features

Visual position, size, text and texture editing; multiple selection and alignment; layer ordering and deletion; nested groups, creation from consecutive selections and neutral-group removal; StackLayout and GridLayout previews; undo/redo; project persistence; and XML export. Preview tools include language resources with fallback, simulated states and sample data, plus approximate list, grid, filmstrip and static coverflow rendering.

## Validation

Latest source checkpoint (October 20): **Save project / Discard changes / Cancel before closing or switching sessions**. Canceled/failed saves retain the active session, preview-only state is included and pending property inputs remain protected. Fresh extracted-package validation: 18 targeted form scenarios, twelve complete demo workflows, twelve clean-session workflows, eight startup scenarios and 31 unchanged payload hashes. See [session transition acceptance](Validation/Session-Transitions/README.md).

The previous attended real-Titan edit/save/reopen/export/client checkpoint remains historical: source 07e8ef2 displayed the changed Home/Video header and narrower list on an isolated skin, with Left/Right navigation and all 2,050 original hashes preserved. See [runtime review](Validation/Current-Runtime-Review/README.md) and [readiness scope](docs/RELEASE_READINESS.md). The broad 204 UI and 394 core checks were not repeated for this dialog-only change.


Historical reports under `Validation` record 189 core checks, 185 UI checks, 602 Titan page/theme variants and ten direct checks against original MP1 StackLayout classes. These counts were imported from the saved release package and do not imply that all tests were rerun during import. A fresh build succeeded during import. The English migration also passed 189 core checks and 185 UI checks; fresh reports are in [Validation/English-Migration](Validation/English-Migration).

After building, run core checks with:

```powershell
$probe = Start-Process .\MP1SkinStudio.exe -ArgumentList '--selftest', "$env:TEMP\MP1SkinStudio-selftest" -Wait -PassThru -WindowStyle Hidden
if ($probe.ExitCode -ne 0) { throw 'Core checks failed' }
```

Sampled Settings and Video exports passed installed-client display/navigation checks, including a fresh Video run against the current executable. Full matched-state visual equivalence and broader runtime coverage remain pending. `Runtime-Comparison` contains the prepared test export. Preview rendering is approximate.

## Continuing development

See [STATUS.md](STATUS.md). This repository preserves the development baseline independently of chat history. Future changes must also be committed and uploaded to GitHub to be backed up there.

License: the existing GPLv3 license in [LICENSE](LICENSE).

A manual review ZIP can be prepared with `powershell -NoProfile -File .\package-review.ps1`. It packages the editor and repository demo only, verifies every file hash, and does not launch any application. The latest attended and automated validation scopes are recorded above.
