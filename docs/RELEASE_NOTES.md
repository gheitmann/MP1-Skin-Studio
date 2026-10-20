# MP1 Skin Studio v0.9.0-preview.1

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
