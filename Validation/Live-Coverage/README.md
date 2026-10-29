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
All 471 editor core checks pass. New per-control visible MP renderer acceptance
is pending: the other active chat's Configuration Studio is left undisturbed.
Historical real-MP acceptance is not relabelled as a fresh visual comparison.

Collection resource rebuilds are coalesced at frame completion. Native list
AllocResources/Calculate paths retain list items; calling collection Dispose
would discard them. Inactive/unallocated test controls skip resource rebuilds.
The primary upstream GUITextControl, GUITextScrollUpControl, GUIListControl,
GUIFilmstripControl, GUIThumbnailPanel and GUIGroup sources were checked against
installed SDK metadata before adapting protected state.

No installed configuration, private paths, skin XML/assets, captures or runtime
logs are included as repository evidence. Results contain counts and filenames.
