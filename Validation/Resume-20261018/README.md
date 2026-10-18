# Resume checkpoint: October 18, 2026

## Source and build

- Resumed from private GitHub main at commit 4d7764d553c2c4e14ad4d3e8c1c4b21eb038a0a3.
- Verified all 125 tracked baseline files against their Git blob hashes. No differences remained before validation.
- A fresh Windows .NET Framework build succeeded. No editor source changes were made.
- Fresh core validation is recorded in Core-Tests.txt. Prior UI and native-layout reports remain historical and were not rerun in this checkpoint.

## One window-control test, then stop

The installed, already running MediaPortal window was enumerated as MediaPortal - Hauptmenü. Activation succeeded and Titan Home was visually observed. This does not establish persistent authorization or a completed client comparison.

During this test the desktop log recorded three new allow/persist=always replies at 18:04:45, 18:04:57 and 18:05:08 UTC, followed by allow/persist=session at 18:05:40 UTC (20:04–20:05 Europe/Berlin). These are frontend approval replies, not proof of backend persistence; each reply has not been independently correlated to the target. Together with the repeated prompts, persistent window access cannot be treated as working.

Once the recurrence was identified, all Computer Use calls were stopped, including capture and activation. Do not retry app approval or window control in this continuation. The owner's stop instruction takes precedence over normal recovery/retry guidance.

Upstream report: https://github.com/openai/codex/issues/55114

## Pending work

The installed Titan Settings rendering, focus/navigation and exported spacing comparison remain pending. No original skin or client configuration was changed. The DirectX dependency replacement task remains explicitly deferred; no investigation or implementation was performed.

Continue source-only development independently when appropriate. Resume interactive client comparison only after a supported resolution or a new explicit instruction from the owner. No desktop screenshots, personal runtime logs or private configuration are included in this commit.
