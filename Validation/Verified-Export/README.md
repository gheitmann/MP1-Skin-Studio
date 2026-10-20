# Verified XML exports and source conflicts

Executed October 20, 2026. SkinProject.Export now delegates to SkinExport:

1. Prepare and validate the changed XML bytes while retaining original bytes.
2. Compare each modified physical source with its loaded original length/hash.
3. Write changed XML plus the existing instructions entry to a temporary ZIP.
4. Read back every ZIP entry and compare its exact bytes and entry count.
5. Recheck modified physical sources, then atomically replace/move the ZIP.

Conflicts, validation or write failures preserve previous output and pending
editor changes. The instructions include relative filenames, original/exported
lengths and SHA-256 hashes, page/theme and shared-include scope. No additional
ZIP entry, machine root path or preview samples are introduced. Hashes identify
exact bytes including encoding/BOM, not authenticity or runtime compatibility.

Only modified sources are checked. Unmodified dependencies and later external
changes are outside the gate; compare destination hashes before applying to a
complete separate runtime skin copy. Save a recovery project if source drift
blocks export. That project retains the old original snapshot; it does not merge
the external source or bypass matching-original requirements when reopened.

## Fresh results

- 389 core checks passed, zero failed. Six new regression checks cover exact
  identities, externally changed sources, missing sources, preserved old ZIP/
  pending state/history, recovery project save, restored-original export and
  Undo/Redo, UTF-16/BOM bytes, and the explicitly bounded modified-source scope.
  Existing locked-destination export tests also passed.
- Six complete real Titan workflows passed: BasicHome, myvideo and SettingsGUI
  in Default and Christmas. Each edits three files in one transaction, saves
  and reopens a project, exports exact XML bytes, checks original/export hashes
  and context, and verifies normal/restored Undo/Redo. All 18 XML payload checks
  and all 45 distinct loaded original source hashes passed.
- The extracted review package passed all twelve complete demo edit/save/reopen/
  exact-export/normal and restored Undo/Redo workflows, plus twelve clean-session
  workflows. All 30 payload hashes remained unchanged during the matrix.

No new attended desktop or installed-client run occurred. Existing Windows,
reporting and installed-client acceptance reports remain historical. This change
affects export preparation/preservation and its instructions, not rendering or
runtime actions. The broad UI suite was not repeated for this model change.

## Reproduce

Build normally. Compile TitanProbe.cs against the built executable with the
Windows .NET Framework compiler and System.Drawing, System.Windows.Forms,
System.IO.Compression and System.IO.Compression.FileSystem references. Put the
probe beside MP1SkinStudio.exe, then run:

```powershell
.\TitanProbe.exe C:\Skins\Titan C:\Review\Verified-Export
```

The second argument is a separate report/project/export directory. The probe
never applies an export to the source skin. Run --selftest with an absolute
output directory for the core regressions. For package acceptance, use
Validation/Package-Readiness/Run-Probes.ps1 -SkipFullUi with a fresh output
directory. Core-Results.txt, Titan-Results.txt, Package-Results.txt and
Session-Results.txt record the executed checks. Final packaging refreshes source
revision/notes and independently verifies the payload manifest.
