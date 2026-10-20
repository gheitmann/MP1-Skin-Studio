# Extracted review-package acceptance

Executed on October 20, 2026 using the editor and Samples from an extracted ZIP,
without an installed Titan skin, language pack or MediaPortal client.

Fresh results:

- **204 automated Windows-form UI checks passed**: property Apply, binding
  protection, move/resize, navigation, project opening/saving, failed-open
  preservation, textures, viewport/locks/hiding, audit/reset, samples, menus,
  structural editing, searches/overlaps, grouping/ungrouping, text/languages,
  stack/grid layout and collections.
- **Twelve complete demo workflows passed**: clean load, relevant preview
  navigation, width edit, save/reopen, matching project signature/focus, exact
  changed XML export, normal and restored Undo/Redo.
- **Twelve clean-session workflows passed**: open without an unsaved marker,
  zoom marks unsaved, Fit restores the exact original signature, save/reopen
  remains clean and retains initial focus. This includes pages with no explicit
  defaultcontrol and pages with no focusable control.
- **All 29 payload hashes unchanged** before/after matrix and full UI acceptance.
  This includes the executable, guide, license and all bundled XML/assets.

No new attended keyboard session or installed-client run occurred. These are
automated WinForms event/workflow tests, not a live MP1 engine test. The previous
370 core/model checks and real Titan matrices were not repeated for this form
initialization change and remain historical.

## Product correction

InstallProject previously captured the clean baseline before RefreshProject
selected an initial control. On pages without defaultcontrol, selecting the
fallback button added focus metadata after the baseline. A newly opened skin
could therefore immediately show an unsaved asterisk, and Fit could not restore
the expected clean state even though XML was unchanged.

The baseline is now captured after initial selection, for both skin and project
opening. Subsequent focus, viewport and XML edits retain their normal detection.
The existing project/save/undo and viewport checks passed, together with the
new initial-state regression and twelve independent session cases.

The broad suite also exposed an older overlap-menu assertion that compared the
entire project signature while selecting a different focusable control. Since
focus is intentionally persisted, the assertion now verifies unchanged XML,
read-only properties, filtered tree behavior and the correct simulated focus.
No selection behavior was changed for that test correction.

## Package provenance and limits

Final UI and matrix runs used the package-readiness-final candidate ZIP. Final
README/package metadata and guide updates were made after acceptance; the
executable and Samples are byte-identical to the accepted candidate. Packaging
independently verifies the final payload manifest. Acceptance requires no writes
to installed skins or client configuration. Generated projects, exports,
screenshots and intermediate failures remain local working files, not payload.

The twelve full pages are demo.xml, demo.checkmark.xml, demo.collections.xml,
demo.filmstrip.xml, demo.free.xml, demo.grid.xml, demo.groups.xml, demo.images.xml,
demo.language.xml, demo.layers.xml, demo.navigation.xml and demo.stack.xml.
demo.menu.xml and common XML files are includes, not standalone scenes. Deliberate
unknown checkmark selection and optional language/runtime samples remain
unverified; clean loading is not a claim that every runtime value is known.

## Reproduce

Use a fresh output directory. The helper extracts the complete ZIP, compiles
the three hosts against that executable, then runs the matrix, clean-session
checks and broad UI suite. The UI host creates its own offscreen form and
synthetic events; some test dialogs may briefly appear. It sends no desktop
keyboard/mouse input. Allow the complete suite to finish before using its report.

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 `
  -PackageZip C:\Review\MP1-Skin-Studio-Review.zip `
  -OutputDirectory C:\Review\Package-Acceptance
```

Matrix-Results.txt, Readiness-Results.txt and UI-Results.txt contain the fresh
successful reports. UiHost.cs invokes the production in-process UI regression
suite; MatrixProbe.cs and ReadinessProbe.cs add independent package-level checks.

## Proportionate follow-up gate

Add -SkipFullUi to run the twelve demo workflows and clean-session checks
without repeating the broad synthetic UI suite. Add -IncludeCore when a fresh
core result is required. Existing calls retain their full-UI behavior. The
October 20 combined -SkipFullUi -IncludeCore run passed with the current
executable; see Validation/Release-Acceptance for its reports and attended
client comparison. Use a fresh output directory for each run.


## Startup and final preservation gate

Add -IncludeStartup to include the eight synthetic startup/recovery scenarios
from Validation/Startup-Acceptance in the same run. Keep -SkipFullUi for routine
changes whose relevant broad UI checks are already accepted. The helper now also
captures the extracted manifest before testing and compares every payload hash
after all selected probes; Payload-Results.txt records the final result. These
options preserve the existing default broad-UI behavior. The startup-integration
milestone records its own fresh broad UI, demo/session and native-startup results.

Add -IncludeTransitions to run the 18 targeted close/session-switch scenarios
from Validation/Session-Transitions before the final payload-preservation gate.
Combine with -SkipFullUi -IncludeStartup for the focused dialog milestone gate.
