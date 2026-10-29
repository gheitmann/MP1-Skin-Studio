# Live coverage audit and extended adapters

October 29, 2026. Official installed MP1 SDK 1.39.0.0; bridge 1.1.0.0.

Build-Probe.ps1 targets an isolated client directory containing Core.dll,
Utils.dll and Common.Utils.dll. CoverageProbe.exe takes the skin root and a
fresh report directory. It uses core factory type lookup and reflection-only
slot metadata: native controls are not constructed by this static audit.
The factory's custom game registry is not populated by process plugins here.

The DefaultWideHD baseline and final reports cover 449 pages, 19,490 expanded
controls and 114,474 explicit editable field occurrences. SDK mappings improve
from 108,736 to 111,528, an increase of 2,792. The same 488 original XML files
are hashed before/after the pass. See Before.txt, After.txt and Pages.tsv.
These are metadata counts, not runtime or visual compatibility percentages.

Run-Adapters.ps1 compiles/runs AdapterProbe against the official SDK and current
bridge. Its 64 checks cover scaled geometry, independent list row/icon sizes,
color/binding/font-cache restoration, safe item-data retention, group alignment,
metadata/slot agreement, complete collection frames and atomic failure/reset.
No rendering resources are initialized by those tests. See Adapter-Results.txt.

The baseline NativeProbe passes 13 SDK session checks. StudioProbe with persist
passes 13 real-pipe/owned-native-host checks, including live saving and Undo.
All 471 editor core checks pass. The isolated complete MP 1.39 renderer passes
16 additional specialized checks; see Real-MP-Results.txt. Changed and restored
text/list/filmstrip scenes were visually inspected. Six list rows and four
filmstrip items survive both live layout rebuilding and restoration. Captured
original properties restore exactly after reset and disconnect. The fixture
does not establish compatibility for every skin page or plugin control.

Collection resource rebuilds are coalesced at frame completion. Native list
AllocResources/Calculate paths retain list items; calling collection Dispose
would discard them. Inactive/unallocated test controls skip resource rebuilds.
The primary upstream GUITextControl, GUITextScrollUpControl, GUIListControl,
GUIFilmstripControl, GUIThumbnailPanel and GUIGroup sources were checked against
installed SDK metadata before adapting protected state.

No installed configuration, private paths, skin XML/assets, captures or runtime
logs are included as repository evidence. Results contain counts and filenames.

## Reproducing the specialized full-renderer test

Prepare-Scene.ps1 accepts ClientDirectory and SkinDirectory, both restricted to
an isolated copy beneath this repository's work directory. Close that copied
MP client before preparing it. The script copies the current bridge, compiles
the test-only scene plugin and SceneProbe, and adds Scene.xml to the copied skin.
It does not install the fixture into the owner's MediaPortal installation.

Set MP1_SKIN_STUDIO_LIVE_DIR to a private IPC directory for both the copied
MediaPortal process and SceneProbe. Start the copied client, then run
SceneProbe.exe with the IPC directory and a fresh result directory as arguments.
The probe opens window 90110 with six synthetic list rows and four filmstrip
items, updates text/colors and collection dimensions, and holds the changed
scene for 35 seconds for visual inspection. It then checks empty-frame and
disconnect restoration. Results.txt records each check.

Desired geometry accepts one skin pixel of rounding after MP's scaled-window
pixel conversion. Restoring captured original values still requires exact
equality. Collection item counts must survive layout rebuilding and restoration.
The fixture is test infrastructure, not a distributable plugin or user skin.
