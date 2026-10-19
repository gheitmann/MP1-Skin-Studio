# Development status and next steps

Date: October 19, 2026. Imported version: 0.9.

## Latest source milestone: preview text expressions

A bounded subset of nested MP1 text expressions now evaluates in preview text:
trim/replace/length, string comparisons, eq/neq and iif. Unknown functions and
missing runtime values preserve the whole expression. Embedded local define
chains resolve without changing XML. See
[expression validation](Validation/Preview-Expressions/README.md).

Fresh Linux validation: all production sources compile with Mono; 25 targeted
console checks passed. The Windows full suite and real Titan visuals were not
rerun. Historical Windows counts below are not fresh validation of this change.
The current session cannot access the Windows desktop. Complex-page visibility
and remaining runtime expressions still need work. DirectX replacement remains deferred.

## Current priority: reliable core editing

The owner prioritized practical edit/save/reopen/export behavior over auxiliary
features. The actual Titan Settings UI workflow and an earlier exported candidate's
installed-MP1 display/navigation passed before automation was suspended. Real
Video/list, multi-page and theme workflows now have console/model validation.
These successes do not establish complete visual fidelity or plugin equivalence.

The owner explicitly reauthorized attended Windows automation on October 19.
The real Titan Video edit/save/reopen/export UI workflow now passed, with manual
completion of native dialogs that Computer Use could not target. No new MP1
client run occurred. DirectX replacement remains deferred.

Latest joint validation: fresh final build, **237 core checks and 39 targeted
checks passed**. Coverage includes filmstrip geometry, string conditions, real
Titan imported-list edits, save/export, multiple pages and Christmas/default
theme preparation. Write-failure tests preserve existing archives and clean
intermediate files. Revert Inputs and the Save Project pending-input guard now have attended UI
validation; other new UI paths remain unverified. Prior UI counts are historical.
See [model performance and batch validation](Validation/Model-Performance/README.md).

See [attended Titan Video UI validation](Validation/Attended-Video-UI/README.md).
The complex-page preview visibly contains unresolved expressions and overlapping
placeholders. Useful preview states and expression evaluation are the next core
priority; the new export still needs MP1 runtime validation. Auxiliary group/style
conveniences remain lower priority. Historical milestones follow.

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

1. Inspect and test collection geometry and dynamic-condition limitations at source/model level. The complex Titan Video model/persistence/export gate now passes; visual/runtime verification is suspended after the reported system freeze. See Validation/Complex-Page/README.md.
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

## Real-page UI feasibility gate

October 18, 2026: attended editor control verified tree/canvas selection,
property changes, direct drag with grid snapping, exact undo/redo, project save
and reopen, and native-dialog ZIP export on installed Titan Settings. Five
independent artifact checks passed; original settings.xml is unchanged.
See [UI workflow](Validation/Core-Workflow/UI-Workflow.md). No product code
changed and the previous full-suite counts were not rerun. The UI-created
EDITOR TEST candidate was not loaded into MP1; the earlier STUDIO TEST candidate
already passed installed-client display/navigation.

The owner confirmed normal MP1 restarted successfully after another reported
DirectX startup error. No startup or DirectX investigation was performed.
The next core gate is a complex real list/facade page, with auxiliary features
still deferred. Historical authorization/startup blockers above are superseded
only by their explicitly recorded attended successes.

## Complex-page console gate and automation suspension

October 18, 2026: after the failed window-control attempt, the owner reported
that the PC became practically unresponsive and required a hard reset. Cause
is unconfirmed. Windows automation is suspended: no app launches, capture or
Computer Use retries. The owner authorized source-code and test work only.
This restriction supersedes earlier attended-retry permissions and is recorded
in AGENTS.md. DirectX replacement remains deferred.

The dedicated console probe passed eleven stages on real Titan myvideo.xml:
211 controls, 17 loaded sources, five facade views, selected sample data and
scrolling, isolated imported-list edits, exact undo/redo, project persistence,
ZIP export and unchanged original-source hashes. No production source changed.
See [complex-page model gate](Validation/Complex-Page/README.md).

