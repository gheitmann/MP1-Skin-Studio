# MP1 Skin Studio user guide

Version 0.9 · October 20, 2026

## Getting started

1. Extract the complete review ZIP. For a source checkout, build with `powershell -NoProfile -File .\build.ps1` instead.
2. Start `MP1SkinStudio.exe`. The bundled demo loads automatically.
3. Select an element in the preview or element tree.
4. Drag to move it; use the lower-right handle to resize it.
5. Edit properties on the right and choose **Apply Properties**.
6. Save an editor project to continue later, or choose **Export Changes as ZIP**.
7. Use **Revert Inputs** to discard unapplied property fields without undoing applied XML changes. Pending fields block save/export and page/theme changes until applied or reverted.

See [Quick start](QUICK_START.md) for a complete first workflow, all twelve demo
scenes and the transition from an editor project to a runtime test copy.

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
original and its unknown XML. Select a top-level group and choose Duplicate to copy
up to 512 self-contained controls, including ordinary nested groups. Copied controls
receive new IDs; internal directional/default-control links and literal
Control.HasFocus/IsVisible/HasText/HasThumb visibility references follow the copies.
Inherited style references become local clone overrides. External targets and shared
styles remain unchanged. Only the outer group is offset; child positions remain intact.
Groups with external includes, view templates, locked descendants, ambiguous IDs or
dynamic navigation are rejected. Scripts and other dynamic references require manual
review. Insert/Duplicate Undo restores the prior selection; Redo selects the new item.
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
Arrow keys browse the focused collection's sample items first: up/down in lists,
left/right in filmstrip or coverflow, and row/column movement in icon grids.
At a collection boundary, arrows test supported control links. Selection and
thumbnail metadata are saved with the editor project; XML and undo history stay unchanged.
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

## Playback preview

Element > Playback preview > Stopped sets five explicit player sample flags to false. Unverified removes those overrides. The Video folder preset includes stopped playback. Missing flags stay unknown; the real player is never controlled. Sample Data exposes #preview.player.hasmedia, #preview.player.hasaudio, #preview.player.hasvideo, #preview.player.playing and #preview.player.paused. Set true or false to inspect individual conditions; independent flags can describe inconsistent scenarios. Apply or revert pending property inputs first. Save the editor project to retain the samples; exported XML remains unchanged. Other player conditions remain unsupported.

Button focus textures and text colors follow the simulated MP1 focus ID. Selecting a non-focusable element changes the editor selection without clearing that focus. Turn off Focus to inspect unfocused appearance. BasicHome runtime menu content still requires appropriate sample data and is not fully reproduced.

## BasicHome sample

On BasicHome.xml (window 35), choose Element > BasicHome preview. This explicitly selects FanartHandler background mode, disables the Titan update indicator, sets stopped playback, hides unverified content and restores the default focus. Save the editor project to retain it. The Titan Default and Christmas samples show six menu tiles with labels; runtime fanart, latest media, notifications and submenu animation remain approximate or unverified. Other pages reject this preset without changing state.

Supported boolean #(...) expressions can drive visibility/include conditions. Missing values and unsupported functions remain unverified. A static control.isvisible lookup with no target returns false; duplicate targets and cycles remain unverified. Large button textYOff values use the remaining label height rather than subtracting the offset twice.

## Preview navigation checks

Enable Navigation and use arrow keys on the canvas to browse supported collection samples, then follow explicit skin directions at boundaries or on other controls. The active collection template supplies facade directions. A target must resolve uniquely and be visibly supported after receiving focus; unavailable routes leave focus unchanged and display a status notice. Pending fields must be applied or reverted first. Successful focus changes are saved with the editor project. Automatic navigation is limited to the linear StackLayout case below; full geometric search and native hidden-target skipping remain unsupported. No neighbor links are written to XML.

For direct button children of a linear StackLayout, preview navigation also handles absent or zero mappings along the stack axis. Known hidden candidates are skipped, unknown visibility stops navigation, and unlinked boundaries retain focus. Explicit mappings always take precedence. This covers Titan Settings menus. Supported nested stack/grid hierarchies use the bounded geometric navigation described below; ordinary groups and standalone buttons use the bounded geometric search described below; full runtime search remains unsupported. Derived neighbors are never written into XML.

The editor project remembers preview focus independently for each page and theme (up to 128 combinations). Returning to an existing saved focus does not itself create an unsaved change. New focus choices are retained when you save the project. Older single-focus projects remain readable and migrate automatically. Missing or ambiguous saved targets use the page default.

Changing the active page or theme is a project change: save to retain the last selected view when reopening. Returning to the previous view removes that change if nothing else changed. View selection does not modify skin XML or the export contents.



