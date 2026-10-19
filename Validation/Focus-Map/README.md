# Independent page/theme focus persistence

October 19, 2026.

Editor projects now retain up to 128 independent focus states keyed by page and theme. Selecting a saved focus again does not modify the editor signature or create an unnecessary unsaved marker. Default focus on a previously unseen page remains implicit. Restoration requires a unique focusable logical control; missing or ambiguous IDs fall back to the page default.

New snapshots include bounded focus entries and retain the legacy last-focus attributes. Older single-focus projects migrate into a slot for their recorded theme (or the active theme if no theme attribute exists). Duplicate slots, invalid IDs, inconsistent last-focus metadata and oversized maps are rejected. Presets validate focus capacity before replacing sample values. Skin XML and undo history remain independent of focus state.

## Fresh validation

261 core checks passed, including independent pages/themes, stable signatures on restoration, legacy migration, duplicate-entry rejection and preset failure atomicity at capacity.

Probe.cs checked four real Titan states through page/theme changes and save/reopen: Default BasicHome=7, Settings=4; Christmas BasicHome=2, Settings=5. All four remained independent. Repeated restoration left the complete content signature unchanged. XML remained clean, no undo entries were created and all 34 loaded original source hashes remained unchanged. A real older BasicHome project migrated with focus 5.

No new interactive Windows-editor or client run was performed for this checkpoint. The preceding attended navigation/caption report remains historical.
