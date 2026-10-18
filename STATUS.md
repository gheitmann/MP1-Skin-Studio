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
dialog still edits StackLayout only; grid expressions can be edited as properties.

Fresh validation: **200 core checks, 188 UI checks and 10 native GridLayout checks
passed**. The native checks ran against the installed original MP1 1.39 Core.dll,
using a separate runtime copy and isolated directory configuration. See
[reports and reproduction notes](Validation/Grid-Preview/README.md).
The demo `Samples/Titan-Sample/demo.grid.xml` and its rendered screenshot were
visually inspected. Two remaining mixed-language compiler/error messages were
also translated into English.

## Remaining work

1. Complete a real MediaPortal 1 client comparison: apply the prepared Titan test export to an isolated copy, and compare rendering and remote-control navigation. The installation and a Titan startup/shutdown have now been confirmed from local files and the installed client log. Visible comparison remains blocked because the Windows automation runtime cannot launch its app-server, including after a JavaScript session reset. Runtime-Comparison records the original installation context; a new client comparison is still required.
2. Improve preview fidelity for complex collections, font metrics, image masks, dynamic conditions and animations. Coverflow is currently a static approximation.
3. Extend design tools: create/ungroup groups, move controls between groups, and provide convenient global style editing.
4. Continue usability and stability checks on large real-world skins.

## Rules for continuing work

- Preserve original skins; use isolated copies and exports.
- Do not export preview-only state as skin XML changes.
- Distinguish inherited test reports from newly executed validation.
- The missing chat project list is independent of the source code saved here.
- Local window control was unavailable during import. Do not claim a visible client comparison has been completed.