The complex page's visible rendering and runtime behavior remain unverified.
Prior full-suite counts were not rerun. Continue with model-level investigation
of preview limitations; auxiliary conveniences remain lower priority.

## Filmstrip text geometry correction

October 18, 2026: fixed filmstrip title geometry ignoring textXOff/textYOff and
texture label dimensions. Installed Titan uses textYOff=-2000 intentionally;
its titles now remain outside the collection viewport in calculated geometry.
A regression probe failed before the fix and passed all six checks afterward.
The complex-page probe passed all eleven stages again. Fresh build and core
suite: 222 passed, zero failed. See
[geometry validation](Validation/Collection-Geometry/README.md).

No Windows automation, capture, GUI launch or MediaPortal launch was used.
Core tests ran through a console entry point; the UI suite remains suspended.
Visual equivalence is not established by numeric checks. Continue source/model
work on dynamic conditions and collection preview limits; DirectX stays deferred.

## Known-value string visibility conditions

Continuation begun October 18, 2026: the preview now evaluates two-argument
string.contains and string.starts when operands are known, using captured
sample/define scopes. Native trimming, case normalization and empty-search
semantics also apply to equality. Unknown values/functions remain unresolved.
The actual Titan scrolling popup was checked with explicit yes/no/missing flags.

Fresh console validation: ten dedicated checks and 223 core checks passed,
zero failures. See [string-condition validation](Validation/String-Conditions/README.md).
No app GUI, MediaPortal, capture or Computer Use was launched. UI/runtime
verification remains suspended; DirectX replacement remains deferred.

## Collection property editing

October 19, 2026: added compatible property-panel fields for list row height,
spacing, icon sizes, filmstrip texture/item/thumbnail sizes and small/large
thumbnail-grid sizes. Shared catalog definitions control type applicability and
numeric validation. Mixed selections expose only fields supported by all items;
existing protection, binding replacement, reset and transaction paths are reused.

Fresh build: 225 core checks passed. The extended real Titan console probe
passed all eleven stages, including row geometry after height/spacing changes,
exact undo/redo, project persistence and exported shared XML. Original source
hashes remain unchanged. See
[collection property validation](Validation/Collection-Properties/README.md).

The new GUI wiring compiles but has not been exercised or visually inspected.
No GUI/client launch, window capture or Computer Use was performed. Continue
model-level core work; manual/UI verification remains pending and DirectX deferred.

## Failed page loads preserve the working session

October 19, 2026: OpenPage now restores the existing active page on a loading
exception, including control identity, source cache, metadata and fonts. Missing,
malformed and out-of-root page requests no longer clear the working session.
A new regression failed before the fix and passes afterward. Fresh build and
console core suite: 226 passed, zero failed.

Nine real Titan multi-page console checks also passed: shared facade edits
appear on both video pages, three edited XML sources survive page switching,
cross-page undo/redo and save/reopen, and export contains all changes. Original
files remain unchanged. See [page loading validation](Validation/Page-Loading/README.md).
No GUI/client launch, capture or Computer Use was performed. Visual/UI/runtime
verification remains pending and DirectX replacement stays deferred.

## Visibility condition editing

October 19, 2026: added Visibility condition to the property panel, reusing
local reset/provenance, binding protection, multi-selection and transaction
behavior. Unknown runtime expressions are preserved and remain unresolved in
preview. Parent/include conditions continue to apply to empty local conditions.

Fresh console core run: 228 checks passed, zero failures, including new group
visibility/descendant/attribute/undo tests and unsupported-condition project/ZIP
preservation. See [visibility editing](Validation/Visibility-Editing/README.md).
The UI wiring is compiled but remains manually unverified. No editor/client GUI,
capture or Computer Use was used; the automation suspension and deferred DirectX
task remain in force.

## Unapplied property input guards

October 19, 2026: extended existing pending-field protection to page/theme and
session switching, save/export, undo/redo, insert/duplicate/alignment/navigation,
and editor protection/visibility refresh actions. Selector values are restored
on blocked changes; pending input marks the session unsaved and closing confirms
discard. No draft values are automatically committed or silently saved.

