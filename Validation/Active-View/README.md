# Active project view persistence

October 19, 2026: active page and theme now participate in the editor/content signature. Switching either creates a saveable project change; returning to the original location restores the previous signature when no other state changed. XML/export signatures and undo history are unaffected. Existing project archives already store the location, so no format migration is required.

Fresh Windows .NET Framework build: 263 core checks passed, zero failed. The initial isolated build run lacked the demo Samples directory and failed fixture-dependent checks; the corrected complete build passed. Two new regression cases cover page/theme detection, return to baseline and exact save/reopen. The prior focus-map assertion now measures focus restoration after page preparation, separating the two state changes.

The included console probe passed four real Titan BasicHome/Settings and Default/Christmas combinations, with exact restored signatures and 34 unchanged loaded source hashes. No fresh interactive editor or installed-client validation was performed for this change. The existing caption/session-save code consumes this signature; its interactive paths were tested at the earlier attended-navigation milestone.
