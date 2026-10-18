# Session loading, input recovery and write-failure validation

Date: October 19, 2026, Europe/Berlin. Baseline: 52596b8fe141cff1a2bc4a8a137324f993323699.

This coherent checkpoint combines three parts of the core editor workflow.

## Prepare before replacing the active session

PreparedSkinSession loads the available page/theme names, validates preview
state and opens the chosen page before the form replaces its active project.
The requested page is matched without case sensitivity; existing myHome,
BasicHome and first-page fallbacks are retained. Empty or malformed candidate
skins fail during preparation. No filesystem enumeration or page loading is
needed after the active-project assignment.

Theme switching uses the same preparation. A failure restores the previous
theme, while OpenPage restores controls, source cache and page-derived state.
Successful switching retains pending XML edits and undo history in other themes.
Selectors are updated only after successful preparation, with callbacks suppressed
during rebuilding. The GUI integration compiles but remains manually unverified.

## Recover unapplied property input explicitly

Added Revert Inputs next to Apply Properties. It rebuilds fields from the current
model and preserves selection, applied XML edits and undo/redo history. It does
not undo an applied edit. This gives users an explicit way to leave the pending
input guard without manually restoring every field. Existing save/export and
switching guards remain in force. Button layout, event behavior and accessibility
have not been visually or interactively verified under the automation suspension.

## Fresh joint console validation

The final source build passed **233 core checks, zero failures**. Five new core
checks cover candidate preparation/fallbacks, failed theme preparation, successful
theme switching with base edits, failed project replacement, and failed ZIP
replacement. Exclusive locks intentionally make replacement fail; the tests
verify byte-identical usable previous archives, unchanged in-memory edits, and
temporary-file cleanup. Production persistence code needed no change for these
write-failure cases.

All dedicated probes were run sequentially against that same final executable:

| Probe | Passed checks |
| --- | ---: |
| Filmstrip geometry | 6 |
| Known/unknown string conditions | 10 |
| Real Titan complex-page edit/save/export | 11 |
| Prepared real Titan and multi-page workflow | 12 |

The last probe also prepared the installed Christmas theme and returned to the
default skin. It retained edits across the shared video facade and page-local
files, reopened the project, exported all three XML sources and checked unchanged
original files. These 39 targeted checks overlap the areas covered by core tests;
they are not 39 additional GUI checks. See the reports in this directory.

No editor GUI, MediaPortal, window capture or Computer Use was launched. The core
suite includes offscreen drawing tests only. No UI smoke suite was run; prior UI
counts remain historical. Runtime plugin state, masks/animations and visual
equivalence remain open. DirectX replacement is still deferred.

The final source comparison checked the existing tracked files against Git blob
hashes. It also found and corrected three sample-text separators in the saved
repository (replacement characters instead of middle dots), so the checkpoint
matches the source used by this validation. One local source BOM was normalized;
its code content was already identical.

Reproduction uses the console RunCore.cs, Collection-Geometry/Probe.cs,
String-Conditions/Probe.cs, Complex-Page/Probe.cs and Page-Loading/Probe.cs. Keep
generated outputs outside the original skin directory.
