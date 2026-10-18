# MP1 Skin Studio user guide

Version 0.9 · October 18, 2026

## Getting started

1. Build the application with `powershell -NoProfile -File .\build.ps1`.
2. Start `MP1SkinStudio.exe`. The bundled demo loads automatically.
3. Select an element in the preview or element tree.
4. Drag to move it; use the lower-right handle to resize it.
5. Edit properties on the right and choose **Apply Properties**.
6. Save an editor project to continue later, or choose **Export Changes as ZIP**.

## Open skins, pages and themes

Choose **Open skin directory** and select the directory containing references.xml,
fonts.xml and Media. Use the page selector to choose a page and the theme selector
to switch themes. Includes and theme overrides are resolved for the selected page.
The complete Titan skin is not bundled; Samples/Titan-Sample is a small demo.

The element tree records each element's source file. Edits to shared include files
affect every page using that file when the export is applied.

## Select, move and arrange elements

Click on the canvas or in the tree. Hold CTRL to select multiple elements.
Drag a selection to move all selected controls in one transaction.
The resize handle changes the selected control's dimensions.
Grid display and 10-pixel snapping can be toggled independently.

The Align menu aligns edges or centers and distributes equal spacing.
At least two controls are required for alignment and three for distribution.
Free nested controls can be aligned only when they share the same group.
Right-aligned labels retain their anchor convention.

Use the Element menu or tree context menu to change layer order, remove elements,
lock/unlock controls or hide them in the editor. Locks and hiding follow element
identity through reordering, undo/redo and project restoration.
[L] identifies locked elements; [H] identifies editor-hidden elements.

Removal shows possible references on the open page and warns about shared includes.
Explicit approval is required where applicable. References are not rewritten.
A removal operation remains one undo step, including nested controls.

Insert text, buttons or images with a new available ID. Duplication preserves the
original and its unknown XML. Group duplication is not yet supported.
Control IDs and types are protected against ordinary property editing.

## Find elements and inspect overlap

Search the open page and loaded includes by name, ID, type, text, source or values.
All words must match. Use filters such as id:10, type:button and source:menu.
Enter selects the next match, Shift+Enter the previous match and Escape clears the filter.
The search filters the tree while the full preview remains visible.
Ancestor groups remain visible as context for nested matches.

Right-click the canvas to choose an obscured element from a front-to-back hit list.
Locked elements remain available for inspection. Editor-hidden elements are excluded.

## Groups and automatic layout

Groups appear as nested tree nodes. Select a group's individual child controls
through the Element menu to move free-layout contents together.
Group frames and controls governed by automatic layout rules are protected against
inappropriate geometry dragging and alignment.

For StackLayout groups, choose **Edit group layout** to set direction, spacing and
whether hidden entries collapse. One application is one undo step.
Child source positions remain unchanged; derived rectangles are used only for
rendering, hit testing and layout. Group margins and alignment are approximated.
Unknown conditions reserve space. Editor hiding does not change runtime layout.

## Text, images and properties

Double-click a supported text control, press F2 or use **Edit Text**.
The dialog shows the stored label and an approximate preview.
Ctrl+Enter applies multiline text; Escape cancels.
Bindings and numeric values that may be language IDs require explicit replacement approval.
Inherited text can be overridden locally without changing styles or references.

Use texture selection to browse PNG, JPG, BMP and GIF assets under Media.
Theme assets take precedence over base assets. Only the relative path is applied.
Transparency is shown on a checkerboard. Source image files are not locked by previews.

Fonts, font aliases and bold/italic styles are read from fonts.xml.
Font sizes follow MP1's 576-line baseline. Missing families fall back to Segoe UI.
Properties include position, dimensions, text, color, textures, alignment, text offsets,
image tint/alpha and navigation targets.

Property provenance identifies local, style and reference values.
Reset removes the local override and previews the inherited value that will take effect.
Bindings, ambiguous XML structures and locked controls remain protected.
Batch reset and editing are atomic undo operations.

## Language resources and preview states

Load a primary MediaPortal language file and optional fallback.
Fallback is used for missing IDs, not explicitly empty primary translations.
Prefixes can be enabled or disabled. Language snapshots are saved in editor projects.
Numeric IDs remain in skin XML. The language picker returns an ID rather than fixed text.
The dedicated German and English fixtures intentionally retain their languages.

Sample Data offers baseline, long-title, weather and news presets, custom values
and simulated plugin states. At most 128 custom values and 128 plugin names are stored.
These affect only preview/editor state, not exported XML or XML undo history.

Collection Preview provides list, small/large icon, filmstrip, album, playlist and
static approximate coverflow views. Facades display one matching subview with fallback.
Enter at most 50 items, one per line:
`Title | Additional text | Optional image relative to Media`.
Collection mode, selection and data are stored only in editor projects.

## Navigation and page auditing

In navigation mode, select a source, choose a direction and click a focusable target.
Arrow keys test supported remote-control paths in the editor.
Duplicate positive IDs and bound navigation targets are protected.

Press F8 or choose Audit to inspect the open page and loaded includes.
Findings cover duplicate IDs, navigation targets, missing images and language resources.
Select a finding to jump to its control and property.
Dynamic and unsupported values are informational, not proven runtime errors.
The audit does not change XML, selection metadata or undo history.

