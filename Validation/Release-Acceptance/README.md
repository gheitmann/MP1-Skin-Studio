# Integrated Windows and runtime acceptance

Executed October 20, 2026 with the executable from source a885253.
Executable SHA256: 45eff11d5a7a478b278ce98f9b9951ce57ec61c9f861c052843814e3899a5ef3.
No editor engine code changed in this milestone.

## Fresh attended Windows checks

The packaged editor opened cleanly without an unsaved marker. Selecting
 demo.navigation.xml and enabling Navigation produced Movies -> Pictures ->
Settings -> Music -> Movies through Right, Down, Left, Up. Selection, focused
texture, property identity and status text followed all four movements. The
page/focus state became unsaved while modified XML remained zero. An older
editor window was preserved.

Installed MediaPortal opened with Titan. The original Video page showed its
Videos folder and count 1. The existing RuntimeVideoProbe was compiled against
the current packaged executable and generated a fresh project/export: list
width 698 -> 578 and shared header STUDIO EXPORT TEST. Exactly two XML files
and instructions were exported; exported XML matched the project and all 17
loaded original source hashes stayed unchanged.

Only those two exported XML files were overlaid on a fresh complete skin copy,
SkinStudio-Acceptance-20261020. Selecting it via Settings > GUI > Skin loaded the
copy. The header marker appeared on Home and Video; the Video focus band was
visibly shorter. Left opened the side menu and Right returned to the list.
Titan was selected again through the same UI and its normal Home title returned.
A full SHA256 comparison found zero changes in all 2,050 original Titan files.
The isolated copy differs from Titan in exactly common.time.xml and
common.facade.video.default.xml. No playback or plugin activation was tested.
This is sampled export acceptance, not complete visual/runtime equivalence.

The first launch helper call returned before the client window was targetable;
a fresh inventory recovered the running client without a second launch. One
editor dropdown accessibility click failed because the cached menu element
was unavailable; a fresh screenshot and coordinate selection succeeded.

## Fresh package checks

- 378 core checks passed, zero failed.
- Twelve complete demo edit/save/reopen/exact-export/normal and restored
  Undo/Redo workflows passed.
- Twelve clean-open/zoom/Fit/save/reopen workflows passed.
- All 30 payload hashes stayed unchanged through the matrix.
- The previous 204 broad UI checks and real-page matrices remain historical.
  They were not repeated for documentation/runner changes.

The accepted candidate includes the new Quick start. Final package notes and
source-revision metadata are refreshed after acceptance; executable and Samples
remain identical. Final packaging independently verifies its payload manifest.
No installed skin, client configuration, project, screenshot or private log is
included in the review ZIP.

## Reproduce the proportionate gate

From a source checkout, run the existing package acceptance helper with a
fresh output directory:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 -PackageZip C:\Review\MP1-Skin-Studio-Review.zip -OutputDirectory C:\Review\Acceptance -SkipFullUi -IncludeCore
```

Omit -SkipFullUi only when changed Windows event/dialog behavior justifies the
broad suite. Omit -IncludeCore when the unchanged engine already has a passing
core report. The helper always checks manifest/demo workflows and clean
sessions. It creates its own forms and sends no desktop keyboard/mouse input.
Installed-client acceptance is attended and separate. RuntimeVideoProbe is
preserved in Validation/Video-Runtime-20261019/Probe.cs; it exports without
writing source skin files. Apply only to a separate complete runtime skin copy.
