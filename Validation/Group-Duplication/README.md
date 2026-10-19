# Self-contained group duplication and structural selection

October 19, 2026.

## Result

Duplicate now accepts top-level, self-contained groups with ordinary nested groups (up to 512 controls). Every copied control receives a new available ID. Internal directional links, defaultcontrol and literal Control.HasFocus/IsVisible/HasText/HasThumb visibility references follow the new IDs. Effective references inherited from defaults/styles become local overrides on the clone; shared style files and external targets remain unchanged. Only the outer group position is offset by 30 pixels; local child geometry, bindings, styles, comments, attributes and unknown XML remain intact.

Copies reject locked descendants, included/imported controls, nested view templates, ambiguous/dynamic IDs, dynamic navigation and recognized unsupported internal references, including XML attribute references. General runtime expressions, scripts and dynamic references are not a full dependency analysis. They still require manual review before applying an exported skin. ID allocation now also reserves static defaultcontrol targets and literal control-function targets in unopened XML files and attributes.

Insert/Duplicate capture selection before and after the operation. In-session Undo restores the original selection; Redo selects the new element/group. Saved projects retain structural XML identities and support their existing aggregate restore undo; selection snapshots are session-only.

## Fresh validation

- Windows .NET Framework build: 272 core checks passed, zero failed (nine new group/ID regression cases).
- The console probe passed Default and Christmas Settings workflows: 12 copied controls, eleven-button down and up loops staying within the clone, project save/reopen, exact exported XML, in-memory and restored-project undo/redo.
- Both workflows compared all 361 cached source hashes with their original bytes, with zero differences. The probe's first run used the first loaded include instead of settings.xml for its undo assertion; the corrected source-specific probe passed.
- Attended Windows editor: selected the Settings buttons group; Duplicate changed 20 to 32 controls and selected buttons (Copy), ID 10006, position 152/351. Undo restored 20 controls, original group selection at 122/321 and a clean project caption. Redo restored the clone selection and unsaved caption. Ctrl+S cleared the caption. Normal close/relaunch restored 32 controls with the copy present and a clean caption. A later Christmas theme switch and Ctrl+S were confirmed in the saved manifest (page settings.xml, theme Christmas, only settings.xml changed).
- UI automation used the computer-use skill. One stale menu index and one capture failure required fresh window observations; the original window remained running. No repeated action was sent from a stale observation. A coordinate action changed preview zoom during the supplementary theme test; it did not edit skin XML.
- No new installed MediaPortal client run or export installation occurred.

Source originals are preserved. Runtime visual equivalence, general dynamic-reference rewriting and group templates with shared includes are not claimed.
