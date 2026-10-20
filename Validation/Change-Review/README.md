# Session-wide XML change review

Date: October 20, 2026. Parent source checkpoint: 73ef1cb.

## Implemented workflow

Element > Review XML changes and Ctrl+Shift+E open an owned read-only snapshot
of every modified XML source retained in the session. The list includes loaded
shared includes, theme overrides and earlier pages; filtering uses literal
case-insensitive relative filenames. Original and edited text are shown side by
side, initially at their first differing character. Previous/Next covers every
section of up to 60,000 UTF-16 characters without truncating the available text.
First difference returns both panes to that location. This is not a semantic or
minimal line diff.

Metadata includes original encoding/BOM, exact original/edited byte counts and
SHA-256 hashes. The on-disk modified originals are checked for drift, absence or
inaccessibility. Refresh rebuilds the snapshot. Matching can become stale and
is not a complete backup/runtime check; export performs its own existing checks.
Pending property fields and preview-only settings are excluded. Opening,
filtering, selecting, refreshing and closing the review preserve XML, history,
selection, drafts and save/export status. Export retains its prior workflow.

## Fresh packaged acceptance

- 402 core checks passed, including eight new review regressions: clean/preview
  state exclusion, shared and earlier-page files, exact byte hashes, immutable
  snapshots through Undo/Redo, same-size drift/missing/restored sources, real
  exclusive source locks, UTF-16 BOM/Unicode, first difference/CR/LF lines and
  literal file filtering.
- Ten synthetic form workflows passed: actual file selection and details,
  read-only panes, filtering/empty state, refresh after source drift/restoration
  and Undo, complete large-file sections and boundaries, first-difference
  action, owned production review with pending fields, production Ctrl+Shift+E
  dispatch and the Element menu callback. All 26 demo hashes stayed unchanged.
- Twelve full demo edit/preview/save/reopen/exact-export/normal and restored
  Undo/Redo workflows and twelve clean-session workflows passed.
- All 31 manifest payload hashes stayed unchanged after the complete selected
  gate. An application-generated DrawToBitmap render was inspected: all file
  columns, full hash details, both panes, section navigation and actions are
  visible at 1220 x 790. This is a synthetic form render, not a desktop capture.

Executable SHA-256: `875c1447b568c36ce963392d1936749497b3604fc6393e03d89b6a3851c45de6`.
Accepted candidate ZIP SHA-256:
`e173fa500b5a1bb1fc75a6fa1815df3fe137c72ee22de1d6d4672b39fc9e1cab`.
The final ZIP changes only its source-revision note. All 31 payload files and
their manifest must match this accepted candidate exactly.

## Reproduce

Build/package the repository, then use a fresh acceptance directory:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 `
  -PackageZip C:\review\MP1-Skin-Studio-Review.zip `
  -OutputDirectory C:\review\change-review-checks `
  -SkipFullUi -IncludeCore -IncludeChanges
```

Run-Probe.ps1 also supports PackageDirectory and a fresh OutputDirectory to run
only the targeted review forms. The probe compiles against the chosen packaged
executable, uses its own fixtures/forms, writes its synthetic render and reports
in that output folder, and never edits installed skin files.

## Boundaries and historical evidence

No new attended desktop input, native file-picker operation or installed-client
run occurred. Previous 204 broad UI, 18 transition and eight startup scenarios
remain historical; the previous source 07e8ef2 Titan Default Home/Video client
checkpoint remains historical. Model editing, export and renderer behavior are
unchanged. Final visual review of an intended skin and broader page/theme/plugin
coverage retain the readiness guide's limits.
