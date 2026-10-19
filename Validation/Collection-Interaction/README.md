# Collection selection, geometry and attended editing workflows

October 19, 2026.

## Changes

- Navigation arrows browse the focused collection's explicit sample items before testing control neighbors. Lists/album/playlist use up/down, filmstrip/coverflow use left/right, and thumbnail grids use their actual small/large column count. At an axis boundary, existing control-link testing takes over. No sample wrapping or native geometric search is invented. Hidden, unknown, empty, unfocused and ambiguous collections do not change sample selection.
- Selected-row textures, selected text colors and image outlines now depend on simulated control focus, independently of designer selection. Standalone thumbnail and playlist controls are focusable.
- Narrow coverflow viewports retain the selected card. Filmstrips scroll with selection instead of jumping complete pages. List capacity does not demand a trailing gap after the final fitting row; secondary text uses its own vertical offset. Counts and nonfinite imported geometry are bounded.
- Clean previews omit missing collection frames/images and empty-collection notices; explicit empty images stay empty even in inspection mode. Inspection mode still exposes missing-image placeholders.
- #selectedthumb follows the selected item alongside #selecteditem, #selecteditem2, #selectedindex and #itemcount. Explicit Sample Data overrides retain precedence. The Video folder preset now derives its thumbnail from its sample row. Existing saved explicit thumbnail overrides remain explicit and can be cleared in Sample Data.
- Page preparation and page focus restoration refresh derived collection values after the page is loaded. Leaving a collection restores baseline/explicit values instead of retaining stale list metadata. Collection samples remain shared editor settings, not independent per-page datasets.
- Applying Collection Preview selects the active concrete facade view when a facade/subview was selected, and returns keyboard focus to the canvas in Navigation mode. Automatic selection on project/page restoration also chooses the active concrete view. This exposes the actual editable geometry; explicitly selecting the wrapper in the tree remains possible.

## Fresh validation

The integrated model/rendering build passed 286 core checks, zero failures, including fourteen new collection regression cases. The final two form-only changes (active-view property selection and keyboard focus restoration) were then compiled and verified through attended Windows interaction; the successful core suite and console matrix were not repeated for those form-only changes.

The included console probe passed 70 complete cases: five real Titan pages (Video, Video Playlist, Music Songs, Music Playlist, Pictures), two themes (Default/Christmas), seven requested modes (list, small/large icons, filmstrip, coverflow, album, playlist). Missing templates use the existing documented fallback; the report records the effective kind for each requested mode. Each case verifies sixteen visible selections, thumbnail metadata, forward/reverse sample navigation, unchanged preview-only XML/history, a concrete width edit, exact project restoration, export, in-memory undo/redo and restored-project undo/redo. All 33 distinct loaded source hashes remained unchanged.

## Attended Windows acceptance

1. Reopened a saved Video list with sixteen samples and Sample 6 selected. Navigation Down selected Sample 7, updated its thumbnail from audio to folder, retained control focus 50 and displayed the unsaved caption/status.
2. Left followed the explicit link to View-As (ID 2). The side menu became visible and the list lost its focused row texture while retaining Sample 7. Right returned to facade 50 and the same selection.
3. Collection Preview switched to Large icons without changing sample data. In the final build, Down moved Sample 7 to Sample 12 using the actual five-column grid; the selected item remained visible on the next grid page.
4. Switched to Coverflow through the Collection dialog. The property panel selected the actual coverflow control with width 1920/height 340, and canvas keyboard focus returned automatically. Right immediately selected Sample 13, with focus still 50.
5. Ctrl+S cleared the unsaved caption. Saved manifest: coverflow, zero-based selection 12, focus 50. The edited XML hash exactly matches the pre-navigation project; preview navigation and mode changes did not alter the width-edit snapshot.
6. Closed normally, rebuilt the final form selection change and reopened the saved project. Sample 13, coverflow and the concrete coverflow property panel were restored with a clean project caption.

Computer Use required fresh observations after a stale menu index, unavailable geometry and a detected external user-input/focus change. Menu keyboard navigation completed the selection; no stale action was replayed. No native permission prompt or user assistance was needed.

No new installed MediaPortal client run or export installation was performed. These are preview/model/editor checks, not a claim of complete runtime equivalence. Coverflow remains a static approximation; masks, animation, general dynamic/plugin values and full native remote-control behavior remain outside this milestone.
