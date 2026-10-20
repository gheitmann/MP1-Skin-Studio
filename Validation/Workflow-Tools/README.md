# Integrated Toolbox, grid settings and independent geometry locks

October 20, 2026. Completes the four legacy SkinEditor recommendations together
with the preceding property/font milestone. No legacy serializer was imported.

## Behavior

- Integrated side palette with text/button/image tools and filterable Media
  thumbnails; drag onto the canvas or click a tool for default placement.
- One image from active skin/theme Media can also be dropped from Explorer.
  External files must first be placed in Media. No automatic imports/skin writes.
- Drops account for zoom/pan and snapping, create top-level controls in the
  current page XML and undo as one action. Images use natural dimensions and
  preserved proportions. Failed/rejected drops report no OLE copy operation.
- Pending inputs, Navigation mode, stale tool roots, multiple files,
  unavailable/overridden/reparse paths and outside-preview drops are rejected.
- Grid/Snap and distance presets persist in the editor project. Zoom-adaptive
  grid display does not change the selected snap distance.
- Position and size protection remain independent from whole-element locks,
  inherit through groups, and survive source ordinal remapping/undo/reload.
  Direct geometry edits/resets and mixed batches retain atomic protection.
- Format 5 persists new flags/options. Formats 1–4 load with legacy defaults.
  Old editors reject format 5; XML export format is unchanged.

## Fresh acceptance

| Scope | Result |
| --- | --- |
| Integrated development core | 438 passed, zero failed; eight new cases |
| Integrated development broad UI | 229 passed, zero failed; thirteen new cases |
| Complete demo workflows | 12 passed |
| Clean-session workflows | 12 passed |
| Integrated payload preservation | 32 hashes unchanged |
| Final focused workflow checks | 17 passed, zero failed |
| Final selector/scroll/menu checks | 25 passed, zero failed |
| Final installed DefaultWideHD XML | 488 SHA256 hashes unchanged |

Broad development executable SHA256:
bb171bb9ecec4fc27fa5a95f7fed9dcf101b4946a6b1d8d195219c57c49ced1a.
Final executable SHA256:
fa023c303c9bc841fb5cf3697256e8dbba0825466267ff9a2cd580249f7f8cfc.
Final polish corrected the OLE success effect and made the command strip wrap,
then placed the Toolbox beside the canvas to preserve useful preview space at
minimum size. These changed paths were checked by the final focused/selector
suites; the full broad suite was not repeated after that UI polish.

The first run exposed a locked resize-corner gesture falling through to a move;
the corrected gesture passed subsequent suites. A selector-harness assumption
of exactly four scroll hosts was updated to check the four actual main content
hosts, since Toolbox adds another dark scroll host. Original assertions about
scrolling, colors, overlay order and draft protection remain.

Synthetic default/minimum form renders were inspected. No attended native OLE
drag or MediaPortal runtime visual equivalence is claimed. Owner windows were
not controlled and no installed skin was changed.

```powershell
.\Validation\Workflow-Tools\Run-Probe.ps1 -PackageDirectory <package> -OutputDirectory <fresh-folder> -SkinDirectory <DefaultWideHD>
```

The isolated probe invokes canvas drag events, modal/session paths, minimum-size
layout and installed Media discovery. It preserves before/after installed XML
hashes and saves synthetic screenshots outside the tracked report folder.
