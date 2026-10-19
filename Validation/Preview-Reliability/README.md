# Preview reliability checkpoint

October 20, 2026. Fresh final build: 300 core checks passed, zero failures. The real Titan probe passed 22 Default/Christmas workflows across BasicHome, Settings, Video, Music and Pictures; media pages cover list, filmstrip and coverflow. Two real idle transitions passed. All 57 distinct loaded original source hashes stayed unchanged.

## Changes

Explicit Video, Music and Pictures folder scenarios set matching sample items, stopped playback, closed hidden menus and zero idle seconds. Samples remain project data; unrelated properties and plugins are retained. Idle commands select zero, sixty or unverified seconds. Native positive system.idletime thresholds clamp to 1000 seconds; samples accept whole seconds from 0 to 86400. Missing or malformed inputs remain unknown. Semantics were checked against the locally downloaded primary [GUIInfoManager source](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIInfoManager.cs), translation around lines 786–810 and evaluation around 2159–2162.

Ctrl+F8 opens informational explanations for unknown visibility and unresolved visible text. Search, source lines, scoped dependencies, missing properties/language entries, unsupported functions, duplicate IDs, cycles and runtime artwork are explained without editing XML. The actual Titan Control.IsVisble spelling is flagged with a suggestion only. Pending property drafts block source jumps and remain intact.

Condition evaluation is bounded to depth 64, 4096 work units and 8192 expression characters. Diagnostics bound dependency traversal to eight levels and output to 1024 element findings plus a limit notice. These bounds return unknown rather than guessing.

Equivalent source paths now share one canonical cache identity. The probe exposed a nested BasicHome source loaded twice through mixed slash aliases; normalizing before cache lookup fixes restored redo consistency. Two regression checks cover alias identity and rejection of paths outside the skin root.

## Fresh validation and limits

Core-Results.txt contains the final 300-check run. Probe.cs and Results.txt cover diagnostics, exact sample/report state after save/reopen, XML export and ordinary/restored undo/redo. Original source hash comparisons cover loaded files, not the entire installed skin. Windows-Results.txt records the attended report/search/draft-protection test. No new installed MediaPortal client run occurred.

Before.txt used the previous build and its generic Video sample on media pages; After.txt uses matching typed folder scenarios. These are explicit scenario comparisons, not universal runtime coverage. In both themes unresolved list visibility decreases Video 16 to 1, Music 6 to 4 and Pictures 7 to 0; BasicHome remains 11 and Settings 0. Pictures list still has one unresolved text finding. Video/Music runtime artwork, BasicHome plugin data, ambiguous IDs and the source typo remain unverified. Masks, animation and full native navigation remain approximate.

Only probe code and textual results are committed. Installed skin XML, saved project/language snapshots, exports and desktop captures are excluded. Prior collection and installed-client results are historical and were not repeated for this checkpoint.