## Collection interaction and clean previews

Enable Navigation to browse sample rows/cards with the arrow keys. Grid movement uses the concrete view's item width and available columns; horizontal grid boundaries do not wrap into another row. Empty collections or hidden/unverified collections do not consume a sample movement. At a boundary, normal supported control-link testing applies. This is bounded preview behavior, not complete native MP1 navigation.

Collection Preview keeps the selected sample visible in narrow coverflow/filmstrip views. After applying a facade view, its concrete control is selected for property editing and keyboard focus returns to the canvas when Navigation is enabled. Opening a saved project likewise selects the active concrete view. Collection mode/data/selection remain shared editor settings; they are not separate per-page datasets.

The selected item's image feeds #selectedthumb along with the title/detail/index/count values. Explicit Sample Data values override derived data; clear such an override to follow the selected row. The Video folder preset uses its sample row image. On pages without collections, derived values return to baseline or explicit samples instead of leaking from the preceding page.

Focused row/card textures and selected colors follow simulated focus. Leaving the collection retains its sample selection while removing focused appearance. Clean preview hides missing collection image/frame placeholders and the Empty collection notice. Explicitly empty images always remain empty. Show unverified content restores diagnostic placeholders. Coverflow still approximates a static arrangement; masks, perspective and animation remain unsupported.


## Explain unverified preview states

Press Ctrl+F8 or choose Element > Inspect unverified preview to inspect the current page and loaded includes. Search the informational findings and use Go to element to inspect the source property. Missing samples, unsupported functions, ambiguous IDs, dependency cycles, absent language entries and runtime/cache artwork remain explicitly unverified. A spelling suggestion never changes the original expression. Pending property drafts must be applied or reverted before jumping; opening and searching the report is read-only. The report is not proof of a runtime error. Very deep, long or expensive expressions remain unknown; output and dependency traversal are bounded.

## Music, Pictures and idle scenarios

Element > Music folder preview and Pictures folder preview provide matching folder items and images on pages with a list control. All three media folder presets explicitly close the hidden menu, set #hideinfo to False, stop playback and select zero idle seconds; unrelated samples and plugin states remain intact. These choices are saved only in the editor project.

Element > Idle-time preview selects Active input (0 seconds), Idle (60 seconds), or Unverified. Sample Data exposes #preview.system.idleseconds and accepts whole seconds from 0 to 86400. Supported system.idletime expressions require a positive integer threshold, capped at 1000 seconds as in MP1. Missing samples or invalid thresholds remain unknown. Other conditions still apply when idle changes. This preview never changes actual Windows activity. Exported skin XML remains unchanged.

## Sample artwork

Choose Element > Sample Artwork to search image texture expressions on the open page and existing saved mappings. Selecting an image control first preselects its expression. Choose skin image opens the existing Media browser, Empty artwork explicitly supplies no image, and Use original expression removes the sample. Apply commits the dialog draft; Cancel leaves the project unchanged. Apply or revert pending property fields before opening the dialog.

Identical trimmed expressions share the same sample across pages and themes; matching is exact and case sensitive. This is useful for repeated runtime CD-Art/ClearArt bindings. It affects image controls and Control.HasThumb, with the current theme overriding base Media images. General text variables, button textures and collection item images keep their separate settings. Duplicate IDs still make condition lookups ambiguous. Removing a sample can restore an unknown runtime condition.

Save the editor project to retain samples. XML bindings, XML Undo/Redo and exported XML remain unchanged; sample images stay in the skin directory and are not embedded. Sample Data preserves mappings during ordinary variable edits; selecting one of its replacement presets resets the artwork scenario too. The media folder presets preserve existing artwork samples.

Ctrl+F8 also lists unavailable or unverified visible artwork, including missing/corrupt chosen samples. Explicit empty and known-hidden images are omitted. At most 128 mappings are supported; choose ordinary PNG, JPG, BMP or GIF files below Media. Absolute paths, parent paths and image-path bindings cannot be used as sample replacements. Images above 32 MB, 8192 pixels on either axis or 32 million pixels stay unverified. This does not simulate native image caches, masks or animation.

## Filmstrip focus and property-driven images

The supported static filmstrip facade (ID 50) derives #facadeview.focus.X, Y, Width and Height from the selected preview thumbnail. Arrow navigation updates them with the selected item. Coordinates are recomputed after reopening. They remain unknown for unsupported/ambiguous/hidden facades, other views, automatic or property-driven facade geometry, or when focus leaves the facade. Sample Data can explicitly override any coordinate; clear the override to resume derivation. Unset rows can therefore use derived values rather than being missing data.

