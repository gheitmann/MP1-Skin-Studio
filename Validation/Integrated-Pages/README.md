# Integrated pages/XML browser with hover-wheel scrolling

Date: October 20, 2026. Parent source checkpoint: 5786b77.

## User-facing change

PAGES / XML now uses an always-visible sidebar ListBox instead of the ChoiceBox
popup. A literal case-insensitive filename filter narrows all available files.
The count and no-matches state explain the result; filtering never opens a page.
Escape in the filter restores all files and the active row. A horizontal
SplitContainer divider lets the owner resize files versus the element tree.

The native list scrolls vertically/horizontally. Its managed wheel handler and
message filter route vertical wheel events over its client area even when a
property input retains focus. Scrolling changes TopIndex only; selection, page,
preview, pending drafts and save state remain intact. Partial deltas accumulate
and system scroll-line/page preferences are honored. Events outside the list,
from another form, or while the owner is disabled are not intercepted. The
message filter is removed when the handle is destroyed.

Clicking/selecting another file uses the existing page loader. Pending property
inputs restore the active row and show Apply/Revert status guidance without a
modal warning. Failed loads retain the existing loader rollback and restore the
row. Skin/theme population clears stale filename filters. XML editing, model
persistence, export and rendering logic are unchanged.

## Fresh extracted-package evidence

- Twelve targeted synthetic form/message scenarios passed: all 449 DefaultWideHD
  files embedded without popup; wheel routing while a property retains focus;
  partial deltas/bounds without selection events; outside/other-form exclusion;
  literal filtering and draft preservation; empty results; real selection event
  loading Video with retained XML edit/save/reopen/export/restored Undo/Redo;
  pending selection rollback without a modal; failed-load rollback; reload and
  minimum-window layout; filter lifecycle cleanup; current menu integration.
- Current menu integration passed 12 assertions. Its former page-menu assertions
  now exercise direct integrated selection; unrelated menu lifetime/anchor/
  callback cases retain their original behavior.
- Twelve complete demo workflows and twelve clean-session workflows passed.
- All 488 installed DefaultWideHD XML and 26 bundled asset/source hashes stayed
  unchanged; the combined final gate passed all 31 manifest payload hashes.
- Application-generated renders at 1520 x 960 and the minimum 1180 x 780 were
  inspected. File filtering/list/divider and the element tree remain accessible.
  These are synthetic form renders, not desktop captures.

The packaged executable was then opened in Windows. A current observation shows
the owner already using its integrated filter (plugin, five of 449 entries)
while the myHome preview remains. After the user-input notice, no further app
input was injected. Desktop wheel testing is not claimed; production wheel
messages, focus preservation and selection invariance are covered synthetically.
The owner's filter, zoom and session remain untouched.

Executable SHA-256: `f8700978466e737c34945beb87f98b9dfe8b8d00fdbd68716a8bede22b052cdb`.
Accepted candidate ZIP SHA-256:
`44315395ae47b829815bd93c6c9ab982b4037552d172a7b7150fdb470ad17daf`.
The final ZIP changes only its revision note; all 31 accepted payload files and
their manifest must match exactly. No installed skin resources are bundled.

## Reproduce

Build/package the current source and choose a fresh output directory:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 `
  -PackageZip C:\review\MP1-Skin-Studio-Review.zip `
  -OutputDirectory C:\review\integrated-pages -SkipFullUi `
  -IntegratedPageSkinDirectory 'C:\ProgramData\Team MediaPortal\MediaPortal\skin\DefaultWideHD'
```

Run-Probe.ps1 accepts PackageDirectory, SkinDirectory and a fresh OutputDirectory
for the targeted checks alone. It copies the demo beside its own executable for
the existing menu integration, and keeps generated projects/exports/renders in
its output folder. It does not write installed XML.

## Historical bounds

Previous 402 core, ten XML-review, 18 transition, eight startup and 204 broad UI
checks remain historical. The earlier large-selector probe expects ChoiceBox
and only applies to its historical executable; its page control was deliberately
replaced here. Broad visual/runtime fidelity remains outside this input/browser
change. The attended observation used the Computer Use skill.