Fresh final build and console core suite: 228 passed, zero failed. This verifies
compilation and existing model regressions, not the new GUI event paths.
Manual acceptance cases are recorded in
[pending-input validation](Validation/Pending-Properties/README.md).
No GUI/client launch, capture or Computer Use was performed. UI validation
remains pending under the automation suspension; DirectX stays deferred.

## Combined session reliability checkpoint

October 19, 2026: session preparation now loads/validates pages, theme lists and
preview state before replacing the active editor project. Theme preparation
restores the prior theme on failure and preserves base edits/history on success.
Revert Inputs restores uncommitted fields without changing XML or undo history.

Fresh final build: 233 core checks and 39 targeted real-skin/model checks passed.
The latter include installed Christmas/default theme preparation and three-file
edit/save/reopen/export. Forced project/ZIP replacement failures retain usable
previous files, unchanged in-memory edits and no leftover temporary archives.
See Validation/Session-Reliability/README.md. No GUI/client launch, capture or
Computer Use was performed; interface paths remain manually unverified.

## Cached source ordinals, safe batch merging and review package

October 19, 2026: cached XML control ordinals remove repeated prefix scans while
loading a page. A bounded 1,600-control benchmark improved from 101.75 ms to
4.61 ms median for cached model loading; this does not measure GUI performance.
EditMany now merges compatible entries for one control, rejects conflicting
values before mutation and validates stale duplicates. A rejected batch retains
redo history; corrected edits after rollback remain usable.

Fresh build: 237 core checks and 39 targeted console checks passed. All original
real-skin sources remain unchanged. package-review.ps1 produced and verified a
manual review ZIP (24 files plus notes and SHA256 manifest) without launching it.
See Validation/Model-Performance/README.md. Recent GUI paths remain manually
unverified; desktop automation and DirectX work remain suspended/deferred.

## Windows continuation validation

October 19, 2026: restored GitHub commit `4fbc846` with all 198 tracked file hashes verified. Fresh Windows .NET Framework build, 237 core checks and 25 preview-expression checks passed. The editor launched and its Titan sample was observed through Computer Use. Broader interaction, persistent permission behavior and MP1 runtime checks remain pending; no client was launched and no original skin changed. See [Windows checkpoint](Validation/Resume-Windows-20261019/README.md). DirectX stays deferred.

## Windows test environment access confirmed

October 19, 2026: attended access test launched the editor and installed MP1, opened/closed the editor Sample Data dialog, moved Titan Home focus from TV to Videos and opened the real Videos page. Successive inputs and captures completed without an observed approval loop. This validates current-session access only; no export was installed and original skins were not edited. See [access test](Validation/Windows-Access-20261019/README.md). DirectX remains deferred.

## Attended Titan Video preview comparison

October 19, 2026: fresh complex-page probe passed all eleven stages on a complete isolated Titan copy. Attended UI checks loaded myvideo.xml, selected the concrete list, hid unverified conditions, loaded German language without prefixes and saved a preview-only project. Full SHA256 comparison: 2,050 skin files, zero mismatches. The list selection closes the simulated side menu and localization fixes numeric title IDs, but unknown metadata and six movie sample rows still differ from the client's single Videos folder. No export was installed; runtime comparison remains pending. See [comparison findings](Validation/Titan-Video-Comparison-20261019/README.md).

## Video folder preview milestone

October 19, 2026: added Element > Video folder preview and fixed initialization of derived collection values after rebuilding preview state. One Videos folder and explicit empty movie metadata are preview-only; unrelated unknown values stay unresolved. Fresh final Windows validation: 239 core checks, 25 expression checks and eleven real Titan model stages passed. Attended UI confirmed the folder, count 1 and matching selected title; save/reopen retained sample and language state. 89 unresolved conditions and remaining placeholders prevent a claim of visual equivalence. No export was installed. See [folder preview](Validation/Video-Folder-Preview/README.md).