Image posX and posY attributes named property can use these values and the supported integer cint/add/sub expressions. Unknown, negative or out-of-range results retain the fixed XML fallback and appear as informational geometry findings under Ctrl+F8. Coordinates are limited to 0 through 100000. Native animation, scrolling effects and the last-valid-position cache remain approximate or unsupported.

The property pane labels these fixed positions as protected XML fallbacks and makes them read-only. Their bindings remain protected during movement, alignment, copying and direct edits; inspect positions by changing preview samples. Other editable properties, such as image width, still retain their ordinary undo/export workflow.

Element > Video PIN preview > Locked / Unlocked chooses an explicit #MyVideos.PinLocked sample. Unverified removes it and keeps missing runtime state unknown. This never changes actual PINs, configuration or media access. Save the editor project to retain the sample. Exported XML preserves original position attributes and contains no PIN sample data.

## Try the self-contained filmstrip demo

Open the bundled demo skin and choose demo.filmstrip.xml from PAGE / XML. Enable Navigation and use Left/Right: the upper movie image follows the selected thumbnail. Select that upper image to inspect its read-only property-driven Position X. Collection changes sample titles and images. Save Project retains the selected sample; changing an ordinary property such as image width can be undone and exported while preserving the position attribute. This example uses bundled Studio images and requires no installed Titan skin, plugins or runtime artwork.

The default menu demo also uses explicit white text for both focused and unfocused buttons, with centered captions. Original Titan reference/theme resources remain unchanged.

## Fit, align and crop images

Choose demo.images.xml from PAGE / XML for six bundled comparisons. Select an image, then scroll the properties pane to Keep image proportions, Crop to fill and Crop from top. Use yes/no values or the picker, then Apply Properties. Save Project retains the edit; Undo/Redo and XML export use the normal editing workflow. The fields are available for image-only selections.

With proportions enabled, the whole image fits inside its rectangle using Horizontal image alignment and Vertical image alignment. Defaults are left/top unless inherited settings change them. Crop to fill keeps proportions while cutting off excess source content centrally. Crop from top keeps the top of the source and overrides centered cropping. With proportions disabled the image stretches and crop flags have no effect. These settings apply to image controls; collection thumbnails/frames keep their existing static fitting.

Legacy centered settings are applied in source order across references, styles and local values; later alignment tags can override individual axes. Unknown flags, definitions or alignment values appear as informational image-layout findings under Ctrl+F8. Native pixel-aspect corrections, image initialization from zero dimensions, masks, borders, fixed-height behavior and animation remain approximate or unsupported. The preview assumes square pixels.

## Automatic grid navigation

Open demo.grid.xml, enable Navigation and use the arrow keys. The active button uses a blue focus texture. Existing nonzero onleft/onright/onup/ondown mappings always win; an absent or zero mapping on a button inside a supported GridLayout hierarchy uses the current static layout rectangles. No navigation links are generated in XML. Save Project retains the simulated focus. Ordinary width/layout edits retain their normal save/export/Undo/Redo workflow.

Selection follows MediaPortal's rounded top-left bearing/distance filters and XML order, rather than treating every grid as a rectangular row/column table. Incomplete rows can therefore navigate diagonally; equal scores select the later eligible control. Eligible standalone buttons and buttons in supported StackLayout/GridLayout hierarchies also participate. ID 1 and its descendants are excluded as in native hierarchy flattening. Editor-hidden and known-hidden targets are skipped.

The bounded implementation requires unique positive IDs, known visibility and finite static geometry/alignment/margins. It permits nested automatic layout groups through 16 levels, and refuses unknown layout wrappers and ordinary groups beneath automatic ancestors, unsupported focusable types (including collection candidates), runtime enable/hidden-focus rules, unresolved inputs and pages over 2,048 logical controls. Coordinates are bounded to one million; the native 10,000-distance initial limit remains. Unknown routes produce a status explanation and preserve focus, XML and undo history. Static layout rounding can differ from live runtime measurements; animations and plugin-generated controls remain approximate. Existing linear stack fallback keeps its separate behavior.

## Navigate nested layout groups

Choose demo.navigation.xml, enable Navigation and try Right, Down, Left, Up. The four-button route crosses a nested grid and vertical stack inside a horizontal stack. Focus follows the selected button, and Save Project retains it. Automatic ancestors must use a supported static StackLayout or GridLayout. Ordinary wrappers outside automatic ancestors are supported with absolute child positions. A top-level stack containing nested groups also uses geometric navigation; simple direct linear stacks keep their existing along-axis fallback. Explicit nonzero mappings continue to win.

Measurement siblings and nested branches are checked before navigation, including nonfocusable controls whose size affects arrangement. Unknown sizes, unresolved layout inputs, ordinary wrappers beneath automatic ancestors or more than 16 levels stop the route without changing focus, XML or undo history. Hidden/collapsed ancestors and editor-hidden branches do not supply targets. Edit ordinary sizes or margins with the existing property tools; calculated child positions remain protected.

