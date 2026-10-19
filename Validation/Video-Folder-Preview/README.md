# Video folder preview milestone

Date: October 19, 2026. Baseline: 55880dcbb28033cb6d2d88d8e0ea0b453e0093f9.

Element > Video folder preview applies an explicit sample state for a page with a list control: one Videos folder, DefaultFolderBig.png, empty movie metadata, list mode and first-item selection. The UI selects the concrete list and hides unverified conditions. Custom unrelated values, plugins and language snapshots are retained. Collection-derived overrides are removed so item count and selection stay consistent. Other unknown runtime properties remain unresolved.

Both candidate state objects are validated before application. Pending property inputs block the UI action. Preview changes do not write skin XML or add XML undo entries. Existing project persistence stores the state without a format change. The command uses Titan's folder asset name; skins lacking it can supply another image through Collection Preview and Sample Data.

The UI test exposed stale count/selection text when rebuilding PreviewState after changing sample data. PreviewState now initializes derived collection values in its constructor, retaining explicit sample overrides. Regression coverage verifies fresh initialization, unknown/custom-value preservation, XML signature and redo preservation, saved-project roundtrip and exact exported XML. Unsupported pages reject the command without changing state.

Fresh final Windows build: 239 core checks, 25 expression checks and all eleven Complex-Page stages passed. An initial test-directory build lacked bundled Samples, causing fixture-path failures; supplying the unchanged Samples directory and running a fresh test root passed. No product fix was made for that packaging error.

Attended Windows UI: opened the saved isolated Titan project in the new build; applied the menu command; observed one Videos row, a large folder image, no side menu and empty plot; saved and reopened the sample state. Final corrected build shows Videos: 1 and selected text Videos after applying the command. German language persisted. The final sample diagnostics show 89 unresolved conditions. Unknown image/text placeholders, fixed clock/date, unmatched font metrics and runtime behavior remain open. Editor selection itself is not persisted; reapply the command after reopening to restore list focus.

No original skin or language files were edited, no export was installed in MP1, and DirectX replacement remains deferred. Next: resolve targeted remaining runtime conditions and compare an isolated export, without treating hidden unknown conditions as visual equivalence.
