# Grouped properties and font preview

October 20, 2026. Read-only legacy SkinEditor review led to this first
implementation package; no legacy parser or full-file serializer was imported.

- Six applicable property categories; collapsible without discarding drafts.
- Existing local/inherited/reset information, multi-selection, binding and
  element protection retained. Apply/Revert includes collapsed fields.
- Context help appears on keyboard focus and label hover.
- Font picker searches skin names/aliases, previews editable sample text and
  reports declared height, effective pixel size, family/style and substitutes.
- Same font scaling as the existing canvas at 100% skin scale. Unavailable font
  families use Segoe UI; shared font definitions are never edited.
- Use font changes the draft; Cancel is inert; Apply/Undo uses existing XML paths.

## Fresh acceptance

| Scope | Result |
| --- | --- |
| Development core suite | 430 passed, zero failed |
| Development broad UI suite | 216 passed, zero failed; eleven new cases |
| Demo edit/save/reopen/export/undo workflows | 12 passed |
| Clean-session workflows | 12 passed |
| Development package hashes | 32 payload files unchanged |
| Final targeted property/font probe | 15 passed, zero failed |
| Final selector/scroll/menu probe | 25 passed, zero failed |
| Final installed DefaultWideHD XML preservation | 488 SHA256 hashes unchanged |

The broad development build executable is SHA256
6bd224ddb31620ffae18735b520ea8a7d396a8c05a4126bc097fe97dc6c4408e.
The final executable is SHA256
640bc188073790dad8e7f6d5a2749f6d6ceea41cf6739db44c6fbd07029f229f.
Between them: added the visible filter label, bounded initial sample text to 512
characters, and guarded invalid fallback font heights. The final targeted suite
also covers empty font lists, invalid height and installed skin fonts. Broad
checks were not repeated after this localized polish.

Synthetic off-screen form renders of the main panel, demo font picker and
DefaultWideHD font picker were inspected. This is not attended desktop or
MediaPortal runtime acceptance. Native chrome is not represented reliably in
DrawToBitmap. No installed skin edits, owner-window actions or font installs.

Run the focused probe against an extracted development package:

```powershell
.\Validation\Property-Tools\Run-Probe.ps1 -PackageDirectory <package> -OutputDirectory <fresh-folder> -SkinDirectory <DefaultWideHD>
```

The probe invokes real modal picker paths with owned test timers, generates
off-screen renders and checks installed XML hashes before/after. Reports in
this folder preserve result text only; generated test fixtures stay outside it.
The public preview tag/assets are not changed. Drag/drop insertion and adjustable
snapping/separate movement/size locks remain future work.
