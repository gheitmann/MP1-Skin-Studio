# MP1 Skin Studio v0.9.0-preview.2

October 21, 2026. Easier visual editing for Windows and MediaPortal 1 skin authors.

## New in preview.2

- Collapsible property categories with contextual help and preserved drafts.
- Searchable skin-font picker with editable sample text, effective pixel size,
  family/style and unavailable-font substitution details.
- Integrated Toolbox with draggable text/button/image tools and filtered Media
  thumbnails. One active Media image can also be dropped from Windows Explorer.
  Insertions target the current page XML and undo as one action.
- Grid/Snap settings and spacing presets, saved in the editor project. Visible
  grid density adapts to zoom while snapping retains the exact distance.
- Separate position and size locks, inherited through groups and preserved
  through structural undo/redo and project save/reopen. Whole-element locks remain.
- Responsive toolbox/sidebar layout retains usable preview space at minimum size.

## Install and compatibility

Extract MP1-Skin-Studio-v0.9.0-preview.2-win.zip completely and run
MP1SkinStudio.exe. Windows and .NET Framework 4.x are required. SHA256SUMS.txt
verifies the ZIP; MANIFEST.sha256 inside verifies the payload. Twelve demo scenes
are included. Use an independent skin copy for editing.

Editor projects now use format 5. Existing formats 1–4 remain readable; older
editors cannot read newly saved format-5 projects. XML skin exports retain their
existing format. External images must first be placed in the active skin Media
directory. Fonts are not installed and shared font definitions are not rewritten.
The preview remains approximate; runtime animations/full visual equivalence are
outside the verified scope. See USER_GUIDE.md and RELEASE_READINESS.md.

## Validation

Fresh integrated development acceptance passed 438 core checks, 229 broad UI
checks, twelve complete demo workflows and twelve clean-session workflows.
Final OLE success reporting and responsive toolbox layout then passed 17 focused
workflow cases and 25 selector/scroll/menu cases, with 488 installed XML hashes
unchanged. Full broad checks were not repeated after that localized UI polish.
See Validation/Workflow-Tools/README.md in the source for exact build scopes.
No attended OLE drag or new MediaPortal runtime comparison is claimed.

## Previous release: v0.9.0-preview.1

October 20, 2026. First public preview for Windows and MediaPortal 1 skin authors.

## Included

- Visual editing of positions, sizes, text and textures; selection, alignment,
  groups, layers and Undo/Redo.
- Editor project save/reopen and source-preserving XML export with change review.
- Automatic installed-skin discovery and Select different Skin Folder in SKIN.
- Complete skin copying with a new name/location, optional applied XML changes,
  background progress and cancellation, preserving the source skin.
- Approximate list/grid/filmstrip/coverflow previews, sample data and language
  resources; readable editing views and recognized MediaTip examples.
- Improved text selection/focus alignment, faster large-skin handling, a wider
  sidebar and coordinated dark scrollbars, dropdowns and native window caption.
- Twelve demo scenes, shared demo resources, guides and the GPLv3 license.

## Install

Download MP1-Skin-Studio-v0.9.0-preview.1-win.zip, extract the complete archive,
then run MP1SkinStudio.exe. Requires Windows and .NET Framework 4.x. No separate
installer or source build is needed. SHA256SUMS.txt verifies the ZIP download;
MANIFEST.sha256 inside the archive verifies each payload file.

## Preview scope and validation

The preview is an editing aid; runtime animations, dynamic plugin content and
full MediaPortal visual equivalence remain outside its verified scope. Use Copy
skin to create an independent base. Project saving and XML export do not install
changes into a MediaPortal skin automatically. See RELEASE_READINESS.md.

Latest focused checks: 25 selector/sidebar/menu/scroll and ten readable-preview
scenarios passed; 488 installed XML and 26 demo hashes were unchanged. Earlier
acceptance passed 430 core checks, eleven copy-dialog scenarios, twelve full
demo workflows and twelve clean-session workflows. The complete DefaultWideHD
copy matched all 3,399 files while preserving every original. These results
retain their individual source checkpoints; publication does not imply that
all historical checks were rerun. Release packaging verifies every payload hash.
