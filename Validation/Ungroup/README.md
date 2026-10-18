# Neutral group removal

Date: October 18, 2026. Baseline: caf3503271653020100c1dceba96dd8273336ea6.

Added **Ungroup selected group** to the Element menu. Direct child controls are
promoted to their wrapper's parent and selected together. Nested child groups,
IDs, geometry, source fragments, comments and processing instructions are
preserved. Structural identity remapping keeps editor flags on the correct
controls. Undo restores the exact wrapper and group selection; redo restores
the promoted child selection.

## Scope and protection

Supports ordinary groups with neutral geometry (absent/zero position and size),
no active/bound layout, concrete nonnegative ID when present, and no behavioral
properties, style attributes or unknown extensions. Nonneutral geometry,
automatic ancestors, locked descendants, editor-hidden wrappers, ambiguous
metadata and included sources not opened as their own page are rejected before
any mutation. This includes the neutral wrappers created by the grouping tool;
it is not a conversion tool for arbitrary runtime layouts.

Loaded-page navigation and control-condition references are checked, including
descendant references and unresolved or complex condition targets. Literal
references in other root/theme XML files are checked as well. Dynamic references
in unloaded pages or plugin code cannot be fully resolved. Source encodings and
XML validation continue through the existing atomic transaction path.

Saved projects retain the resulting snapshot. The existing project format
restores an undo step to the original on-disk baseline after reopening; it does
not serialize the complete earlier in-session undo stack. Tests distinguish that
contract from in-session wrapper restoration.

## Tests

Fresh Windows build succeeded; **221 core checks and 203 UI checks passed**,
both with exit code 0. Full reports: Core-Tests.txt and UI-Tests.txt.

New core coverage exercises nested byte preservation, comments/processing
instructions, preview geometry, marker remapping, saved snapshots and undo,
behavior/style/inheritance/ID protection, locks and automatic ancestors, stale
objects, navigation/condition references, export preservation and source bounds.

New UI coverage invokes the actual Element menu item, verifies pending-edit
protection and checks the promoted selection and exact undo/redo selection.

No MediaPortal input was needed. Original skin XML and live client configuration
were not changed. DirectX replacement remains explicitly deferred. The full MP1
spacing/export comparison is still pending.
