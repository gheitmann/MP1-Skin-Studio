# Durable skin-copy saving validation

October 29, 2026. This milestone adds Save skin beside Live. It writes a managed
output copy, leaving the current source and MP runtime baseline intact.

Fresh checks:

* **471 core checks passed**, including 16 new persistence cases: first complete
  copy, repeated saves, exact backups, saving Undo, external and late conflicts,
  multi-file rollback when a later file is locked, stale receipts, source
  identity/baseline drift, immutable reviewed bytes, exact review hashes, UTF16,
  project round trip, XML page discovery, unsafe receipts, read-only targets and
  invalid output locations. See Core-Results.txt.
* **13 focused dialog checks passed**: applying valid pending inputs, target XML
  review, background update, up-to-date state, source preservation, saving Undo
  while retaining Redo, minimum-size Toolbox layout, forced first-copy options
  and safe conflict recovery. See UI-Results.txt.
* **13 Studio/native-MP-SDK integration checks passed**, including complete copy
  creation and saving Undo while the live connection and native preview remain
  active. This is an owned host with official MP GUI control objects, not a new
  full MP renderer acceptance. The earlier real-MP acceptance remains historical
  evidence for the unchanged live transport and native property adapter.
* **17 existing toolbox/geometry workflows passed** with the added toolbar
  action; 488 installed DefaultWideHD XML hashes remained unchanged.

Run `Run-Probe.ps1 -OutputDirectory <fresh directory>` after building Studio.
For the native live integration, build Validation/Live-Bridge/StudioProbe with
Build-StudioProbe.ps1, then pass the isolated test skin, a fresh report directory
and `persist`. Normal desktop process rights are needed for its named pipe.
Copying/saving uses background tasks while the owned UI pumps messages, matching
the actual dialogs. A synchronous UI-blocking test would exercise watchdog
disconnect instead of the implemented workflow.

No installed configuration or skin files were written. Private test copies,
receipts, backups, captures and absolute output paths stay outside the repository.
