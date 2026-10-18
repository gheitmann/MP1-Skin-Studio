# Grid layout dialog validation

Executed on October 18, 2026. This milestone adds **Edit grid layout** to the
group menu, with columns, rows, separate spacing, and fill direction.

Results: **203 core checks and 195 UI checks passed**. Reports are in
`Core-Tests.txt` and `UI-Tests.txt`; `Grid-Layout-Dialog.png` was visually inspected.

The model checks cover atomic edits, unchanged child positions, updated preview
geometry, exact source restoration through undo, locks, explicit replacement of
bindings and other layouts, invalid dimensions, and nested or duplicate layout
properties. UI checks exercise the actual group menu, dialog submission,
undo/redo, invalid zero dimensions, full integer spacing, and binding protection.

The dialog screenshot is an application-generated render. No original skin or
MediaPortal installation file is modified by these tests. The earlier original
assembly comparison is recorded separately in `../Grid-Preview`; no new native
client comparison is claimed for this milestone.

Reproduce from the repository root:

```powershell
.\build.ps1
# Use an absolute writable output directory for both runs.
.\MP1SkinStudio.exe --selftest C:\TestOutput\Grid-Dialog
.\MP1SkinStudio.exe --ui-smoketest C:\TestOutput\Grid-Dialog
```

The first UI run exposed a test harness issue: rendering a screenshot hides the
main form, so subsequent simulated button clicks did not execute. The harness
now shows and activates the form before continuing with the grid dialog checks.
