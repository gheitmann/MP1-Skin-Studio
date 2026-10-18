# Installed MP1 client comparison

Date: October 18, 2026. These observations supplement the historical runtime
materials; they do not establish a completed settings rendering comparison.

## Verified

- Codex CLI 0.163.0 is now on PATH and desktop window enumeration works.
- The user installed the legacy DirectX runtime and started the installed MP1.
- The installed MP1 1.39 Titan Home screen was visually observed.
- An independent prerequisite probe using the original Utils.dll returns
  `MediaPortal.Configuration.DirectXCheck.IsInstalled = True`.
- A fresh runtime export was generated from the installed Titan settings.xml.
- Exported source differs only in `StackLayout(7, Vertical, true)` becoming
  `StackLayout(15, Vertical, true)`. Child positions and all remaining XML text
  are identical. The baseline copy still matches the installed original by SHA256.
- No original skin XML was modified.

## Scope and blockers

An isolated runtime and two skin directories were prepared initially. The
isolated client stopped during startup; the complete cause was not established.
The separate DirectX probe is not proof that this client rendered successfully.
The user then explicitly requested using the installed client rather than the
program copy. Continue with that installation.

The installed client was closed normally during the earlier switch to the test
runtime. Subsequent attempts to launch either runtime through desktop automation
were rejected with `Computer Use was not approved to use mediaportal`.
This is the current access blocker; do not infer another missing DLL from it.
The user can start the installed client manually before another observation.

The following are still pending:

1. Open the installed Titan Settings page and compare its layout, text and focus
   against the editor using the same language and visibility state.
2. Exercise keyboard/remote navigation, including first/last item behavior.
3. Verify the exported spacing change in the client with a reversible skin change
   only after preserving the original. Do not change the live TV configuration.

The current repo remains at 200 core checks and 188 UI checks from the GridLayout
milestone, plus ten native grid checks. No new editor code was changed in this
client-validation checkpoint, so those tests were not rerun.

Private client configuration, complete personal runtime logs and the desktop
capture (which included an overlapping personal window) are not committed.
