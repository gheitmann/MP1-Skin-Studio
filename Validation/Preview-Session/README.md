# Reproducible clean Video preview

October 19, 2026.

Saved editor projects now retain the last explicit preview focus, scoped to the page. Session preparation and initial selection restore that focus instead of reopening Titan's hidden menu. Missing or non-focusable IDs fall back to the page default. Older projects without focus attributes still load normally. The Video folder preset saves focus and clean-preview state together.

Element > Show unverified content controls unsupported visibility states, unresolved labels and image placeholders. Clean mode omits missing images (including runtime-cache artwork); diagnostics and raw XML remain available. Explicit literal sample values remain literal. Empty image textures no longer produce fake rectangles. This is a display option, not an assumption that unknown runtime values are empty.

## Validation

247 core checks passed, including save/reopen/page switching, malformed focus rejection and known/unresolved label handling. A targeted real Titan probe passed: 211 controls, 91 hidden, 87 unresolved; list focus 50 survives project roundtrip and prepared-session loading without manual focus injection. Nested German localization and known-content conditions remain correct. All 17 loaded source hashes, XML signature and undo history remained unchanged. The production offscreen render and interactive reopened project were visually inspected: Videos folder and list visible, side menu closed, unresolved artwork boxes absent, no unsaved marker on initial reopen.

Full interactive editing, other themes and MP1 playback were not retested in this checkpoint. Earlier installed-client export validation remains historical. Remaining differences include fixed sample clock/date, font metrics, rating indicator and unsupported runtime expressions. This checkpoint hides unverified content; it does not resolve the 87 unknown conditions.
