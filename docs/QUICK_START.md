# Start and finish a skin edit

Extract the entire review ZIP into its own folder and run MP1SkinStudio.exe.
Windows and .NET Framework 4.x are required. A compiler and an installed
MediaPortal skin are unnecessary for the bundled demo.

## A complete first workflow

1. Keep demo.xml selected. Select Movies in the tree or preview.
2. Change Text / data binding to My movies and click Apply Properties.
3. Undo restores Movies; Redo restores My movies.
4. Save Project to a new .mp1skinproj file. Open Project restores the edit,
   selected page, preview samples and undo history.
5. Export Changes as ZIP creates changed XML and application instructions.
   Saving a project or exporting does not overwrite the source skin.
6. Open the export and review every changed file before applying it to a
   separate complete skin copy. A shared include can affect several pages.

Apply Properties commits field input. Revert Inputs discards pending fields.
Pending input blocks save/export and page changes until applied or reverted.
An asterisk also records unsaved preview/page/focus state; it does not always
mean that skin XML changed. Export is enabled only for changed XML.

## Choose a demo

Use PAGE / XML. Common XML files and demo.menu.xml are includes; choose one of
the full scenes below. For arrow tests, enable Navigation first.

| Scene | Try |
| --- | --- |
| demo.xml | Text/size edits, Undo/Redo, project save and XML export |
| demo.navigation.xml | Right, Down, Left, Up across nested grid/stack groups |
| demo.free.xml | The same route across ordinary groups |
| demo.grid.xml | Arrow movement across five grid buttons |
| demo.stack.xml | Up/Down through a linear stack |
| demo.checkmark.xml | Selected/unselected images; Up/Down focus |
| demo.filmstrip.xml | Left/Right sample selection and following image position |
| demo.collections.xml | Collection settings and sample item selection |
| demo.images.xml | Proportional fit, alignment, center crop and top crop |
| demo.groups.xml | Group selection and supported structural operations |
| demo.layers.xml | Layer order and overlap selection |
| demo.language.xml | Explicit language samples and fallback |

## Continue with an installed skin

Copy the complete skin to a separate working directory. Open skin directory
loads that copy. Choose a page and theme, inspect source provenance, then edit,
save a project and export. Keep backups of the runtime test copy. Select that
copy through MediaPortal's skin settings for a client comparison; restore the
original skin afterward. The editor never applies an export automatically.

The preview approximates layout. Navigation changes simulated focus or explicit
sample selection, without running plugins, actions or playback. Checkmarks do
not toggle on activation. Slider/spin value interaction and rendering remain
unsupported; guarded arrows explain why focus stays unchanged. Ctrl+F8 lists
unverified preview details. See USER_GUIDE.md for precise support and bounds.
