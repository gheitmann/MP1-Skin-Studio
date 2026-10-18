# Development status and next steps

Date: October 18, 2026. Imported version: 0.9.

## Current priority: prove the core workflow

The owner explicitly prioritized real edit/save/reopen/export/client validation
over further auxiliary features. Do not resume group/style conveniences until
this gate is resolved.

The installed Titan settings page passed the production-API edit/save/reopen/ZIP
chain for position, size, text, texture and menu spacing. The saved candidate was
visually observed in the editor. Its export was applied exactly to a separate
skin copy. The original skin hashes remain unchanged.

The initial automatic test launch was canceled without a targetable window.
The owner then manually started the same installed client with the prepared
isolated skin. Native Settings rendered STUDIO TEST, the changed icon and spacing;
focus wrapping and a submenu return passed. The test client was closed normally.
Automatic normal-start attempts did not expose a window, although the startup log
selected normal Titan; manual normal startup may still be needed. Actual real-page
mouse/keyboard editing is the next unproven gate. See the
[core workflow gate](Validation/Core-Workflow/README.md) and its API report.
No DirectX investigation or replacement was performed.

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

## Group creation milestone

Added **Group selected controls** to the Element menu. Consecutive siblings on
the open XML page can be wrapped in a neutral ordinary group, including nested
group subtrees. Child XML, positions and IDs are preserved. Undo/redo restores
structure and selection; project persistence retains control identities and flags.
Automatic layouts, locked descendants, shared-source edits and behavioral group
defaults are protected.

Fresh validation: **211 core checks and 199 UI checks passed** after a successful
Windows build. See [group creation validation](Validation/Group-Creation/README.md).
These editor tests did not use Computer Use or interact with MediaPortal.

## Text presentation milestone

Corrected the two editor defects found during the attended Titan comparison:
imported title/clock/date text is now presented after control textures, and font
scaling converts the integer MP1 point size to pixels. Deferred text retains its
original clip, including collection viewports. No skin XML is rewritten.

Fresh build, **213 core checks and 199 UI checks passed**. The installed Titan
Settings preview was rendered and visually inspected: title, sample clock/date
are visible and text sizing is closer to the earlier client observation.
See [text presentation validation](Validation/Text-Presentation/README.md).
No new client input was used. Font substitution, runtime labels/plugin state and
a full matched-state image/exported-spacing comparison remain open.

## Neutral ungroup milestone

Added **Ungroup selected group** with direct-child promotion, preserved nested
XML/comments/processing instructions, structural identity remapping and exact
in-session undo/redo of XML and selection. Neutral wrappers created by the
grouping tool are supported. Behavioral/nonneutral groups, layout ancestors,
locks, hidden wrappers, source boundaries and known group-ID references are
protected. Saved projects retain the resulting snapshot and original-baseline
undo, under the existing project-format contract.

Fresh build: **221 core checks and 203 UI checks passed**. See
[ungroup validation](Validation/Ungroup/README.md). No MediaPortal input or
configuration changes were needed. The DirectX task remains deferred.

## Remaining work

The preceding milestone added an **Edit grid layout** dialog with column
and row counts, independent spacing, fill direction, binding replacement
protection, and exact undo/redo. Child XML positions are preserved. Fresh
validation: **203 core checks and 195 UI checks passed**; the dialog render was
visually inspected. See [Grid dialog validation](Validation/Grid-Dialog/README.md).

1. Prove actual real-page UI editing (selection, property changes, save/reopen and export through the interface). The production-API round trip and installed-client candidate display/navigation have passed for Titan Settings. Verify manual normal MP1 startup if needed. See Validation/Core-Workflow/README.md.
2. Improve preview fidelity for complex collections, font metrics, image masks, dynamic conditions and animations. Coverflow is currently a static approximation.
3. Extend design tools: move controls between groups and provide convenient global style editing.
4. Continue usability and stability checks on large real-world skins.

Latest client observation (October 18, approximately 19:43 local time): the user
manually started installed MP1 successfully and the Titan Home screen was
captured. Activating that window was rejected with `Computer Use was not
approved to use mediaportal`. Desktop input authorization currently blocks the
remaining settings/navigation comparison; no additional input was attempted.

## Latest continuation checkpoint

October 18, 2026, 20:04–20:05 Europe/Berlin: resumed from GitHub commit
`4d7764d` and verified all 125 tracked baseline files by Git blob hash.
A fresh build succeeded; fresh core validation is in
[Resume checkpoint](Validation/Resume-20261018/README.md). Editor source was not changed.

Activation of the user's running MediaPortal succeeded and Titan Home was
observed, but the repeated approval loop recurred despite Always allow.
All Computer Use calls have been stopped, including activation and capture,
under the owner's explicit stop instruction. Do not retry during this continuation.
Track the blocker at [openai/codex#55114](https://github.com/openai/codex/issues/55114).
Settings rendering, focus/navigation and spacing comparison remain pending.
The DirectX dependency replacement task remains deferred.

## Attended client retry

October 18, after 22:26 Europe/Berlin: the owner explicitly authorized another
attempt with manual approval. Activation and keyboard input succeeded; Titan
Settings opened, and first/last focus wrapping was verified without toggling
settings. Persistent Always-allow behavior remains unverified. The earlier stop
remains the default for unattended window-control retries.

An initial installed-skin/editor comparison found missing title/clock content and
smaller text in the editor. Full matched-state image comparison and exported
spacing validation remain pending. See [attended retry](Validation/Client-Comparison/Attended-Retry.md).
No original skin or client configuration was changed; DirectX remains deferred.

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
