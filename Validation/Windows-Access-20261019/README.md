# Attended Windows access test

Date: October 19, 2026, Europe/Berlin.
Baseline: 761b36def50636a5e651d45e3ce94d0f1fbc17b1.

Fresh access test using the Computer Use sky API:
- Enumerated current Windows windows.
- Launched and activated the existing Windows editor build; observed its Titan demo.
- Opened Sample Data with a mouse click and closed the dialog with Escape. Window enumeration confirmed closure after a stale-dialog capture failed.
- Launched the installed MediaPortal client, activated it and observed Titan Home.
- Right arrow visibly moved focus from TV to Videos. Enter opened Videos; a subsequent capture showed the loaded page with one Videos folder and item count 1.
- Multiple successive client inputs/captures completed without an observed permission loop. This confirms current-session access, not persistence across future sessions.

No original skin files were edited or replaced. No export was installed. No video playback was started. The client remains open on Videos. DirectX replacement remains deferred.

Next: compare this runtime state with an isolated Titan Video editor preview and validate an isolated export.
