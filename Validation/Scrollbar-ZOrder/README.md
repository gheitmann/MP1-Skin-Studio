# Visible dark scrollbar correction

Date: October 20, 2026. Parent: 8ae31d1.

The owner reported that the wider sidebar still had light Windows scrollbars.
Live inspection reproduced this in the preceding delivery. DarkScrollHost had
added its content first and the overlay siblings behind it. DrawToBitmap showed
the custom bars, masking the incorrect desktop child-window stacking order.
The native content now explicitly uses SendToBack immediately after all siblings
are added. No scrolling, range calculation, viewport or XML logic changed.

Fresh checks on executable SHA256
10ece6be94f7a235252169b48f6d6c463f28588d9e09fac9442b2792109bbd68:

- 420 core checks passed; see Core-Results.txt.
- 23 focused form/menu/scrolling scenarios passed; see UI-Results.txt.
  The new assertion verifies every overlay precedes its native content sibling
  in all four hosts. Existing arrow/wheel/thumb, pending-input and session
  transition checks remain included. All 488 installed XML hashes stayed intact.
- Live Windows inspection of the corrected editor confirmed dark page/tree
  bars in both axes and dark properties/information bars. A subsequent owner
  page change still showed dark bars. The automated click attempt was stopped
  by concurrent user input; state was refreshed and no successful automated
  desktop scrolling is claimed. Native input behavior is covered by the focused
  synthetic scenarios. No desktop screenshots or private data are committed.

Prior integrated-page/readability/package/client reports are historical rather
than newly executed for this localized correction. The delivery ZIP manifest is
independently checked against every payload entry and the accepted executable.
