# Image presentation checkpoint

October 20, 2026. Fresh validation: 332 core checks passed, zero failures; 14 real Titan edit/save/reopen/export/undo/redo workflows passed across BasicHome and Video/Music/Pictures list/filmstrip in Default/Christmas. All 98 observed visible local image plans stayed within their source and target bounds; zero unsupported image settings occurred in those candidates. All 55 distinct loaded original XML hashes remained unchanged. Native Windows image-field Apply/save/Undo/Redo passed.

## Behavior

Image controls now fit using their actual horizontal/vertical alignment, with native left/top defaults. keepaspectratio retains the full source within the rectangle; zoom crops the source centrally to fill it; zoomfromtop retains its top and takes precedence. Crop flags are ignored when proportions are disabled. Tints and transparency continue through the existing image attribute pipeline. Legacy centered behavior and explicit align/valign are processed in reference/style/local XML order.

The shared renderer keeps existing collection frame/item fitting behavior; these image-control changes do not introduce native collection zoom, perspective or animation. Pixel ratio is assumed to be one. Positive supplied preview bounds are used; native intrinsic zero-size initialization, fixed-height behavior, border/mask effects and clipping compensation are not reproduced in full. Tiny/extreme crops that would create zero source pixels are rejected.

Image-only property rows expose Keep image proportions, Crop to fill and Crop from top, with yes/no pickers. Existing align/valign rows receive image-specific labels. Edits use ordinary Apply, project persistence, source-span export and Undo/Redo. Unknown flags, missing definitions or unsupported alignment values remain informational PREVIEW_IMAGE_LAYOUT findings under Ctrl+F8 and use an approximate preview fit. No XML is rewritten by rendering or diagnostics.

## Primary source and evidence

Locally read upstream [GUIImage.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIImage.cs): declarations around 80–92, fitting/alignment/source cropping around 1255–1375, and legacy Centered setter around 2715–2735. The implemented subset follows its square-pixel static geometry and integer source crop rounding; it is not native-engine validation.

Core-Results.txt includes ten new focused tests: default alignment, nine anchor combinations, ordered/inherited centered rules, centered/top/landscape crop areas, stretch isolation, definitions/unknown diagnostics, invalid/extreme bounds, collection isolation, actual production crop pixels, and saved/exported/restored state. The initial inherited-rule test reused a cached reference source after modifying its fixture file; creating a new project for that fixture produced the final passing suite.

Probe.cs/Results.txt cover 14 real-page workflows and 98 bounded plans. Each scene edits a real image through fit, center crop and top crop; exact export, normal/restored Undo/Redo and saved state are checked. The bundled demo.images.xml adds six self-contained comparisons and has zero unverified findings. Its production form render was visually inspected. Windows-Results.txt records the native property-field workflow and saved archive inspection.

The final image-specific alignment label wording was compiled after the successful suite/matrix/native checks; unchanged logic was not retested for this form-only wording. Earlier filmstrip, artwork, package and client acceptance is historical. No new installed MP1 client run occurred. Only code and textual reports are committed; original skins, editor project snapshots, language snapshots, bitmap renders and desktop captures are excluded.
