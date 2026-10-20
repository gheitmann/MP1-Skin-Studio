# Review candidate readiness

Date: October 20, 2026. Version: 0.9. Delivery: portable Windows review ZIP.
The ZIP includes the executable, twelve complete demo scenes, their shared
resources, license, user guide and quick start. Extract it completely before
running. Windows and .NET Framework 4.x are required; no compiler is needed.

## Accepted practical scope

Visual property editing, source preservation, pending-field guards, project
save/reopen, matching-backup recovery, XML ZIP export, exact-byte export checks,
Undo/Redo and explicit preview state are available. Project-file startup retains
a usable demo after cancel or a load error. Exports contain modified XML and
instructions; they are never automatically installed into a client skin.

The candidate is suitable for reviewing these workflows against isolated copies.
It is not a complete MediaPortal renderer or an unrestricted compatibility claim.

## Evidence and remaining boundaries

The current integration gate exercises the broad synthetic Windows-form suite,
all twelve demo edit/save/reopen/export workflows, twelve clean-session workflows
and eight project-startup success/failure/recovery scenarios. The validation
record in the repository distinguishes newly executed checks from earlier core,
real-Titan, attended Windows and installed-client results.

The prior core milestone passed 394 checks. The prior matching-backup milestone
passed six complete real-Titan form workflows and 18 exact XML payload checks;
all 2,050 files in its baseline and each test copy matched after fixture cleanup.
Those results remain historical rather than being counted again as fresh tests.

Remaining acceptance before a general release: perform the owner's final visual
review on the intended skin/pages and confirm client behavior for that actual
export. The earlier isolated-client Video comparison covers its recorded sample
and source revision. It does not prove every current edit, theme or plugin.

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
