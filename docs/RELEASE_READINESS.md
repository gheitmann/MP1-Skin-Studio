# Review candidate readiness

Date: October 20, 2026. Version: 0.9. Delivery: portable Windows review ZIP.
The ZIP includes the executable, twelve complete demo scenes, their shared
resources, license, user guide and quick start. Extract it completely before
running. Windows and .NET Framework 4.x are required; no compiler is needed.

## Accepted practical scope

Visual property editing, source preservation, pending-field guards, project
save/reopen, matching-backup recovery, XML ZIP export, exact-byte export checks,
Undo/Redo and explicit preview state are available. Project-file startup retains
a usable demo after cancel or a load error. Closing/session switching offers
Save project, Discard changes and Cancel; canceled/failed saves retain work.
Pending property inputs remain guarded. Exports contain modified XML and
instructions; they are never automatically installed into a client skin.

The candidate is suitable for reviewing these workflows against isolated copies.
It is not a complete MediaPortal renderer or an unrestricted compatibility claim.

## Evidence and remaining boundaries

The previous attended review covered the executable from source 07e8ef2:
open a real saved Video project, change its default list width to 590 in the
Windows editor, Apply, Save Project, reopen the archive in a separate process
and export exact XML bytes. A complete isolated runtime copy loaded the new
header and narrower list in MP1. Left/Right menu navigation passed. Titan was
restored, and all 2,050 installed original hashes stayed unchanged.

This accepts the bounded first-use workflow for the tested Default-theme
Home/Video sample. It is not general release publication or proof of every skin.
For another intended edit, page, theme or plugin, use the review procedure below
and obtain the owner's final visual acceptance of that actual result.

The current XML-review milestone adds a read-only session-wide file list,
original/edited text sections, exact byte hashes and refreshable source checks.
Fresh extracted-package acceptance covers 402 core checks (eight new review
regressions), ten targeted synthetic review-form scenarios, twelve complete demo
workflows and twelve clean-session workflows. All 31 payload hashes stay
unchanged after the run. Form/menu/shortcut events are synthetic, with no new
attended desktop input or installed-client claim.

Previous 204 broad UI, 18 close/session-switch and eight startup scenarios remain
historical. The matching-backup milestone's six real-Titan workflows also remain
historical. XML review does not change renderer/export/model editing behavior.

Preview masks/animations, plugin-dependent values, playback and some control
interactions remain approximate or unverified. Slider/spin value interaction is
not simulated. Read the user guide for per-feature bounds. Hash matching covers
modified XML originals; retain complete matching resources for a recovery skin.

## Review procedure

1. Follow QUICK_START.md with the bundled demo and a new project file.
2. Review the intended real skin only on a complete separate copy.
3. Save the project, reopen it, inspect findings and Review XML changes, then export XML.
4. Compare the instructions' original SHA-256 values to the runtime copy before
   applying; compare exported values afterward. Shared includes affect pages.
5. Check the intended page, theme, focus and navigation in the client, then restore
   the original skin. Keep the project, source backup and exported ZIP together.

No release publication, automatic installation or skin activation is performed
by this ZIP. GitHub main is the source checkpoint; README-Review.txt in the ZIP
identifies its source revision and MANIFEST.sha256 identifies payload bytes.
