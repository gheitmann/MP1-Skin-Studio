# Copy a skin as a new independent base

Date: October 20, 2026. Parent: f43a326.

Copy skin beside SKIN opens a name/location dialog. Every source file and empty
subdirectory is copied, including Media, fonts and Themes. Applied editor XML
can replace its disk counterpart in the copy, preserving encoding/BOM through
SkinSource.Bytes. The source/session/undo stack are never modified. Capture takes
an independent byte snapshot before dispatching the background task. Disk-only
copying is available; preview/editor settings are not transferred to the new
session. New skin identity is the folder name; existing metadata is retained.

The worker validates names and containment, refuses existing targets and linked
paths, copies in cancellable chunks into a unique sibling staging folder, checks
edited source hashes and validates the copied page/theme before publishing with
a directory move. Failure/cancellation removes only the owned staging folder.
The UI stays responsive with marquee progress and Cancel. Closing while busy
requests cancellation and waits for cleanup. There is no overwrite flow or client
configuration change. Existing save/discard/cancel guards protect optional opening;
cancelling that transition retains both the original session and completed copy.
Custom-location copies remain in the dropdown for the current application session.

Final executable SHA256:
4200357e65e09e665e43b15a905268c5d825cb7fc572915a5f416b010173c05a

Fresh checks:

- 430 core checks passed, including eight copy cases for complete assets/empty
  folders, applied versus disk XML, immutable snapshots, names/containment and
  existing targets, source drift, cancellation/locked files, invalid copied XML
  and escaping edited paths. The initial fixture lacked its nested theme folder;
  the fixture was corrected before these acceptance checks.
- Eleven synthetic dialog/button workflows passed on the final executable:
  pending-input guard, including XML, keeping the original session, custom
  dropdown registration, cancelled/discarded session transition, opening a clean
  disk base, button bounds, invalid-name recovery and worker cancellation.
- Twelve full demo and twelve clean-session scenarios passed on the extracted
  final candidate; all 31 payload hashes remained unchanged.
- The 23 sidebar/scroll scenarios passed during implementation. The final layout
  adds six pixels of header height and enough space for the new button caption.
- Complete DefaultWideHD copy: all 3,399 files matched byte-for-byte, all 3,399
  original hashes stayed unchanged, and copied myHome reopened with 262 controls.
  This ran before the final button-spacing adjustment; copy logic is identical.
- Live Windows inspection confirmed the final readable button and copy dialog.
  No copy was created in the installed skin directory through desktop input.
  The final review window stays open; prior owner sessions remain intact.

Previous readable-preview, broad UI, integrated-page and client results remain
historical. Final packaging updates guides/revision metadata and checks every
payload entry against its manifest. No installed skin assets, projects, desktop
captures or private logs are included in GitHub or the review ZIP.

Run the dialog scenarios with Run-Probe.ps1 using fresh output directories.
RealCopyProbe.cs accepts a source skin and an isolated output parent; compile it
beside the accepted executable to repeat complete hash/copy/load verification.
