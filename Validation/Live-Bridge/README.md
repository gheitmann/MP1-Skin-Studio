# Live bridge validation

October 29, 2026. Target SDK: installed official MediaPortal 1.39.0.0.

The original installed skin and configuration are read-only test references.
`Prepare-Runtime.ps1` creates a separate MP client, config, language, logs,
database, thumbnails, cache and DefaultWideHD skin. It retains only selected
standard GUI plugins and the new process plugin. Test controls are added only
to its copied myHome.xml. `PathsProbe.cs` confirms MP directory resolution.

## Reproducible probes

* Studio `--selftest <output>` includes protocol framing/entity/path checks,
  duplicate ID matching, theme normalization, drafts, Undo, ambiguous controls,
  unsupported fields and source-hash drift.
* `TransportProbe.cs`, compiled with `LiveServer.cs`, `SessionContract.cs` and
  `src/LiveProtocol.cs`, exercises an authenticated same-user pipe, command
  dispatch and restoration when the client disappears. Run with a fresh output
  directory as its sole argument, under normal Windows desktop permissions.
* `Run-Native.ps1 -ClientDirectory <isolated MP client> -SkinDirectory <copy>`
  compiles/runs `NativeProbe.cs` against that client's official Core.dll. It
  tests native label/color/position changes, atomic invalid-frame handling,
  stale sessions, exact animation/geometry restoration and tree removal/reset.
* `Build-StudioProbe.ps1 -ClientDirectory <isolated MP client>` builds
  `StudioProbe.exe`. Run with the test skin copy and a fresh report directory.
  This owned test host uses original MP GUIWindow/GUILabelControl objects and
  a real bridge/server/client, while driving owned Studio inputs. It checks
  authentication/hash verification, typing without Apply/XML writes, geometry,
  invalid input, Revert, Apply, Undo, in-progress/cancelled gesture previews,
  Disconnect and unchanged disk source. It is not the full MediaPortal renderer.
* `RuntimeProbe.cs`, compiled with LiveProtocol.cs, connects to an actual MP
  process using its isolated endpoint directory. The skin must contain the
  deterministic controls inserted by Prepare-Runtime. Optional second argument
  `hold` holds the changed text/position/color for an attended visual check.
  It also checks an invalid frame, empty-frame restore and reconnect/reset.
* `StudioRuntimeProbe.cs`, compiled with references to the editor, System.Core,
  System.Xml, System.Drawing and Windows.Forms, drives owned Studio inputs over
  the real bridge to an already running isolated MP process. Its arguments are
  the copied skin, endpoint directory and a fresh result directory. Run under
  normal Windows desktop rights. It verifies text/geometry/color, unchanged
  runtime identity, invalid drafts, Revert/Apply/Undo, gesture streaming,
  Disconnect/reconnect and unchanged disk XML. It holds its changed visual for
  40 seconds for an attended native-window comparison.

## Executed evidence

Core: 455 checks passed. Native SDK: 13 checks passed. Studio-to-native-SDK
integration: 11 checks passed. Authenticated transport/crash restore passed
under normal Windows rights. The pre-existing 17 toolbox/geometry workflows
passed with the added Live toggle; 488 installed XML hashes remained unchanged.
Packaged acceptance: twelve full demo edit/save/reopen/export/Undo workflows,
twelve clean-session workflows and 229 broad UI checks passed. The broad UI
host completed under normal Windows rights. The final two insertion-template
checks were added afterwards, with no changes to existing non-live behavior.
All 489 original installed skin/config XML hashes were rechecked unchanged.

The final official MP 1.39 executable ran its real DefaultWideHD-derived Home
page with the deterministic controls. Ten direct runtime checks and eleven
complete Studio-to-real-MP checks passed. The native window visibly showed
the text typed in Studio, its changed position and yellow color without a page
reload. Revert/Undo and Disconnect/reconnect restored the original baseline.
The MP process and runtime control epoch stayed unchanged during live edits.
See Real-Studio-Results.txt and Results.txt. All 488 original installed skin XML
hashes were rechecked unchanged after this real MP run. The installed process
plugin DLL hash matches the tested package after owner-confirmed Windows
administrator installation. Normal Windows rights are required for the pipe;
the earlier restricted sandbox launch could not accept clients.

The installed MP configuration differs from the initial baseline across
concurrent Configuration Studio work in another chat; it was not overwritten
or reverted. No configuration-preservation claim covers that concurrent work.

No installed skin, MP binaries, private configuration, endpoint nonce, user
paths, captures or runtime logs belong in this repository's validation evidence.
