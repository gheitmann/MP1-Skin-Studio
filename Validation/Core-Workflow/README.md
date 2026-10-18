# Core workflow feasibility gate

Date: October 18, 2026. Baseline: f21dd308fc020c88009e1866f662183d1a45d112.

The owner prioritized the essential edit/save/reopen/export/client workflow over
further auxiliary features. This checkpoint tests an installed real Titan page,
rather than adding another editor feature or relying only on fixture counts.

## Candidate edits

| Target in settings.xml | Original | Candidate |
| --- | --- | --- |
| Menu heading label | language ID 924 | STUDIO TEST |
| Menu heading X/Y | 152 / 225 | 172 / 235 |
| Menu heading width | 500 | 440 |
| Header icon texture | icon_settings.png | icon_music.png |
| Header icon width/height | 58 / 58 | 70 / 70 |
| Button StackLayout gap | 7 | 11 |

Only settings.xml was edited. Control IDs and navigation properties were retained.
The loaded original settings.xml hash after preparation was
78b2b21deff4355a588cd0830865b8d3aab2d9185d6cc9fde58373aaf7e5bf31.

## Established

Probe.cs was compiled against the current production editor executable and run
with the installed Titan root and installed German/English language files.
All six stages in API-Results.txt passed: unique target selection, editing through
production APIs, snapshot save/discard/reopen, exact ZIP export, reopened-snapshot
undo/redo and unchanged original source hashes. This was not a claim that the
edits were entered by mouse/keyboard in the editor.

The saved project was then opened in the actual editor window. The preview was
visually observed with STUDIO TEST, the music icon and modified menu arrangement.
The production executable was not modified for this test.

The reopened project's ZIP was extracted into a separate full Titan skin copy.
The applied settings.xml was independently checked equal to the ZIP content.
A separate runtime configuration was prepared; it and the skin copy remain
local and are not committed.

## Runtime gate: still pending

The running original MP1 was closed normally. A launch of the installed original
MediaPortal.exe with /windowed, /NoTheme, an absolute /skin path and a separate
/config did not produce a targetable window or a new client log in the observed
interval. The launch attempt was canceled. No new DirectX failure was observed,
and no DirectX investigation or replacement was performed.

A local Start-Core-Workflow.lnk points to the installed executable with those
test-only parameters. The owner was asked to launch it manually after automated
startup and Explorer navigation failed to produce a visible result. Successful
rendering of the exported candidate has NOT yet been observed.

The absolute skin path is based on the upstream SkinOverride parser and
GUIGraphicsContext.Skin using Config.GetSubFolder/Path.Combine, not an assertion
that this installed runtime has passed the test:

- [MediaPortal.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/MediaPortal.Application/MediaPortal.cs)
- [GraphicContext.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GraphicContext.cs)
- [Config.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Utils/Configuration/Config.cs)

After manual startup, open Settings and verify the candidate text/icon/geometry,
focus, first/last wrapping and entry/return from a normal submenu. Do not toggle
the PIN/security setting. Finally return to the normal installed Titan startup.
The original skin remains available and unchanged.

## Assessment and next priority

The model/persistence/export chain works on this real page and the saved result
is visible in the editor. The decisive MP1 export-rendering/interaction gate is
still open. Do not call the full core workflow successful until that gate passes.
Actual real-page mouse/keyboard editing also remains to be exercised separately.

The editor preview includes MiniDisplay while the earlier live client hid it;
runtime plugin state and label overrides are not yet mirrored completely. This
is a core preview-reliability limitation to track, not a reason to add unrelated
group/style conveniences. Further auxiliary work is deferred until this gate is
resolved. Private configuration, desktop captures and full copied skin assets
are excluded from this commit.

## Reproduction of the API stages

Build the editor, then compile Probe.cs with the .NET Framework C# compiler,
referencing MP1SkinStudio.exe, System.Core, System.Drawing, System.Windows.Forms,
System.IO.Compression and System.IO.Compression.FileSystem. Run the probe next to
the editor executable with three arguments: Titan root, a separate output
directory and the MP1 language directory. Keep output outside the original skin.
