# Start and finish a skin edit

For native WYSIWYG editing in an already running MediaPortal 1.39 window, see
[Live preview](LIVE_PREVIEW.md). Install the included process plugin once, then
enable Live beside Toolbox and Connect to the same skin, theme and XML page.

Extract the entire review ZIP into its own folder and run MP1SkinStudio.exe.
Windows and .NET Framework 4.x are required. A compiler and an installed
MediaPortal skin are unnecessary for the bundled demo.

## A complete first workflow

To keep a complete usable output while continuing to edit, use **Save skin …**
beside Live. Create a new output copy once, then Update saved copy on later
saves. Review XML, conflict checks and backups protect that copy; current source,
Undo and live preview continue. See [saving skin copies](SAVE_SKIN_COPY.md).

To start a new skin from an installed one, select its SKIN entry and choose
**Copy skin …** beside the heading. Choose a new name and destination folder,
then Create copy. Leave Open enabled to continue in the independent copy.
Applied editor XML can be included; pending fields must first be applied or
reverted. The source skin stays untouched and existing folders are protected.

1. Keep demo.xml selected. Select Movies in the tree or preview.
2. Change Text / data binding to My movies and click Apply Properties.
3. Undo restores Movies; Redo restores My movies.
4. Save Project to a new .mp1skinproj file. Open Project restores the edit,
   selected page, preview samples and undo history.
5. Use Ctrl+Shift+E (Element > Review XML changes) to compare original/edited
   files and check for source conflicts, including shared includes. Close the
   review, then Export Changes as ZIP creates changed XML and application instructions.
   Saving a project or exporting does not overwrite the source skin.
6. Open the export and review every changed file before applying it to a
   separate complete skin copy. A shared include can affect several pages.

Apply Properties commits field input. Revert Inputs discards pending fields.
Pending input blocks save/export and page changes until applied or reverted.
An asterisk also records unsaved preview/page/focus state; it does not always
mean that skin XML changed. Export is enabled only for changed XML.

Closing or opening another session offers Save project, Discard changes and
Cancel when needed. Canceling Save As or a failed save keeps the session open.
Apply/Revert pending fields first; the close dialog disables Save while fields
are pending. Enter/Escape safely cancel the decision.

Use **SKIN** above THEME to select a detected installed skin or the bundled demo.
The list refreshes when opened. Open skin directory remains available for another
location. Skin changes retain pending-input and save/discard/cancel protections.

The wider left sidebar has matching dark scrollbars. The PAGES / XML list is integrated into the left sidebar. Use its filename
filter and mouse wheel to browse; scrolling does not switch the active page.
Click a file to open it. Drag the divider below the list to resize it.

Use View above the canvas: Edit layout gives short example captions; Isolate
selection helps edit overlapping elements; All layers reveals raw variants.
On recognized MediaTipOverlay.xml, Example selects Music/Video/Pictures/Folder/
Radio. These are preview-only examples; property bindings and exports stay intact.

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

## Keep a list of findings

Audit checks the open page; Ctrl+F8 explains unverified preview content. Search
the findings, choose whether to include informational rows and use Save shown
report to save text or HTML with source locations and details. Rerun the check
after edits: saved reports are snapshots. Zero filtered findings is not a
complete runtime acceptance.

## Match an export to the runtime copy

_SkinStudio-Instructions.txt in an export lists original and exported SHA-256
hashes for each changed XML file. Match Original SHA256 to the target copy
before applying; match Exported SHA256 afterward. Export stops if a modified
source changed on disk after loading. Save your editor project for recovery
and compare the source with your backup; the editor does not merge that conflict.

## Resume after a source conflict

Save your pending work as an editor project, then use **Open Project**. If a
modified original no longer matches, the editor names the conflicting XML and
offers a folder chooser. Select a complete matching backup skin root (not its
Themes folder). The old directory may remain in place. Missing saved roots use
the same chooser. Cancel keeps your current session; an unsuitable backup is
rejected without replacing it. After recovery, save the project to remember the
new root, then export to a separate runtime test copy.

Only originals of modified XML are hash-matched. Unmodified dependencies, images
and runtime behavior still require a complete appropriate backup and review.

## Start directly with a saved project

Pass the saved file as the editor's argument, including quotes for spaces:

```powershell
.\MP1SkinStudio.exe "C:\Skin Work\My-Skin.mp1skinproj"
```

The editor first opens its demo, then loads the requested project. Missing source
folders and modified-source conflicts use the same matching-backup chooser as
Open Project. Cancel or a load error leaves the demo available for another try.
No Windows file association is installed by the review ZIP.

## Current practical review

The current executable passed a fresh edit/save/reopen/export/client comparison
on Titan's Default Home/Video: list width 590, a shared header marker and working
Left/Right menu navigation. The original Titan skin was restored and all 2,050
original file hashes stayed unchanged. This is a sample acceptance; inspect your
own actual exported pages/themes in the client. See RELEASE_READINESS.md for scope.
In Titan, H switches BasicHome/standard Home; standard Home exposes Settings.
Settings > GUI > Skin selects the test copy and restores Titan afterward.
