# Reliable page switching and multi-page persistence

Date: October 19, 2026, Europe/Berlin. Baseline: e530ffea461c1794345d66ba3fa736c168e6c7e5.

OpenPage previously changed Page and cleared the current controls, warnings and
page-derived metadata before opening the requested XML. A missing or malformed
page could therefore leave an existing edited session without its active page.

OpenPage now snapshots page state and restores it if loading throws. Restoration
includes the original control objects, source-cache membership, warnings,
defaults/styles/defines, fonts, dimensions, window identity and default focus.
Successful page loading still reuses edited source objects. Undo/redo stacks and
editor state are not changed by page loading. Nonfatal include warnings retain
their existing behavior; this does not make every include warning a fatal error.

The new permanent regression failed before the fix and passed afterward. It
checks missing XML, malformed XML and an out-of-root path, exact content
signature, selected control identity, source cache, undo/redo counts and working
undo after failure. The fresh build and full console core suite passed:
**226 checks, zero failures**.

The dedicated console probe passed nine stages using installed Titan:

1. Change list row height in myvideo.xml through its shared facade source.
2. Switch to myvideoTitle.xml and observe the same edited shared list.
3. Edit a page-local icon, then change the Settings heading on settings.xml.
4. Undo/redo across page boundaries without losing the active page.
5. Save while Settings is active and reopen with all three changed sources.
6. Switch back to the video pages and verify shared and local values.
7. Reject a missing page while retaining the reopened working session.
8. Export all three edited XML files plus instructions, with exact XML content.
9. Confirm all loaded original source files remain unchanged.

See Results.txt and Core-Tests.txt. The probe uses production APIs, not desktop
input. No editor GUI, MediaPortal launch, capture or Computer Use was used.
Visible page-selector behavior and runtime rendering remain unverified under
the automation suspension. DirectX remains deferred.

Compile Probe.cs as a console executable referencing MP1SkinStudio.exe, System,
System.Core, System.IO.Compression and System.IO.Compression.FileSystem. Pass
the installed Titan root and a separate scratch output directory outside it.
The core suite can be run through Validation/Collection-Geometry/RunCore.cs.

The current Probe.cs also tests prepared-session startup and an installed theme.
The historical nine-stage report above is retained; its fresh extended run is in
../Session-Reliability/Page-Results.txt.
