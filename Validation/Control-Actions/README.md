# Slider/spin arrow preview boundaries

Executed on October 20, 2026. Fresh results:

- **378 core checks passed**, including eight new checks for reserved slider
  directions, horizontal/default/numeric and vertical spin orientation, unknown
  or property-driven orientation, target visibility/ID protection, informational
  diagnostics, saved XML/export/restored Undo/Redo and unaffected normal controls.
- **Ten real Titan workflows passed** across GUI, OSD and Slide Show Settings,
  TVOSD and the Video audio menu in Default and Christmas. Each includes width
  editing, save/reopen, exact export, normal/restored Undo/Redo and saved focus.
  **28 explicit cross-axis routes accepted; 92 routes guarded** for limited
  interaction or existing missing/ambiguous/unverified targets. **Twelve original
  loaded XML hashes unchanged**, captured before edits and checked afterward.
- **Sixteen synthetic Windows canvas arrow events passed**. Reserved/unknown
  directions retain focus, project signature and explanatory status. Cross-axis
  routes update focus/selection correctly. Four informational interaction
  findings appear; XML and undo stay clean; focus 60 survives save/reopen.
- **Twelve full demo workflows passed from the extracted candidate package**,
  with all 29 payload hashes unchanged. No new demo assets were needed.

The preceding broad 204 UI checks and twelve clean-session checks were not
repeated because this change is confined to control-action routing/diagnostics.
No new attended desktop input or installed MediaPortal client run occurred.

## Correction and primary-source basis

Previously an explicit arrow mapping could be followed even when the native
control handles that action before base navigation. The raw XML editor/audit
still exposes those mappings, but preview navigation now checks the source
control first.

The primary [GUISliderControl.OnAction source](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUISliderControl.cs)
handles Left/Right as value adjustment and delegates other actions to its base.
Preview Left/Right therefore stays unverified without changing values or focus;
Up/Down explicit mappings retain normal target checks.

The primary [GUISpinControl.OnAction source](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUISpinControl.cs)
can consume orientation-axis arrows according to inner-button selection and
available value movement. The preview lacks that runtime state, so it
conservatively guards horizontal Left/Right or vertical Up/Down. This does not
claim native consumption is unconditional. Known cross-axis explicit mappings
remain usable; unknown/dynamic orientation blocks all arrows.

XML links, control values, callbacks and exports are unchanged by guarded input.
Slider/spin rendering and value simulation remain unsupported, now exposed as
informational PREVIEW_INTERACTION findings. Hidden editor controls are omitted.
Checkmark diagnostics also received a specific message, rather than being
mistakenly labeled as generic unresolved text. Report output remains bounded.

## Reproduce

Build with build.ps1 and run --selftest with an absolute output directory.
Run-Probes.ps1 compiles the two integration probes beside a separate executable
copy. Supply an isolated Titan skin copy. The form probe creates only its own
small authored scene and injects events directly into that form, not the desktop.

```powershell
.\Validation\Control-Actions\Run-Probes.ps1 -SkinDirectory C:\SkinCopies\Titan
```

Package regression can be reproduced with MatrixProbe.cs from Package-Readiness
against an extracted review ZIP. Core-Results.txt, Titan-Results.txt,
Form-Results.txt and Package-Results.txt contain the fresh reports. Generated
projects, authored form fixture and intermediate packages remain local work
files; no real skin resources or client configuration are shipped.
