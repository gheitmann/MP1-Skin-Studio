# Release 0.9.0 acceptance

November 16, 2026. The final Windows package uses the same editor and
LiveBridge 1.3.0.0 binaries as the audited release candidate.

The final page probe verified the XML source against the native MediaPortal
1.39 process before applying changes. It used an independent DefaultWideHD
copy and the standard MediaPortal plugins.

| Page | Linked editor elements | Native controls | Verified operation |
| --- | ---: | ---: | --- |
| Plugin overview (`myHomePlugIns.xml`) | 128 | 262 | Move list #50, then Undo |
| Videos (`myvideo.xml`) | 76 | 82 | Move label #91919291, then Undo |

Both changes took effect in their original native windows and Undo restored
their exact original positions. The Studio renders were inspected separately.
They show sample content for the editor layout rather than live media data.
A native screenshot capture request timed out, so the cross-client comparison
uses the native control snapshots and does not establish visual equivalence.
A separate Music Genres trial exceeded the isolated client's command timeout
and is excluded from the passing results.

To reproduce, compile Probe.cs beside the packaged executable, referencing
MP1SkinStudio.exe, System, System.Core, System.Windows.Forms and System.Xml.
Pass the isolated IPC directory, the test MediaPortal PID, copied skin root
and an existing writable report directory. The probe captures Studio images
to that directory after each successful page check.

The release candidate also passed 490 core checks, 229 desktop UI checks,
the guided Live and saved-skin acceptance, and 2,225 model page/theme loads.
See [First steps validation](../First-Steps/README.md).
