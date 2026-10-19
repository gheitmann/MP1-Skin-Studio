# Page/theme workflows and button focus

October 19, 2026.

Button textures, fallback fills and text colors now follow the simulated focus ID rather than the editor selection. Selecting a non-focusable element can no longer erase a still-active button focus appearance. The Focus preview option disables that appearance. Designer selection outlines remain separate.

## Fresh validation

250 core checks passed. A new rendered-pixel regression uses different selected and focused buttons and verifies focused/unfocused textures, the Focus toggle and unchanged project state.

Probe.cs exercises six real Titan pages in Default and Christmas: BasicHome.xml, myHome.xml, settings.xml, myvideo.xml, mymusicgenres.xml and mypics.xml. Each of the twelve workflows edits a concrete position, saves and discards the model, reopens and prepares the session, verifies the complete content signature, compares every exported XML entry exactly, checks snapshot undo/redo and rehashes all initially loaded original sources. All twelve passed with no original changes. This is a sampled matrix rather than exhaustive property/structural coverage.

Settings and BasicHome were rendered in both themes. The Default Settings render was visually inspected after the fix: General receives the focused cyan texture and focused text color without any editor selection. The Christmas BasicHome render was inspected before the fix: theme assets load, but runtime menu properties are unavailable and the tiles remain incomplete. No claim of complete BasicHome visual fidelity is made.

The matrix rechecks the workflows after the renderer fix; no new MP1 client run or installation was performed. Source assertions and results are retained here. The previous 19 unresolved Video conditions are historical and were not recounted in this checkpoint.
