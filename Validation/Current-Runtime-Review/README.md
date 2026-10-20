# Current editor to installed-client review

Executed October 20, 2026 with the executable from source 07e8ef2.
Executable SHA256: edb59ea32a5109edf5dd853e2855a2b30fad7cb4e1431f5f88622d012876a31e.
No editor engine or Windows event code changed in this milestone.

## Fresh workflow and evidence

A fresh project from installed Titan myvideo.xml changed the default video list
width from 698 to 578 and the shared common.time.xml header to STUDIO REVIEW READY.
Video folder and German language samples were saved explicitly. The extracted
current review-package editor opened this project through its startup path.

Attended Windows editing then changed Width from 578 to 590, applied it and saved
the project through the actual form. The title returned to clean. A separate
probe process reopened that saved archive and generated the final export, proving
that the attended edit persisted. Export was performed by the API, not through a
new native Save As dialog. Exactly common.facade.video.default.xml and common.time.xml
plus instructions were exported. Both XML entries match exact project bytes and
original/export SHA256 records. Restored Undo/Redo passed; all 17 loaded original
XML hashes stayed unchanged.

A fresh complete installed runtime copy, SkinStudio-Current-Review, received only
those two XML files after checking its original target identities. Its 2,050 files
differ from Titan in exactly those two entries. The client selected this copy
through Settings > GUI > Skin. The new header appeared on BasicHome and Video;
the shorter Video focus band and folder/count 1 appeared correctly. Left opened
the side menu and Right returned to the list. No media item was activated.

Titan was restored using the same client UI. Normal Home/Videos headings and the
wider original Video focus band returned. All 2,050 installed original file hashes
match the pre-test snapshot. The test copy remains available for reproducibility;
originals and older test copies were preserved.

The Windows interaction used the Computer Use skill. The launch helper returned
before a targetable client window, and a fresh inventory recovered the running
client without launching twice. A temporary user-input notice in the editor led
to a fresh observation before the planned width edit. Some MP1 screens required
an additional observation after transitions. H switches BasicHome/standard Home;
standard Home exposes Settings. These are interaction notes, not application errors.

## Scope and previous results

This milestone closes the fresh sampled client gate for this exact executable,
Default-theme Home/Video and two-file export. It is not a pixel-perfect renderer,
all-theme/all-page/plugin certification or playback test. The owner can review
other intended edits using the same isolated-copy procedure. General publication
and automatic application are not performed.

The preceding 204 synthetic UI checks, eight startup scenarios, twelve demo
workflows and twelve clean-session workflows remain historical and were not
repeated because the executable and samples are unchanged. The earlier 394 core
checks and Titan recovery matrices likewise remain historical. Final packaging
compares every unchanged runtime payload byte with the accepted package; only
updated guide/notes/revision metadata differ.

Prepared-Results.txt and Resumed-Results.txt describe probe stages;
Attended-Results.txt records observed editor/client behavior;
Preservation-Results.txt records complete original preservation. Payload-Results.txt
records package continuity. No installed skin XML, project, captures or private
client logs are committed or included in the deliverable.

## Reproduce

Compile Probe.cs against the current MP1SkinStudio.exe with the Windows .NET
Framework compiler and Drawing, Windows.Forms, IO.Compression and
IO.Compression.FileSystem references. Keep the probe beside the executable.

```powershell
.\CurrentRuntimeProbe.exe C:\Skins\Titan C:\Review\Current C:\Languages\strings_de.xml
.\MP1SkinStudio.exe C:\Review\Current\Current-Video.mp1skinproj
```

Change the concrete default list's Width to 590, Apply and Save Project. Then:

```powershell
.\CurrentRuntimeProbe.exe --resume C:\Review\Current
```

Apply only the two exported XML entries to a complete separate runtime copy after
matching the original hashes. Select it in the client, inspect Home/Video and
Left/Right navigation, restore the original skin, and compare all original file
hashes. Never overlay the source skin. User-specific paths/resources remain local.
