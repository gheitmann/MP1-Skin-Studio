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

The current close/session-switch change is validated by 18 targeted synthetic
form scenarios, including real modal button and FormClosing events, plus twelve
complete demo workflows, twelve clean-session workflows and eight startup
scenarios against the extracted ZIP. All 31 payload hashes stay unchanged.
The native Save As cancellation outcome is injected; actual native file-picker
input and a new installed-client run are not claimed for this dialog-only change.

The prior broad gate passed 204 synthetic form checks; 394 core checks and six
real-Titan matching-backup workflows remain historical. Renderer/export/model
code and bundled demo assets were not changed by this milestone.

Preview masks/animations, plugin-dependent values, playback and some control
interactions remain approximate or unverified. Slider/spin value interaction is
not simulated. Read the user guide for per-feature bounds. Hash matching covers
modified XML originals; retain complete matching resources for a recovery skin.

## Review procedure

1. Follow QUICK_START.md with the bundled demo and a new project file.
2. Review the intended real skin only on a complete separate copy.
3. Save the project, reopen it, inspect findings and export XML.
4. Compare the instructions' original SHA-256 values to the runtime copy before
   applying; compare exported values afterward. Shared includes affect pages.
5. Check the intended page, theme, focus and navigation in the client, then restore
   the original skin. Keep the project, source backup and exported ZIP together.

No release publication, automatic installation or skin activation is performed
by this ZIP. GitHub main is the source checkpoint; README-Review.txt in the ZIP
identifies its source revision and MANIFEST.sha256 identifies payload bytes.
