# Explicit artwork preview checkpoint

October 20, 2026. Fresh validation: 311 core checks passed, zero failures; eight real Titan artwork workflows passed across Video/Music, list/filmstrip and Default/Christmas. All 23 distinct loaded original XML hashes stayed unchanged. Four list canvas renders were generated; Default Music was visually inspected. Windows acceptance covers the dialog menu, search, explicit empty selection, Apply and save.

## Behavior

Element > Sample Artwork lets the owner map an image control's original texture expression to a chosen local skin Media image, explicit empty artwork, or the unchanged original expression. Search filters expressions and preserves drafts; the selected image expression is preselected when available. Identical trimmed source expressions share a sample globally across pages and themes; matching is exact and case sensitive. Samples do not alter general text variables, button textures, collections, XML, or XML undo history. Collection artwork remains configured separately in Collection Preview.

Rendering and Control.HasThumb use the same effective image expression. Local samples use normal theme/base Media precedence. Empty means explicitly absent; removing the sample restores original runtime uncertainty. Duplicate control IDs stay ambiguous even when individual images are sampled. Ctrl+F8 now also explains unavailable/unverified visible image controls and missing/corrupt samples; hidden and explicitly empty images do not produce artwork findings. All explanations remain informational.

Mappings are copied and signed with preview settings, saved in editor projects, and excluded from skin ZIP exports. Sample Data retains mappings when editing variables; choosing its replacement presets resets them with the rest of the preview scenario. Existing media folder presets preserve artwork samples. Old projects with no artwork entries remain readable. Malformed encodings, duplicate entries, excessive counts and invalid paths are rejected before replacing the last saved archive.

At most 128 mappings are saved; source expressions are bounded to 2048 characters and sample paths to 512. Samples accept PNG/JPG/JPEG/BMP/GIF paths below Media or the empty string, with no absolute/parent paths, bindings or path wildcards. Selected images remain in the skin; they are not embedded in project/export files. Rendering now applies the same 32 MB, 8192-axis and 32-million-pixel limits used by the thumbnail availability check. Missing, corrupt or oversized images stay unavailable/unverified.

## Evidence and limits

Core-Results.txt records 311 checks, including exact-expression scope, duplicate-ID uncertainty, copy/preset retention, cancellation/search drafts, legacy and invalid archives, theme resolution, real canvas painting and explicit empty suppression. Probe.cs/Results.txt exercise real runtime/cache expressions with empty and selected sample states, save/reopen/report consistency, restored XML Undo/Redo and exact export. The sample folder graphic is a test fixture, not a claim to reproduce actual CD-Art or ClearArt.

For the explicitly chosen empty/sample scenarios, unresolved visibility decreases Video list 1 to 0 and Music list 4 to 2 in both themes. Video filmstrip decreases 5 to 3 and Music filmstrip 2 to 1. Remaining Music conditions involve duplicate runtime IDs; Video filmstrip still needs facade focus coordinates and PIN state. Artwork samples never resolve these by guessing. Full masks, animation, caches, plugin behavior and native rendering remain unsupported or approximate.

The final button docking correction was compiled and checked through native Windows Apply/save after the successful model suites; those unchanged suites were not repeated. Prior 22-page preview and installed-client results are historical. No new installed-client run occurred. Only probe code and textual reports are committed; installed XML, project/language snapshots, render images and desktop captures are excluded.
