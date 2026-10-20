# Editor responsiveness

Date: October 20, 2026. Baseline source: 604d666.

The profile uses the actual DefaultWideHD myHome.xml (262 expanded controls)
and MediaTipOverlay.xml (42). Each operation runs seven times; reports retain
the median and maximum in milliseconds. These are synthetic Windows Forms
measurements in the same local Windows session, not end-to-end desktop latency
or frame-rate guarantees. Existing owner sessions remain open. The baseline
binary is the exact accepted readable-preview executable.

The dominant selection cost was updating every native tree label and color,
even when unchanged. Tree updates now suspend drawing and replace only changed
values. Building a filtered tree assembles each source branch before attaching
it to the native tree. Diagnostics count hidden and unresolved results in one
pass instead of evaluating each control twice.

Pointer selection previously rebuilt a complete StackPreview for every BoundsOf
lookup. Left-button hit testing, navigation connection targets and overlap menus
now share one measured layout within each inspection. The scope ends before
selection callbacks can change focus, samples or XML, and restores the previous
measurement in finally. Nothing is cached across input events or frames.
Rendering and condition evaluation continue to read current preview state.

Run against an extracted package and a real DefaultWideHD directory:

```powershell
.\Validation\Responsiveness\Run-Probe.ps1 -PackageDirectory C:\ReviewPackage `
  -OutputDirectory C:\FreshProbeDirectory -SkinDirectory C:\DefaultWideHD
```

The probe compares optimized overlap results with independent uncached bounds
at three points on both pages, initially and after focus, sample, XML geometry
and Undo changes. It checks front-to-back ordering, deduplication and scope
cleanup, and verifies all 488 installed XML hashes. Geometry edits remain in
memory and are undone; the probe never writes to installed skin files.

See Baseline-Results.txt and Optimized-Results.txt for timings, and the retained
package/core/UI/session reports for behavior checks. The older readable-preview
reports describe the preceding milestone; they are not substituted for current
package acceptance. Installed-client rendering was not repeated for this change.

Canvas painting remains a measurable cost, roughly 0.1 seconds on myHome in the
initial profile. Texture loading, text rendering and larger XML loads remain
future profiling candidates. This milestone improves selection and picking;
it does not claim that every editor action is instantaneous.

The first broad run stopped at a historical assertion that unknown conditions
were drawn by default (173 checks had passed). Edit layout intentionally hides
those conditions since the preceding readability milestone. LanguageTests now
checks the clean default and explicitly enables diagnostic layers before checking
unverified content. No product behavior was changed to satisfy this assertion.
Initial-UI-Error.txt retains that failure. The final reports identify the complete
rerun separately.

## Final serial comparison and acceptance

The final timing runs ran sequentially after the broad acceptance host exited.
Earlier exploratory timings taken during acceptance were discarded for this table.

| myHome operation | Baseline median ms | Optimized median ms |
| --- | ---: | ---: |
| Select element | 338.72 | 31.44 |
| Update tree preview | 317.85 | 19.29 |
| Inspect overlapping elements | 89.14 | 13.83 |
| Rebuild element tree | 99.98 | 72.20 |
| Diagnostics | 10.60 | 5.50 |
| Paint canvas | 106.05 | 105.45 |

Selection improved by about 91%, overlap inspection by about 84% in this fixture.
MediaTip selection changed from 13.00 to 5.58 ms; painting stayed near 17 ms.
These figures measure specific operations, not overall project completion or
universal speedup. Individual maxima and the complete second-page measurements
are retained in the raw reports.

The final extracted candidate passed 411 core tests, 205 broad UI assertions,
ten readable-preview scenarios, twelve complete demo workflows and twelve
clean-session workflows. All 488 installed XML, 26 demo assets and 31 payload
hashes stayed unchanged. Both baseline and optimized independent hit-equivalence
probes also passed initial/focus/sample/geometry/Undo checks at three points on
both pages, without touching installed XML.

Accepted executable SHA256:
6292bd2085a462a0a82f68f4fe531a070739162dca06bfc5dc77840eb7e02264

The final delivery retains these exact executable and sample bytes. Release
notes/readiness documentation and revision metadata are refreshed after testing;
the final ZIP manifest is checked independently. The new broad count includes
one additional clean-default assertion; the corrected diagnostic assertion is
retained. Historical real-client acceptance was not rerun for this optimization.
