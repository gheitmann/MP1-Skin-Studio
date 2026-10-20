# Rendering and large-page loading

Date: October 20, 2026. Baseline: source 62560cb.

DefaultWideHD measurements use myHome.xml (262 expanded controls), BasicHome.xml
(1,237) and MyMinesweeper.xml (507). Fresh models and complete form loads run
three times; cached-source loads and warm canvas paints run seven times. Reports
retain median and maximum milliseconds. "Fresh" means a new project/source
cache, not an empty Windows filesystem cache. The final baseline and optimized
timing runs were sequential, before the broad UI host started. A subsequent
baseline replay investigates pixel repeatability only; its concurrent timings
are excluded from the comparison.

## Changes supported by profiling

The XML scanner previously searched the remaining file for each comment, CDATA
and processing-instruction opener at every tag, then checked whether the match
started at the current offset. Missing or distant markers made this quadratic.
It now compares only the current position. The existing validated XML reader,
source spans, original bytes, encoding/BOM and patch/export paths are unchanged.
Closing markers still search once through their own block.

The canvas previously evaluated the same top-level visibility repeatedly during
layout, filtering and drawing. A scoped pass now retains completed results by
control occurrence until that paint or pointer inspection ends. Recursive calls
still use their original cycle detection and work budget; results are never
reused across recursive contexts. finally/Dispose release the pass before
mutation callbacks or the next frame. Direct visibility queries remain fresh.
There is no persistent preview cache to invalidate after page/focus/sample/XML
changes. Two exploratory image changes (identity color-matrix bypass and decoded
premultiplied bitmaps) showed no useful improvement and were discarded.

## Serial measurements

| Operation | Page | Baseline ms | Optimized ms |
| --- | --- | ---: | ---: |
| Fresh model | myHome | 378.15 | 14.54 |
| Complete form load | myHome | 494.35 | 138.94 |
| Warm canvas paint | myHome | 131.23 | 103.37 |
| Fresh model | BasicHome | 1800.43 | 152.22 |
| Complete form load | BasicHome | 3120.84 | 1535.96 |
| Warm canvas paint | BasicHome | 444.93 | 294.64 |
| Fresh model | MyMinesweeper | 1179.23 | 28.62 |
| Complete form load | MyMinesweeper | 1321.61 | 266.78 |
| Warm canvas paint | MyMinesweeper | 85.48 | 68.44 |

These are local synthetic Windows Forms operations, not desktop input-to-display
latency or guaranteed results on other skins/hardware. Rendering uses a requested
1000 by 700 bitmap and the normal form/canvas layout. Individual maxima and
cached-source model timings are retained in the raw reports. XML loading improves
strongly; BasicHome still needs about 1.5 seconds for complete form preparation
and about 0.3 seconds per canvas redraw. Native tree work, complex conditions and
GDI+ image scaling remain worthwhile future profiling targets.

## Independent checks

The probe fingerprints exact XML bytes and every element's source offsets,
control ordinal and DOM serialization for all 488 installed XML files. Baseline
and optimized fingerprints are identical. The nine canvas comparisons include
three pages, changed focus/sample state and diagnostic All layers. See the
comparison/replay reports for exact pixel results and any repeatability limits.
Installed XML hashes are checked before/after both probe runs.

Three added core cases cover recursive/unknown visibility and nested scopes,
exception cleanup plus subsequent focus/sample/XML/Undo changes, and comments,
CDATA, PIs, quoted tag delimiters and a final empty property. The full core report
is retained alongside current package/UI/session reports. The first standalone
core invocation used an unnormalized path containing '..', causing three
pre-existing fixture path-equality assertions to fail; all three new tests passed.
The canonical-path rerun passed all 414 tests. No product change was made for
that harness error. Historical installed-client checks were not rerun.

```powershell
.\Validation\Render-Load\Run-Probe.ps1 -PackageDirectory C:\ReviewPackage `
  -OutputDirectory C:\FreshOutput -SkinDirectory C:\DefaultWideHD
```

Build Compare.cs against System.Drawing/System.Core and run it with the baseline
and optimized output directories to compare all mappings and nine saved renders.

## Pixel repeatability and executable identity

The first serial capture matched eight of nine images exactly. BasicHome All
layers differed at five pixels within x=597..654, y=94..97, with a largest channel
delta of 45. Repeating the unchanged 62560cb baseline produced that same five-pixel
difference from its first capture, and matched all nine optimized images exactly.
Comparison-Results.txt uses this baseline replay. The variation is therefore
observable without the optimization; no wider pixel tolerance was introduced.
The probe's standalone repeat renders also matched across the old/new binaries.
No image-quality, interpolation, color-matrix or texture-decoding change shipped.

Baseline executable SHA256:
6292bd2085a462a0a82f68f4fe531a070739162dca06bfc5dc77840eb7e02264

Optimized executable SHA256:
40c21d84bfbd1ab5c8260e3b8337427020db029c9c6a3d8826b6150af9c530ea

Core checks ran once with a canonical output directory against that exact
optimized executable; the extracted package retains the same executable bytes.
Release documentation and revision metadata are refreshed for final delivery.

## Final acceptance

All 414 core tests passed against the exact executable subsequently packaged.
Fresh extracted-package checks then passed 205 broad UI assertions, ten real
DefaultWideHD readability scenarios, twelve complete edit/save/reopen/export/
Undo/Redo demo workflows and twelve clean-session workflows. All 488 installed
XML, 26 demo source/assets and 31 package payload hashes stayed unchanged.
The core suite was not redundantly repeated by the package runner. Tests use
synthetic forms/events; no new attended installed-client acceptance is claimed.
