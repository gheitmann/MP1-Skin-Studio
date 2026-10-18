# Development status and next steps

Date: October 18, 2026. Imported version: 0.9.

## Baseline

Imported from the complete locally extracted `MP1-Skin-Studio-0.9` release package. Includes source code, tests, samples, build script, existing license, historical validation reports and runtime test materials. Installers, compiled binaries, error logs and temporary working directories are excluded.

All documentation and validation descriptions use English. Intentional multilingual fixtures and original external schema identifiers are preserved. New documentation, commit messages, issues and pull requests use English unless requested otherwise.

## English migration

Repository paths, documentation, application messages and test descriptions use English.
See [migration notes](docs/ENGLISH_MIGRATION.md). Fresh validation: **189 core checks and 185 UI checks passed**, with reports in [Validation/English-Migration](Validation/English-Migration). Build and runtime-export XML integrity checks also passed. New projects should start in English,
with a private GitHub repository and a maintained status document.

## GridLayout milestone

Added static GridLayout measurement and arrangement, including Titan's vertical
fill, row precedence, margins, alignment, stretching and MP1 visibility behavior.
Calculated rectangles drive rendering and mouse selection without editing XML.
Nested stack/grid groups and undo refresh are covered. The existing group-layout
dialog edits StackLayout; a separate grid dialog now edits GridLayout rules.

Fresh validation: **200 core checks, 188 UI checks and 10 native GridLayout checks
passed**. The native checks ran against the installed original MP1 1.39 Core.dll,
using a separate runtime copy and isolated directory configuration. See
[reports and reproduction notes](Validation/Grid-Preview/README.md).
The demo `Samples/Titan-Sample/demo.grid.xml` and its rendered screenshot were
visually inspected. Two remaining mixed-language compiler/error messages were
also translated into English.

## Remaining work

The latest completed milestone adds an **Edit grid layout** dialog with column
and row counts, independent spacing, fill direction, binding replacement
protection, and exact undo/redo. Child XML positions are preserved. Fresh
validation: **203 core checks and 195 UI checks passed**; the dialog render was
visually inspected. See [Grid dialog validation](Validation/Grid-Dialog/README.md).

1. Complete a real MediaPortal 1 client comparison using the user's installed client, as explicitly requested. The app-server/PATH blocker has been resolved and the installed MP1 1.39 Titan Home screen was observed after an earlier manual start. A fresh settings export changes only spacing from 7 to 15 pixels. On the latest automated attempt (October 18, approximately 19:39 local time), launch authorization succeeded, but MP1 showed a June 2010 DirectX prerequisite error despite D3DX9_43.dll existing in both Windows system directories and the earlier positive component probe. The cause remains unresolved; a manual client start is the next comparison point. Settings rendering, focus and navigation remain pending. See [current client validation](Validation/Client-Comparison/README.md). No original skin file was changed.
2. Improve preview fidelity for complex collections, font metrics, image masks, dynamic conditions and animations. Coverflow is currently a static approximation.
3. Extend design tools: create/ungroup groups, move controls between groups, and provide convenient global style editing.
4. Continue usability and stability checks on large real-world skins.

## Deferred backlog

- [Issue #1](https://github.com/gheitmann/MP1-Skin-Studio/issues/1): assess removal
  or replacement of legacy DirectX runtime dependencies in MediaPortal 1.
  Explicitly deferred by the user; do not investigate or implement now.

## Rules for continuing work

- Preserve original skins; use isolated copies and exports.
- Do not export preview-only state as skin XML changes.
- Distinguish inherited test reports from newly executed validation.
- The missing chat project list is independent of the source code saved here.
- Local window control was unavailable during import. Do not claim a visible client comparison has been completed.
