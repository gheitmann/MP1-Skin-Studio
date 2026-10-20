# Checkmark preview and mixed navigation

Executed on October 20, 2026. Fresh results:

- **370 core checks passed**, including nine checkmark checks: selection samples,
  dimension aliases and exact reset/undo, ambiguity guards, label-side placement,
  invalid-layout diagnostics, mixed navigation and guards, production image
  pixels independent of focus, and exact project/export/restored Undo/Redo.
- **Eight complete real Titan workflows passed**, across Slide Show, Skip Steps,
  Music and Recording Settings in Default and Christmas: width editing,
  save/reopen, exact XML export, normal and restored Undo/Redo, and retained focus.
  **172 explicit routes accepted, 28 guarded; 12 original XML hashes unchanged**.
- **Six synthetic canvas arrow events passed through the Windows form**:
  881 → 882 → 883 → 884 → 883 → 882 → 881. Designer selection and focus agree;
  XML/history stay clean during navigation. All six checkmark-only properties
  show correct model values. Selected expression true → false Apply creates one
  XML undo step; save/reopen restores the edit and focus 882.
- Production state/edited form renders were visually inspected. The clean demo
  distinguishes selected, unselected and unverified marks, with readable labels.

Real Titan routes are explicit mapping regressions. New automatic mixed-type
navigation is covered by authored fixtures and demo.checkmark.xml. No new
attended keyboard session or installed MediaPortal client run occurred.

## Behavior and limitations

Selection chooses the two mark textures independently of focus. Mark dimensions
and label-side placement are static; text uses focused textcolor or unfocused
disabledcolor. The implementation was checked against the primary
[GUICheckMarkControl source](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUICheckMarkControl.cs),
particularly its render and arrow-action paths. No claim is made that GDI font
metrics, clipping, shadows, dynamic sizing or animation match the live client.

Empty selected defaults to false. Other expressions require a known true/false
result from current explicit samples; yes/no and unknown states are not coerced.
Unknown selection omits a guessed mark and supplies an informational Ctrl+F8
finding. Static mark dimensions are bounded to 0–8192, label length to 8192.
Unsupported alignments/property-driven dimensions remain unverified. Local
dimension aliases retain reference/style/local precedence and existing tag
spelling. Duplicate local aliases reject edits/reset before XML mutation.

Checkmarks join supported bounded geometric candidates. A mixed linear stack
uses geometry so a button cannot silently skip the checkmark. Selection state
does not decide focusability. Unknown visibility, duplicate IDs, collections and
other unsupported focusable types retain existing guards. Arrow navigation never
activates/toggles a mark or executes click callbacks. Runtime enable state and
other control-specific value interactions are not simulated.

The final renderer additionally bounds label work and respects a missing/disabled
font. Final core, Titan and form runs above include that change. No successful
unchanged broad matrices or historical native-math checks were repeated.

## Reproduce

Build with build.ps1, run --selftest with an absolute directory, then use
Run-Probes.ps1 with a separate Titan skin copy. The helper compiles TitanProbe.cs
and FormProbe.cs into a separate work directory. It injects events into its own
test form, not the desktop, and does not launch MediaPortal or alter configuration.

```powershell
.\Validation\Checkmark-Preview\Run-Probes.ps1 -SkinDirectory C:\SkinCopies\Titan
```

Core-Results.txt, Titan-Results.txt and Form-Results.txt are the fresh reports.
Generated projects and form renders remain local working files. The review ZIP
contains only the editor, bundled XML/assets, license, guide and package metadata.
