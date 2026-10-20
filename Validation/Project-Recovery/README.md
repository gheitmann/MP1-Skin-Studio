# Saved project recovery against matching skin backups

Executed October 20, 2026. Open Project now routes unavailable saved roots and
modified-source conflicts to a single matching-backup folder selection. It can
recover while the old root still exists. A typed source-conflict exception keeps
archive corruption and unrelated errors outside this path. Validation is unchanged:
no hash bypass, snapshot extraction, source overwrite, automatic merge or retry loop.

The full replacement project is prepared before publication. Cancel or rejection
preserves the active session. A relocated session marks its location unsaved;
Undo, export and failed saves cannot clear that marker. A successful project save
persists the new root, and the next open no longer requests a folder. Loading a
new skin resets the marker as part of its normal baseline.

## Fresh results

- 394 core checks, zero failures. Five new cases cover existing-root recovery,
  restored changes/history and new-root persistence, cancellation, wrong backup,
  missing-root recovery and corruption/healthy-path routing.
- Six synthetic Windows-form workflows on complete isolated Titan copies:
  BasicHome.xml, myvideo.xml and SettingsGUI.xml in Default and Christmas. Each
  changes three XML files, intentionally drifts one on disk, then verifies cancel
  and wrong-backup preservation of the active project and property draft. Matching
  recovery restores all three changes and one combined Undo/Redo step. The tests
  exercise the actual form recovery/install/save handlers and Undo/Redo buttons.
- All 18 exported XML payloads match the recovered in-memory bytes exactly.
  Locked project saves preserve the archive and location marker; successful saves
  persist the backup root and reopen without another folder request. Loading a
  new skin starts clean.
- All 2,050 files match across the original test baseline and each complete skin
  copy after restoring the intentional drift in the disposable source fixture.
  The matching backup is never written. No installed original skin is edited.
- The extracted review package passed twelve full demo edit/save/reopen/exact
  export/normal and restored Undo/Redo workflows, plus twelve clean-session
  workflows; all 30 manifest payload hashes remain unchanged.

No attended desktop input, native folder-dialog selection or installed-client run
was performed in this milestone. Synthetic callbacks test chooser outcomes; the
broad historical UI/client acceptance was not repeated. Only modified XML original
bytes are matched by project loading; a complete appropriate backup still matters
for unchanged dependencies, themes, images and fonts. This is not backup provenance
certification, external-edit merging or a runtime rendering acceptance.

The core suite ran before a caption-only encoding correction; the final executable
passed the six recovery and twenty-four packaged demo/session workflows afterward.

## Reproduce

Build with build.ps1, then run:

```powershell
.\Validation\Project-Recovery\Run-Probe.ps1 -SkinDirectory C:\Skins\Titan -OutputDirectory C:\Review\Project-Recovery
```

The probe creates two complete skin copies beneath a fresh output directory and
mutates only its disposable source fixture, restoring drift in a finally block.
Its original argument is read-only. It copies the current executable and compiles
the test host with the Windows .NET Framework compiler. Core checks use --selftest
with an absolute output directory. Package regression uses
Validation/Package-Readiness/Run-Probes.ps1 -SkipFullUi and a fresh output directory.

Core-Results.txt, Recovery-Results.txt, Package-Results.txt and Session-Results.txt
record this milestone. An initial probe harness failed at its final public-method
lookup; the reflection flags were corrected and all six workflows rerun in a fresh
directory. No application failure or additional desktop input was involved.
Final packaging refreshes revision notes and independently verifies all payload hashes.
