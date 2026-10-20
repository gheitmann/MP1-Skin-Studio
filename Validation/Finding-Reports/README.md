# Saved audit and preview findings

October 20, 2026: both finding dialogs now save their displayed filter as UTF-8
text or a self-contained HTML page. Source locations, codes, details and
page/theme/filter/total counts are retained in displayed order. HTML encodes
XML-derived content and uses no external assets or scripts. Reports explicitly
identify their open-page scope and snapshot nature, including empty filters.
The shared filter implementation prevents dialog/export disagreement.

Reports replace an existing destination only after complete temporary output
was written. Failed replacement preserves the previous report and removes the
temporary file. Only .txt/.html are accepted; XML, projects and ZIP destinations
are rejected before writing. Reporting does not change editor state or skin XML.

## Fresh validation

- 383 core checks passed, including include source/line preservation, report
  read-only behavior, HTML encoding, misleading empty-filter prevention,
  locked-destination replacement failure and rejected source/project paths.
- 144 saved dialog snapshots passed across BasicHome, myvideo and SettingsGUI,
  Default and Christmas, audit and preview modes, text and HTML, both information
  toggles and all/code/no-match searches. Minimum-size button bounds and retained
  finding navigation passed. All 45 loaded original XML hashes stayed unchanged.
- Final packaged executable: twelve complete demo edit/save/reopen/exact-export/
  normal and restored Undo/Redo workflows, plus twelve clean-open/zoom/Fit/save/
  reopen workflows passed. All 30 payload hashes stayed unchanged in the matrix.
- Attended Windows: fresh editor opened cleanly; Audit displayed the new action;
  Save shown report opened the native dialog with both format choices. Cancellation
  returned to the unchanged editor with zero modified files and no unsaved marker.
  The helper could not reliably identify filename focus, so attended native file
  submission was not claimed. Actual file writing is covered by the 144 synthetic
  dialog workflows and core failure checks.

Core and targeted real-page checks preceded the final filename-default-only
adjustment. The final packaged executable passed the matrix/session checks and
attended open/cancel test. The prior broad 204 UI checks and installed-client
acceptance remain historical and were not repeated for this reporting feature.
No rendering, export XML or runtime navigation behavior changed.

## Reproduce

Build normally, then use a fresh output directory:

```powershell
.\Validation\Finding-Reports\Run-Probe.ps1 -SkinDirectory C:\Skins\Titan -OutputDirectory C:\Review\Finding-Reports
```

ReportProbe creates its own forms and synthetic calls without desktop input or
client configuration changes. It writes generated reports only under its output
directory. Core-Results.txt, Titan-Results.txt, Package-Results.txt and
Session-Results.txt record the executed validation. The review ZIP contains the
executable, samples and guides; generated real-skin reports are not bundled.
