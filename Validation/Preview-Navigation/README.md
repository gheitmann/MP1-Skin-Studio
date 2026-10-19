# Preview navigation from simulated focus

October 19, 2026.

The existing Navigation mode arrow-key test now starts from the simulated focus rather than the editor selection. Facade navigation reads the active collection template. Targets are probed under their prospective focus before checking visibility, allowing focus-dependent menus to become visible. Probing restores the prior state; selection and persisted focus change only after a successful target resolution. Hidden, editor-hidden, ambiguous or unverified targets are rejected. Expected unavailable directions now produce a status notice instead of a modal dialog. Pending property inputs still require Apply or Revert first. This does not emulate native hidden-target skipping.

## Fresh validation

256 core checks passed. New checks cover target-focus visibility without state leakage, rejected hidden/unverified directions, direction validation and active list/filmstrip facade routing.

Real Titan routes passed in both Default and Christmas BasicHome: 4→6→7→5→3→2→4→4→6. Every tile is visited, the self-link is retained, final focus 6 survives an exact saved project/session roundtrip, and all 32 loaded source hashes remain unchanged. Source XML and undo history stay unchanged.

Settings explicit edges 3→20→3 passed. Its first button has no explicit ondown direction: the probe initially exposed this and the final check verifies that an unavailable direction preserves focus. MP1 automatic group navigation is still unsupported and remains the next concrete navigation gap. No inferred neighbor was inserted into XML.

No new interactive keyboard/UI run or MP1 client run occurred; the production routing model and status-handling implementation were checked. Existing BasicHome image reports remain historical.
