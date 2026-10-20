# Save before session transitions

Date: October 20, 2026. Parent source checkpoint: 971f43c.

## Change

Closing or switching sessions offers Save project / Discard changes / Cancel
for unsaved XML and editor state. Save continues only after successful persistence
and a clean session. Canceling Save As or a failed/locked save preserves the
active session, history and existing archive. Discard permits the transition
without destroying active state before the replacement loads. Enter/Escape
select Cancel. Clean sessions do not prompt.

Pending fields continue to block session switching. The close dialog disables
Save, explains Apply/Revert and offers Cancel or explicit Discard. Preview-only
state and relocated roots use the existing unsaved-state detection.

## Fresh extracted-package evidence

- 18 targeted synthetic Windows-form scenarios passed, including actual owned
  decision dialogs, button events, CanSwitch and FormClosing decisions.
- Twelve complete demo edit/preview/save/reopen/exact-export/normal and restored
  Undo/Redo workflows passed.
- Twelve clean-session/zoom/Fit/save/reopen workflows passed.
- Eight queued startup/recovery workflows passed.
- All 31 manifest payload hashes stayed unchanged after all selected probes.
  Each form probe also checked all 26 bundled demo source/asset hashes.

Actual existing-file Save is exercised. Save As cancellation is an injected
return value; native file-picker input is not claimed. Locked saving uses a real
exclusive file lock, verifies the exception and byte-identical old archive, and
checks retained unsaved work/history. These are synthetic Windows application
tests, without desktop input or installed-client execution.

Executable SHA-256: `a6076e6808bc8c4019c30fe8bcb10f23eee8fc2e1914b195dc3932e6435d538e`.
Accepted candidate ZIP SHA-256:
`696783d1de15eb3a5c0a97faf871055dbdefd7790b7d54fa6080761c9208a357`.
The final ZIP changes only its revision note; all 31 payload hashes must match
this candidate's manifest. Reports in this folder are from that candidate.

## Reproduce

From the source root, build/package normally, then choose a fresh output path:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 `
  -PackageZip C:\review\MP1-Skin-Studio-Review.zip `
  -OutputDirectory C:\review\transition-checks `
  -SkipFullUi -IncludeStartup -IncludeTransitions
```

Run-Probe.ps1 can also run the transition probe alone with PackageDirectory and
a fresh OutputDirectory. It compiles its host against the chosen executable
and starts it hidden. Fixtures, projects and reports stay in that output folder;
no installed skin is edited.

## Historical evidence

The 204 broad UI and 394 core checks remain historical; model/render/export
logic and demo assets did not change in this milestone. The attended real-Titan
client checkpoint accepted source 07e8ef2 on Default Home/Video before this
dialog change. It is not a new runtime acceptance of the rebuilt executable.
Other pages/themes/plugins and final owner visual review retain the readiness
guide's existing limits.