## Ordinary groups and standalone buttons

Open demo.free.xml, enable Navigation and try Right, Down, Left, Up. Buttons in ordinary groups and at page level now use the same bounded geometric fallback for absent/zero arrow mappings. Ordinary group positions do not translate child coordinates; use each child's absolute Position X/Y. Ordinary wrappers may contain nested ordinary groups or independently positioned stack/grid groups. Group selected controls, Ungroup and structural Undo/Redo retain the routes. Save Project retains focus; ordinary position/size edits follow the existing export and Undo/Redo workflow.

An ordinary wrapper beneath an automatic ancestor remains unsupported because its children cannot safely be assumed to follow that wrapper's calculated position. Dynamic layout attributes, unknown geometry/visibility, ambiguous IDs, unsupported focusable types and runtime enable rules remain guarded. Explicit nonzero mappings always win, even when invalid; a dash or missing target never silently becomes an automatic route. Native ID-1 subtree exclusion and existing limits remain.

## Checkmark controls

Choose demo.checkmark.xml, enable Navigation and use Up/Down. Native checkmark controls now participate in bounded geometric navigation with buttons, including mixed linear stacks. Navigation changes focus only: it never activates the control, toggles its value or executes onclick. Explicit nonzero mappings retain precedence; unsupported focusable types and existing geometry/visibility/ID bounds remain guarded.

Select a checkmark and scroll its property pane to Selected expression, Checkmark width/height, Selected/Unselected checkmark image and Unfocused text color. Apply Properties, Reset, Undo/Redo, Save Project and Export Changes as ZIP use their normal workflows. A static selected expression accepts true or false; runtime expressions can use explicit Sample Data. Empty selected defaults to false. Values such as yes or unresolved samples remain unverified. Selecting a control does not change its selected expression.

The selected flag chooses textureCheckmark or textureCheckmarkNoFocus independently of focus. markWidth/markHeight use static integers from 0 through 8192. Zero dimensions draw no mark. Dimension-name casing is resolved through references/styles/local XML; edits preserve existing tag spelling. Duplicate local dimension aliases reject editing/reset. Alignment right places the label after the mark; left places the mark after measured text. Fonts, text clipping, shadows, runtime sizing and animations remain approximate. Labels are bounded to 8192 characters.

Unknown selection displays no guessed mark, while known label text can remain visible. Ctrl+F8 explains unresolved selection or layout as informational PREVIEW_CHECKMARK findings. Missing local textures are omitted. Runtime enable states, selection activation and plugin callbacks are not simulated. This checkmark support does not implement slider or spin-control value interaction.

## Opening a clean editor session

Opening a skin directory or editor project establishes its saved-state baseline after the initial control selection. Pages without an explicit default control can therefore use the editor's initial fallback focus without immediately showing an unsaved asterisk. Changing focus, zoom, preview samples or XML afterward retains its normal save detection. Returning zoom/pan to Fit restores the original clean state when nothing else changed. Saving and reopening retain the selected focus.

The review ZIP is intended to be extracted completely into a separate folder. Keep its Samples, docs and executable together. Its README identifies the source revision and MANIFEST.sha256 records every payload hash. The package includes twelve complete demo pages plus their includes and Studio assets, and requires no installed Titan skin to open those scenes. Deliberate unknown checkmark state and optional language/runtime samples remain explicitly unverified. Package acceptance covers editor workflows; installed-client rendering remains a separate check.

## Slider and spin-control arrow limits

The navigation-link editor and audit show XML mappings. Arrow preview additionally checks whether a control consumes the direction before ordinary navigation. Slider Left/Right adjusts a runtime value in MediaPortal, so this editor leaves focus unchanged with an explanation. It does not change slider values or execute callbacks. Up/Down explicit mappings still use normal target visibility and ID validation.

For spincontrol, horizontal orientation (default or 0) makes Left/Right depend on its runtime inner-button/value state; vertical orientation (or 1) makes Up/Down depend on that state. Those axes remain unverified. Unknown or property-driven orientation blocks all arrows. Explicit mappings across the other axis can still work. This is a conservative boundary, not a claim that the native control always consumes an axis in every internal state.

Ctrl+F8 reports visible slider/spin controls as informational PREVIEW_INTERACTION findings because their rendering and runtime value interaction are not implemented. Raw XML mappings, editing, saved projects, Undo/Redo and export remain available. Guarded arrows preserve focus, editor signature and XML. Checkmark selection/layout findings now have a Checkmark-specific heading. Other existing control/layout guards remain in effect.
