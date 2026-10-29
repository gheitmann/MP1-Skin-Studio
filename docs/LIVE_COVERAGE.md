# Extended live controls — bridge 1.1

This development package extends the existing MP1 1.39 live connection. The
ready status now includes the running bridge version.

## Improvements

* Scrolling and multi-line text controls expose text, loaded font and color.
  Replacing text also clears an old binding flag; restoring the original text
  reinstates its binding. Font changes retain the native font cache for restore.
* Lists and playlists support row height/spacing, icon width/height, primary
  font/color and text offsets. Icon height and row height are separate values.
* Filmstrips support texture and thumbnail dimensions, primary font/color and
  safe item sizing. Gallery controls add aspect-ratio, font/color and offsets.
* Ordinary group alignment uses MP's group-alignment property.
* Collection item sizing preserves list data. Layout changes rebuild visuals
  through the native layout path instead of disposing the whole collection.
  Rebuilds occur only when a relevant property actually changes, and preserve
  the selected item where available. Text reflow can restart its scroll position.

## Updating

Close MP once to install the included **LiveBridge 1.1.0.0** DLL using
`LiveBridge/Install-LiveBridge.ps1` or the setup steps in LIVE_PREVIEW.md. Start
MP again and reconnect Studio. The ready status identifies the loaded version.
Program Files may require the normal Windows administrator confirmation. The
plugin was not silently replaced in an already running installation.

This initial plugin update requires a restart. Ordinary supported property
changes continue to preview without a restart, XML export or page reload.
Save skin copy retains its existing separate-output behavior.

## What the audit establishes

The DefaultWideHD audit loaded **449 pages**, expanding **19,490 controls**.
Among **114,474 explicit editor-field occurrences**, SDK-writable mappings rose
from **108,736 to 111,528**: **2,792 additional mappings**. All 488 installed XML
hashes stayed unchanged. This measures available property mappings, not a
rendering-accuracy percentage or proof that every plugin page works in MP.

Fresh validation includes 64 extended native-adapter/session checks at a scaled
window size, 13 baseline adapter checks, 13 Studio/native-SDK live/save checks
and 471 editor core checks. The new specialized controls have not yet received
a renewed visible comparison in the complete MP renderer; Configuration Studio
is active in another chat and was preserved. Earlier attended real-MP tests
remain evidence for the existing live path, not a new per-control visual pass.

## Remaining limits

Special checkbutton textures, large/small gallery thumbnail templates, effects,
global font/reference hot reload and plugin-specific controls remain outside
this extension. Game controls registered by their own plugins are not resolved
by the core-only static audit. Structural edits on ambiguous/repeated XML still
use the existing protection. Unsupported changes are reported explicitly.
