# MP1 Skin Studio user guide

Version 0.9 · October 18, 2026

## Getting started

1. Build the application with `powershell -NoProfile -File .\build.ps1`.
2. Start `MP1SkinStudio.exe`. The bundled demo loads automatically.
3. Select an element in the preview or element tree.
4. Drag to move it; use the lower-right handle to resize it.
5. Edit properties on the right and choose **Apply Properties**.
6. Save an editor project to continue later, or choose **Export Changes as ZIP**.
7. Use **Revert Inputs** to discard unapplied property fields without undoing applied XML changes. Pending fields block save/export and page/theme changes until applied or reverted.

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

GridLayout groups are also previewed with equal cells, row or column filling,
separate spacing, margins and child alignment. Open `demo.grid.xml` for an example.
Use four layout parameters for spacing, for example `GridLayout(2,0,30,25)`;
a fifth parameter selects `Horizontal` or `Vertical`. A positive row count
overrides the column count, matching MP1. Calculated positions remain preview-only.
Choose **Edit grid layout** from the group menu to set columns, rows, horizontal
and vertical spacing, and fill direction. At least one row or column is required.
Applying creates one undo step and preserves child source positions. Existing
bindings or other layout types require the dialog's explicit replacement option;
locked groups and ambiguous layout properties remain protected.

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

## Creating groups

Select at least two consecutive controls in the current XML page, then choose
**Group selected controls** from the Element menu. Controls must share the same
XML parent. Existing groups can be included, or a new group can be created inside
an ordinary group. The new group is selected so its layout can be edited
immediately.

Child XML, IDs, positions, ordering and comments between the selected controls
are preserved. Undo restores the exact source and original multiple selection;
redo selects the new group again. Saving a project preserves the structure and
editor markers. Original skin files are changed only through your separate export
workflow.

Grouping is unavailable across skipped controls, imports, different parents or
files, inside automatic layouts, or when any affected control is locked. Open an
included file as its own page to group its contents. Skins with behavioral group
defaults in references.xml are rejected rather than silently applying visibility,
animation or other inherited behavior to the new wrapper. Validate exports in MP1;
the editor preview is not a runtime comparison.

## Ungrouping

Select one neutral group, then choose **Ungroup selected group** from the Element
menu. Its direct children move up one XML level and are selected together. Child
IDs, positions, complete XML fragments and nested groups stay intact. Comments
and processing instructions inside the wrapper are retained. Undo restores the
exact wrapper and its selection; redo selects the promoted children again.

This first implementation supports neutral wrappers, including groups created
with **Group selected controls**. A wrapper must have absent or zero position and
size, no active or bound layout, no style/condition/animation/extension behavior,
and a concrete nonnegative ID if one is present. Show editor-hidden groups first.
Locked children, automatic parent layouts and indirect edits to included sources
are protected. Open an included source as its own page to ungroup there.

Known references to the removed group ID block the operation. Checks include
loaded-page navigation/conditions and literal references on other root/theme XML
pages. External dynamic references and plugin code cannot be fully checked;
review those uses before applying an export.

Project files save the resulting snapshot and identities. In-session undo keeps
each edit; after reopening a project, undo returns to the original source-file
baseline rather than replaying the entire previous editing history.

## Version history

- Ungrouping: neutral wrappers can be removed with preserved child XML, markers,
  comments and in-session selection undo/redo. Behavioral groups remain protected.

- Text presentation correction: imported header/clock text uses a deferred font
  pass with preserved clipping. Font scaling includes the MP1 point-to-pixel
  conversion; canvas zoom still applies afterward. Unavailable font families
  continue to use a substitute.

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

## Current property editing

Select a concrete list, filmstrip or thumbnail subview in the element tree to
edit its compatible row, spacing and image dimensions. For multiple selection,
only collection fields supported by every selected element are offered.

**Visibility condition** edits the XML `visible` expression. Parent and include
conditions still apply. Unsupported runtime conditions remain unresolved in the
static preview and are preserved in saved projects and exports.

These recent interface paths are awaiting manual validation. Refer to STATUS.md
for the current automation restriction and the limits of verified behavior.

## Video folder preview

On a page with a list control, choose **Element > Video folder preview** to use one Videos folder, a folder image and explicitly empty movie metadata. The command selects list view and hides unverified content. It explicitly sets an unwatched sample (count and percentage 0) and idle scrolling; Titan may display its unwatched icon. Other unknown properties remain unresolved. Element > Show unverified content reveals unsupported conditions, unresolved labels and missing-image placeholders for inspection. Empty image textures stay empty. Existing unrelated sample values, plugin states and language settings are retained. Skin XML and undo history stay unchanged; save the editor project to keep the sample state. List focus and clean-preview state are saved in the editor project and restored when reopening. The preset uses Titan's DefaultFolderBig.png; use Collection Preview and Sample Data to change the image for other skins.

Known static Control.HasText and Control.HasThumb conditions are evaluated from preview data and local media. Missing runtime properties, ambiguous IDs and unsupported image paths remain unresolved. Nested L(...) expressions use loaded language snapshots with fallback and prefix selection. Missing language IDs retain the original expression. These checks do not reproduce MP1 runtime texture caches or font metrics.

Standalone label and fadelabel previews honor XML text anchors, including omitted dimensions for centered labels and right-aligned X positions. Designer selection rectangles can be larger than text. Font metrics, scrolling animation and autosizing remain approximate.
