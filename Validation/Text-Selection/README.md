# Caption-sized label selection

Date: October 20, 2026. Parent: 9023fe6.

The reported DefaultWideHD MediaTip.xml Music Album Artist Label has no width or
height. The designer substituted 400 by 64 units, spanning adjacent Artists and
the value column even though the text itself is single-line. Runtime text drawing
already used font metrics. This was an editor selection/hit-target mismatch.

Label/fadelabel selection now uses the same caption, font, alignment and clipping
as SurfaceText. A glyph path removes unused line leading and derives a tight ink
outline with a one-unit margin. The raw XML/layout rectangle is unchanged. Click,
overlap menus and double-click use this caption geometry. Single-line captions
remain draggable, with the existing grid snapping and transaction/Undo behavior.
Their resize corner is suppressed because caption ink is not an XML field size;
Width/Height remain editable in Properties, with a selection hint explaining this.
Buttons, images, collection surfaces, groups and multiline textboxes retain their
existing field frames. Empty/whitespace captions retain a measured-line fallback
and remain accessible in the tree. Captions over 8192 characters avoid glyph-path
expansion and use their measured line box. Font-family substitution remains as
documented for the approximate renderer.

Pointer inspections share one Graphics instance as well as their existing layout
and visibility scopes, releasing it before callbacks. Actual dragging still uses
the unchanged logical position rectangles. No font-derived dimensions are written
to XML and no skin geometry is automatically resized.

## Fresh evidence

- 416 core checks passed, including two new row/column, alignment/clipping,
  non-text-frame and source-preservation checks.
- 58 targeted synthetic Windows Forms assertions passed: the actual MediaTip
  adjacent rows, overlap menu, double-click/editor cancellation, dragging with
  snapping, exact Undo, explicit Width editing and no invented Height; plus the
  existing designer, search and text-edit UI suites.
- Ten real DefaultWideHD readable-preview scenarios, twelve complete demo
  edit/save/reopen/export/Undo/Redo workflows and twelve clean-session workflows
  passed against the extracted candidate.
- All 488 installed XML, 26 demo assets and 31 payload hashes stayed unchanged.
- The generated MediaTip form render was inspected. The album-label outline
  measured approximately X=631.3, Y=282.0, W=158.9, H=22.1, compared with raw
  X=632, Y=274, W=400, H=64. The Artists ink starts below Y=310.

The prior 205 broad UI report and renderer/load timing/pixel-comparison reports
are historical. A focused set of relevant UI suites was run for this change
instead of repeating every unrelated dialog. No new installed-client rendering
or attended desktop-input claim is made. Tests operate on in-memory changes and
isolated project/export outputs; original skin files stay untouched.

Accepted executable SHA256:
48bbcfb95632c401ac6843fa2073115032b695cef10adab0f2e4062efed1c384

```powershell
.\Validation\Text-Selection\Run-Probe.ps1 -PackageDirectory C:\ReviewPackage `
  -OutputDirectory C:\FreshOutput -SkinDirectory C:\DefaultWideHD
```

Final delivery retains the accepted executable/sample bytes; the user guide,
release notes and revision metadata are refreshed and the ZIP manifest verified.
