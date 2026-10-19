# Bounded automatic stack navigation

October 19, 2026.

MP1 uses a geometric fallback when a navigation direction is absent or zero; it does not generate every missing link from a group layout. The preview now covers a bounded linear case: direct button children of a supported horizontal or vertical StackLayout can move along the stack axis in source order. Explicit mappings retain precedence, including invalid or hidden targets. A known hidden candidate is skipped; unknown visibility or ambiguous IDs stop the probe. At an unlinked stack boundary focus stays on the source. No derived links are written into XML.

Prospective visibility probes restore focus on every path. Cross-axis movement, nested groups, grid/free-position geometry, non-button candidates and native hidden-target skipping remain unsupported. Editor-hidden candidates are skipped in this preview. Facade explicit navigation continues to use the active collection template before considering fallback.

The [upstream GUIControl.OnAction/Navigate behavior](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIControl.cs) was consulted. This is a documented linear subset, not a full port of its angle/distance search. No upstream implementation was copied.

## Fresh validation

258 core checks passed. New regression coverage exercises vertical/horizontal stacks, labels and hidden candidates, boundaries, reverse travel, explicit-target precedence, unknown-visibility rejection, unchanged state and active-facade navigation through the fallback dispatch.

The real Titan Settings route passed a complete down/up loop across all eleven buttons: 3,4,5,10,7,8,9,21,6,11,20 and back. Explicit wraparound edges and automatic neighbors work together. XML remains clean and no undo entries are generated. The previous probe had documented missing ondown as unsupported; that specific gap is now covered.

Default and Christmas BasicHome routes also passed unchanged: 4→6→7→5→3→2→4→4→6. Saved final focus 6 and full editor signatures survive reopening; all 32 loaded source hashes remain unchanged for each theme.

No new interactive keyboard/UI run or MP1 client test was performed. Other layouts and geometric fallback remain the next navigation coverage gap.
