# Group creation validation

Date: October 18, 2026. Baseline: main at 769b359c53e840089b6e63b6ab1a0ec2b4fe5c74.

The Element menu now offers **Group selected controls** for consecutive sibling
controls on the open XML page. The new ordinary group retains child source bytes,
IDs, order, comments and positions. It has a fresh ID, neutral geometry and an
explicit empty layout. Selection, hidden state and structural identities follow
undo/redo and project save/load.

Grouping rejects source/parent boundaries, nonconsecutive controls, included
sources not opened as their own page, automatic layouts, locked descendants and
behavioral group defaults. Existing group subtrees can be wrapped without
rewriting their contents. This milestone implements creation; ungrouping and
moving controls between groups remain separate work.

Fresh Windows build: successful. **211 core checks and 199 UI checks passed**,
both with exit code 0. Full reports: Core-Tests.txt and UI-Tests.txt.

## Reproduction

Build with build.ps1. Run the GUI executable with Start-Process -Wait -PassThru:

```powershell
Start-Process .\MP1SkinStudio.exe -ArgumentList '--selftest', "$env:TEMP\MP1-Group-Core" -Wait -PassThru -WindowStyle Hidden
Start-Process .\MP1SkinStudio.exe -ArgumentList '--ui-smoketest', "$env:TEMP\MP1-Group-UI" -Wait -PassThru -WindowStyle Hidden
```

Inspect the exit codes and generated text reports. Core tests cover byte-exact
undo/redo, nested preview bounds, identity mappings after saving, IDs, import
barriers, inheritance protection and export preserving original files. UI tests
exercise the actual menu callback, pending-edit protection and restored selections.

These are newly executed editor tests. No MediaPortal window input or capture was
attempted. Client rendering/navigation comparison is still blocked by
https://github.com/openai/codex/issues/55114; the owner's stop instruction remains
in force. The DirectX dependency task remains explicitly deferred.
