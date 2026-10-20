# DefaultWideHD large-selector responsiveness

Date: October 20, 2026. Parent source checkpoint: 7629781.

## Reproduced cause and correction

The owner reported that DefaultWideHD made the editor almost unresponsive and
XML selection/expansion unusable. The older owner editor was observed on
ARGUS_UpcomingTv.xml. DefaultWideHD has 449 selectable top-level XML files
(451 including excluded metadata XML). Initial myHome model load took 455 ms;
the full initial form/load/render diagnostic completed in about 2 seconds.
That bounded observation did not reproduce a general model-load freeze.

The production ChoiceBox menu did reproduce severe blocking. Adding every item
triggered a layout; reopening also disposed every old item with repeated layout.
Baseline first/repeated opens measured 9,242 / 26,519 / 25,479 ms.

OwnerMenus.Prepare now suspends layout during clearing/disposal, and ChoiceBox
suspends it throughout population/checkmark setup, resuming once before showing.
Callbacks still capture original indexes; every file, current checkmark, menu
reuse/ownership, Escape and owner disposal retain their existing behavior.
No model editing, renderer, export or source-preservation logic changed.

## Fresh acceptance

- Seven targeted synthetic selector/form workflows passed against the extracted
  candidate, including the installed 449-file list. Each open used one layout,
  rather than a layout per insert/removal.
- Corrected opens measured 74 / 73 / 82 ms on this machine. These are local
  elapsed measurements, not hardware-independent latency guarantees.
- First/middle/last callbacks, exactly one selected check, cancellation, removed
  callbacks, ampersands and owner disposal passed.
- Actual form page-menu Home/Video/Settings workflows retained an XML edit
  across page changes, project save/reopen, export and restored Undo/Redo.
  Complete page actions measured 705 / 658 / 1,611 ms including menu/load/paint.
- Pending-field preservation exercised the production OpenSelectedPage method
  and selector restoration directly. Its native warning dialog was not driven.
- Existing menu integration passed all 12 assertions, including property reset,
  anchor rebuilding, insertion/alignment, protection, presets and nested loops.
- All 488 installed DefaultWideHD XML hashes and 26 demo hashes stayed unchanged.
- Twelve full demo workflows and twelve clean-session workflows passed first;
  the targeted selector host then completed separately after its DemoRoot test
  fixture setup was corrected. All used the same accepted executable. The final
  preservation check after both runs and attended use passed all 31 payloads.

Attended Windows then opened the corrected packaged editor separately, opened
the long list promptly, selected common.facade.music.xml, reopened/canceled the
list and collapsed/expanded its XML tree node. The previous owner session stayed
open. See Attended-Results.txt for the bounded observation and stale-menu target
limitation. The new editor is left open for review; no installed client was run.

Executable SHA-256: `e6da770d717eb84c108ee17a7fb2725d08599fe294188c7c2920f3e23a34feb7`.
Accepted ZIP SHA-256:
`5dc2f55c1c3d60e55a83d2be588d5265e34f6831a37c8dea7d7d99f0ba2c26d8`.
The final delivery changes only its revision note; all 31 accepted payload files
and their manifest must match. No installed skin resources are bundled.

## Reproduce

Use a fresh output directory and an installed complete DefaultWideHD root:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 `
  -PackageZip C:\review\MP1-Skin-Studio-Review.zip `
  -OutputDirectory C:\review\selector-checks -SkipFullUi `
  -LargeSelectorSkinDirectory 'C:\ProgramData\Team MediaPortal\MediaPortal\skin\DefaultWideHD'
```

Run-Probe.ps1 accepts PackageDirectory, SkinDirectory and a fresh OutputDirectory
to run only the targeted selector tests. The fixture requires over 400 selectable
files. Benchmark.cs can be compiled separately against before/after executables
and run with the skin root to time three production-menu opens. Neither helper
changes installed XML; generated project/ZIP fixtures stay in their output folder.

## Historical checks and remaining bounds

The preceding 402 core, ten XML-review, 18 transition, eight startup and 204 broad
UI suites remain historical. This milestone exercises the existing menu subset
because it directly shares the changed clearing path. Large-page rendering,
dynamic expressions and preview fidelity remain separate from menu latency.
An already running old editor does not receive the fix until the updated
executable is started. The attended test used the Computer Use skill.