## Zoom, pan and input protection

The mouse wheel zooms around the pointer. Drag the middle mouse button to pan.
100% displays one skin pixel per canvas pixel; Fit restores the full view.
View state is saved in editor projects without changing skin XML.

Pending property edits are protected when changing selection, searching or opening dialogs.
Apply them before performing an operation that would replace the current selection.

## Saving and exporting

Original files in the opened skin directory are not overwritten.
Changes initially exist only in the current editor session.
Save Project stores the session for later work in a .mp1skinproj archive.
Export Changes as ZIP produces only modified XML files at their relative paths,
plus export instructions; unchanged images and XML files are not copied.

Apply exports to a separate complete skin copy and test that copy in MediaPortal.
Exporting retains the editor's cumulative changes against the loaded originals.

Project format 4 stores collection settings. Formats 1, 2 and 3 remain readable.
Structural mappings preserve element identities and markers through undo/redo.
Projects validate original file hashes, snapshot integrity, paths and size limits.
External changes to originals prevent silent restoration.

The editor patches targeted source spans instead of reserializing whole XML files.
Unknown entries, comments, attributes, animations and conditions remain intact unless edited.
Existing encoding and BOM are preserved. Unrepresentable characters are rejected.
DTDs, external entities and UTF-32 are unsupported.

## Preview limitations and runtime comparison

The preview uses GDI+ and is not the MP1 Direct3D renderer.
Font metrics, 3D perspective and time-dependent behavior are approximate.
Masks, animations, unsupported layouts, expressions and plugin-specific behavior remain incomplete.
Collections use sample data; videos use a placeholder; missing images use substitutes.

Supported conditions include selected forms of window/control visibility, focus,
plugin.isenabled and string.equals. Unknown conditions are dotted by default.
They can be hidden independently of **Show hidden** without changing XML conditions.
Includes with unknown conditions may still load for editing.

There is no animation editor, complete plugin adapter or full skin creation workflow.
Global styles are displayed; ordinary editing creates local overrides.

Runtime-Comparison contains the prepared Titan settings spacing probe.
A full client/image and remote-control comparison remains pending.

## Validation and development

Build uses the Windows .NET Framework C# compiler with no NuGet dependencies.
Run the application with:
- --selftest OUTPUT-DIRECTORY for core checks.
- --ui-smoketest OUTPUT-DIRECTORY for UI checks.
- --check-skin SKIN-DIRECTORY OUTPUT-DIRECTORY for batch compatibility checks.
- --prepare-runtime SKIN-DIRECTORY OUTPUT-DIRECTORY for the runtime probe.
- --generate-demo-assets to regenerate editor-authored demo PNGs.

These are GUI executables and do not print normal console output.
Use Start-Process -Wait and inspect result files and the process exit code.
Historical version 0.9 reports record 189 core checks, 185 UI checks, 602 Titan variants,
586 edit/undo probes, ten project/export probes and ten direct native layout checks.
Historical results and newly performed tests are documented separately.

Create a new visible test instance only after changes that warrant user inspection.

## Version history

- 0.2: multiple selection, insertion, duplication, alignment and navigation editing.
- 0.3: editor projects for saving and restoring sessions.
- 0.4: texture browsing, view controls, element protection and page auditing.
- 0.5: local-property reset, sample data and saved plugin states.
- 0.5.1: corrected menu disposal and added actual menu-lifecycle tests.
- 0.6: layer ordering, removal and stable structural identity mappings.
- 0.6.1: element search, overlap selection and pending-input protection.
- 0.6.2: nested groups, child selection and automatic-layout edit protection.
- 0.6.3: direct text editing through double-click, F2 and the Element menu.
- 0.7: language resources, fallback, ID selection and define/include improvements.
- 0.8: static StackLayout rendering and an atomic layout editor.
- 0.9: collection/facade previews, font scaling, text rendering and native layout probes.
- English repository migration: English paths, documentation, UI and validation descriptions;
  intentional multilingual fixtures and original external identifiers are preserved.

## Sample provenance and license

common.time.xml, common.default.background.xml, fonts.xml and references.xml
originate from MediaPortal/MediaPortal-1, mediaportal/MediaPortal.Base/skin/Titan,
master, retrieved October 17, 2026. Line endings may be normalized.
https://github.com/MediaPortal/MediaPortal-1/tree/master/mediaportal/MediaPortal.Base/skin/Titan

Original Git blob SHAs:
- common.time.xml: 5e2378ddbb0b73981e676028b77b73c8306a52b9
- common.default.background.xml: 086796cee5511aa2010dfcf93ffb87009fc0bc05
- fonts.xml: 7bd804d512c19f02414d6d034bdb6be696c0a4d7
- references.xml: c4892f80845ed5c4a4e1a9c8d00e2093aa3e7ad6

demo*.xml and the seven PNGs under Media/Studio are editor-authored examples,
not original Titan screens or graphics. Original Titan fonts are not bundled.
MediaPortal sample files: copyright Team MediaPortal; see LICENSE.
New editor source is provided under GPL-3.0-or-later.
This is not an official Team MediaPortal product.
