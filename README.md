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

Latest source checkpoint (October 20): **automatic skin selection and wider dark sidebar**. SKIN above THEME lists detected installation folders/current siblings/the demo and refreshes when opened. The left column is 30% wider, with matching dark scrollbars in the main lists, tree, properties and information region. Skin switching preserves existing session protections; preview tools wrap at minimum width. Fresh checks: 420 core, 22 skin/sidebar/scrolling, twelve page, ten readability and 24 demo/session workflows. See [validation](Validation/Skin-Selector/README.md).

Previous source checkpoint (October 20): **caption-sized text selection**. Single-line label/fadelabel frames and click/double-click targets follow the visible caption, avoiding synthetic boxes over adjacent rows. Dragging retains position edits; Width/Height remain in Properties. Fresh checks: 416 core, 58 targeted selection/designer/search/text UI, ten readability and 24 demo/session workflows. See [selection validation](Validation/Text-Selection/README.md).

Previous source checkpoint (October 20): **faster XML loading and canvas visibility passes**. Position-bounded source scanning avoids repeated whole-remainder searches; completed visibility results are reused only within one paint/input inspection. DefaultWideHD BasicHome complete load measured 3121 to 1536 ms and warm redraw 445 to 295 ms. All 488 XML mappings agree; nine rendered images match a repeat of the baseline. Fresh checks passed: 414 core, 205 broad UI, ten readability and 24 demo/session workflows. See [render/load measurements and validation](Validation/Render-Load/README.md) for scope and current checks.

Previous source checkpoint (October 20): **faster element selection and pointer inspection**. Native tree updates are batched and only replace changed entries; picking shares one measured layout within each input inspection. Diagnostics evaluate visibility in one pass. Fresh package checks passed: 411 core, 205 broad UI, ten readable-preview, twelve demo and twelve clean-session workflows. See [responsiveness measurements and current acceptance](Validation/Responsiveness/README.md). The readable MediaTip preview remains included.

Previous source checkpoint (October 20): **readable preview for overlapping variants**. Integrated View offers Edit layout, temporary selection/group isolation and diagnostic All layers. Recognized MediaTipOverlay.xml exposes Music/Video/Pictures/Folder/Radio examples, showing one block with short sample text. Bindings, runtime conditions and exports remain unchanged; project files retain view/example state. Fresh package validation: 411 core checks, ten targeted DefaultWideHD form scenarios, twelve demo workflows, twelve clean-session workflows and 31 unchanged payload hashes. See [readable-preview validation](Validation/Readable-Preview/README.md). Other plugin layouts use generic isolation rather than inferred grouping. Prior broad/UI/browser/transition/startup reports remain historical.

The previous attended real-Titan edit/save/reopen/export/client checkpoint remains historical: source 07e8ef2 displayed the changed Home/Video header and narrower list on an isolated skin, with Left/Right navigation and all 2,050 original hashes preserved. See [runtime review](Validation/Current-Runtime-Review/README.md) and [readiness scope](docs/RELEASE_READINESS.md). The preceding XML-review milestone passed 402 core checks; the current 205 broad UI assertions are recorded separately.


Historical reports under `Validation` record 189 core checks, 185 UI checks, 602 Titan page/theme variants and ten direct checks against original MP1 StackLayout classes. These counts were imported from the saved release package and do not imply that all tests were rerun during import. A fresh build succeeded during import. The English migration also passed 189 core checks and 185 UI checks; fresh reports are in [Validation/English-Migration](Validation/English-Migration).

After building, run core checks with:

```powershell
$probe = Start-Process .\MP1SkinStudio.exe -ArgumentList '--selftest', "$env:TEMP\MP1SkinStudio-selftest" -Wait -PassThru -WindowStyle Hidden
if ($probe.ExitCode -ne 0) { throw 'Core checks failed' }
```

Sampled Settings and Video exports passed installed-client display/navigation checks, including the recorded Video run against source 07e8ef2. Full matched-state visual equivalence and broader runtime coverage remain pending. `Runtime-Comparison` contains the prepared test export. Preview rendering is approximate.

## Continuing development

See [STATUS.md](STATUS.md). This repository preserves the development baseline independently of chat history. Future changes must also be committed and uploaded to GitHub to be backed up there.

License: the existing GPLv3 license in [LICENSE](LICENSE).

A manual review ZIP can be prepared with `powershell -NoProfile -File .\package-review.ps1`. It packages the editor and repository demo only, verifies every file hash, and does not launch any application. The latest attended and automated validation scopes are recorded above.
