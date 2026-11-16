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

## Music Genres timeout follow-up

The released bridge held MediaPortal's render lock across window activation.
Music Genres waits for its directory worker, which clears the facade's list.
The native list needs the same render lock to clear its items. The GUI thread
and worker therefore could not finish while the activation held the lock.
The bridge now releases the lock after restoring preview overrides and lets
MediaPortal activate the window on its GUI thread. Snapshot, frame and cleanup
operations retain the render lock.

An isolated A/B run used the same MediaPortal 1.39 installation, skin, database
and probe with only the bridge changed. The released build hit its in-flight
timeout after 8,022 ms. The patched bridge activated Music Genres and returned
its native snapshot in 418 ms. Plugin overview and Videos still passed their
native move and exact Undo checks. The native screenshot request in the
original release check timed out while awaiting app access. It was independent
of the bridge command.

The probe's optional three-argument mode takes the IPC directory, process ID,
and window ID. Window 504 exercises Music Genres activation and snapshot.

## Version 0.9.1 final verification and image comparison

The versioned LiveBridge 1.3.1.0 activated Music Genres and returned a native
snapshot in 618 ms. It then opened the Plugin overview in 374 ms.

NativeCapture runs as a test-only plugin in the isolated client. It saves the
native render surface and the Plugin overview list state. MatchedPreview feeds
those exact list strings, selection and focus into the release editor.
Both frames show DefaultWideHD, its default theme, focus control 50 and the
selected My Notifier row. The background, list panel, selected row and lower
description panel align closely. The native plugin icon and description are
replaced by editor placeholders, the selected text uses a different color,
and the clock uses editor sample data. The comparison is specific to this
page and state.

For reproduction, compile NativeCapture.cs as an isolated process plugin with
the MediaPortal SDK and its bundled SharpDX assemblies. Set
`MP1_SKIN_STUDIO_CAPTURE_DIR` to an empty writable validation directory.
Open window 34 and create `capture.request` inside it. Then compile
MatchedPreview.cs beside the versioned editor and run it with the copied skin
root and capture directory. The capture plugin belongs only to the fixture.

To reproduce, compile Probe.cs beside the packaged executable, referencing
MP1SkinStudio.exe, System, System.Core, System.Windows.Forms and System.Xml.
Pass the isolated IPC directory, the test MediaPortal PID, copied skin root
and an existing writable report directory. The probe captures Studio images
to that directory after each successful page check.

The release candidate also passed 490 core checks, 229 desktop UI checks,
the guided Live and saved-skin acceptance, and 2,225 model page/theme loads.
See [First steps validation](../First-Steps/README.md).
