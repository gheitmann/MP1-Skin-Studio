# Attended Titan Video preview comparison

Date: October 19, 2026, Europe/Berlin. Baseline: 1dd8fcf0115dde99089d4d712dd8f7b1af29c1c4.

Created a complete isolated Titan copy. Fresh Complex-Page probe against the current Windows build passed all eleven stages: five facade modes, sample selection/scrolling, compatible width/text-offset/height/spacing edits, undo/redo, project save/reopen and export. All 17 loaded source hashes stayed unchanged. An initial probe launch lacked its assembly beside the executable; after supplying the existing built assembly, the rerun passed.

Attended editor observations:
- Open XML successfully loaded isolated myvideo.xml through the native file dialog: 211 controls.
- Initial View-As selection simulates menu focus and opens the hidden side menu. It is unsuitable for comparing the client's selected video list.
- Unchecking Show unverified conditions removed some overlapping elements but left unresolved property/image placeholders. Unknown-expression hiding alone does not establish runtime fidelity.
- Searching for listcontrol and selecting its concrete list template removed the side menu. Diagnostics then reported 82 hidden and 97 unresolved conditions.
- Loaded installed strings_de.xml through the language dialog (2,487 entries), disabled prefixes and applied. Numeric header 3 became Videos, matching the previously observed client header.
- Saved Titan-Video-Preview.mp1skinproj in local scratch. Independent ZIP inspection found project.xml and language/primary.xml only: files is empty, showUnresolved=false, prefixes=false and German language snapshot persisted. No XML edits were saved.
- Full SHA256 comparison of all 2,050 installed Titan files with the isolated copy found zero mismatches after the UI work.

Remaining differences are concrete: the client shows one Videos folder, whereas the editor uses six movie sample rows; plot, selected-thumb, current-module and movie/plugin bindings remain unresolved. The unknown thumbnail renders as a large placeholder rather than the client's known folder graphic. Date/time still use fixed samples. Preview focus and language must be aligned before judging geometry.

No new source changes, client skin installation or exported-runtime verification occurred. The generated probe export remains local and uninstalled. The editor remains on the saved comparison session. Original skins and language files were not modified. DirectX replacement remains deferred.

Next: provide a reproducible folder-browsing preview state and distinguish known-empty runtime metadata from unknown properties; then compare the isolated export in MP1. Do not claim visual equivalence from model tests or from hiding unknown conditions.
