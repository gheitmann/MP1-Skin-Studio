# MP1 Skin Studio

A visual Windows skin editor for MediaPortal 1, built around visual editing and source-preserving XML changes. The current preview is **v0.9.0-preview.2**, based on the 0.9 prototype.

## Download

Get the portable Windows ZIP and SHA256 checksum from [v0.9.0-preview.2](https://github.com/gheitmann/MP1-Skin-Studio/releases/tag/v0.9.0-preview.2). Extract the complete archive and run `MP1SkinStudio.exe`. Windows and .NET Framework 4.x are required. The package includes twelve demo scenes and a quick start guide.

This preview provides a practical visual guide; rendering remains approximate. Start from an independent skin copy using **Copy skin …**. See [release notes](docs/RELEASE_NOTES.md) and [practical scope](docs/RELEASE_READINESS.md).

## Build and run

The development package includes [LiveBridge 1.1](docs/LIVE_COVERAGE.md), extending
scrolling text, lists, filmstrips/galleries and group alignment. The ready status
shows the loaded bridge version. A plugin update requires closing MP once;
ordinary live edits continue without page reload or restart.

The development build also offers [Save skin …](docs/SAVE_SKIN_COPY.md): create
a complete output copy once, then update its reviewed XML with conflict checks
and automatic backups while editing and live preview continue.

The development build adds [native live preview in MediaPortal 1.39](docs/LIVE_PREVIEW.md).
It includes a local process plugin: valid input drafts and canvas gestures update
the active MP controls without Apply, XML export or a restart. Enable **Live**
beside Toolbox after the one-time plugin setup. Disconnect restores preview
overrides; saving/exporting remains explicit. The released preview.2 assets do
not include this new feature.

The review ZIP runs directly after complete extraction: see [Quick start](docs/QUICK_START.md). Building from source requires Windows, .NET Framework 4.x and its C# compiler. From the repository directory:

```powershell
powershell -NoProfile -File .\build.ps1
.\MP1SkinStudio.exe
```

Demo assets are included under `Samples`. See [User Guide](docs/USER_GUIDE.md) for workflows and limitations. English is the default for documentation, code comments, application text and GitHub discussions.

## Features

Preview.2 adds **collapsible property groups with contextual help**, a
**searchable font picker**, an integrated **drag-and-drop Toolbox**, configurable
grid/snapping and separate position/size protection. Project format 5 retains
the new settings while reading existing formats 1–4. See
[workflow tools validation](Validation/Workflow-Tools/README.md) and
[property tools validation](Validation/Property-Tools/README.md).

Visual position, size, text and texture editing; multiple selection and alignment; layer ordering and deletion; nested groups, creation from consecutive selections and neutral-group removal; StackLayout and GridLayout previews; undo/redo; project persistence; and XML export. Preview tools include language resources with fallback, simulated states and sample data, plus approximate list, grid, filmstrip and static coverflow rendering.

## Validation

Preview.2 acceptance passed 438 core checks, 229 broad UI checks and 24 complete
demo/session workflows on the integrated development build. Final OLE and
responsive-layout polish passed 17 targeted workflows and 25 selector/scroll/menu
cases; 488 installed XML hashes stayed unchanged. Exact build scopes are recorded
in [workflow validation](Validation/Workflow-Tools/README.md). The release ZIP
was also downloaded anonymously and its SHA256 matched the published asset.

Public-preview source checkpoint (October 20): automatic skin discovery, **Select different Skin Folder …**, complete independent skin copying, caption/focus alignment, a wider sidebar, dark scrollbars/dropdowns and a matching native title bar. Latest focused validation passed 25 selector/sidebar/menu/scroll scenarios and ten readable-preview scenarios with source/demo hashes unchanged. Earlier acceptance passed 430 core checks, eleven copy-dialog scenarios, twelve demo workflows and twelve clean-session workflows. A full DefaultWideHD copy matched all 3,399 files and preserved every original. Reports below retain their own dates and scopes; they do not imply a new full-suite run for publication.

Previous source checkpoint (October 20): **automatic skin selection and wider dark sidebar**. SKIN above THEME lists detected installation folders/current siblings/the demo and refreshes when opened. The left column is 30% wider, with matching dark scrollbars in the main lists, tree, properties and information region. Skin switching preserves existing session protections; preview tools wrap at minimum width. Fresh checks: 420 core, 22 skin/sidebar/scrolling, twelve page, ten readability and 24 demo/session workflows. See [validation](Validation/Skin-Selector/README.md).

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

