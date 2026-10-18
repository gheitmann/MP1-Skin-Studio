# Model loading, atomic batches and a manual review build

Date: October 19, 2026, Europe/Berlin. Baseline: 0e2ea541f68e632e6c16999784e1257b5da58546.

## Avoid repeated source scans

Loading each control previously searched preceding source elements to determine
its ordinal. This repeated a growing XML-prefix scan for every control. The
parser now assigns ControlOrdinal once in XML preorder; model loading uses that
cached ordinal. Nested controls, duplicate runtime include occurrences and
structural identity remapping retain the same source keys. XML is unchanged.

The bounded console benchmark uses 1,600 synthetic label controls, one warmup,
then five cached OpenPage calls. It checks all source ordinals on every load.

| Same-machine cached model load | Median |
| --- | ---: |
| Before | 101.75 ms |
| After | 4.61 ms |

These measurements are about 22 times faster for this specific cached model
operation. They are not a benchmark of full startup, file parsing, rendering,
plugin behavior or interactive GUI responsiveness. Before.txt and After.txt
retain all five samples. Probe.cs creates only its own scratch fixture.

## Preserve every compatible batch value

EditMany previously kept only the first entry for each source control. It now
validates every nonempty entry, merges compatible properties and rejects
conflicting values before mutation. Stale duplicate entries cannot evade
selection validation. Rejection retains existing XML, undo and redo history.
A failed cross-file transaction can also be followed by a corrected edit and
normal undo/redo, as verified by the extended regression.

## Fresh joint verification

The fresh build and core suite passed **237 checks, zero failures**. Four new
checks cover cached nested ordinals, compatible merging, conflict rejection with
redo preservation, and stale duplicate entries. Existing tests cover grouping,
removal, persistence, write failures and source mapping.

All **39 targeted console checks** passed against that executable: six geometry,
ten string-condition, eleven real complex-page and twelve prepared multi-page/
theme checks. The complex-page probe now submits its real Titan list changes as
two compatible entries for the same control and checks that all four properties
survive undo/redo, project persistence and export. Original files remain unchanged.
The reports in this directory are newly executed; earlier directories remain
historical checkpoints.

## Review archive

package-review.ps1 creates a ZIP containing the built editor, the repository's
small demo XML/images, license, user guide, review notes and SHA256 manifest.
Every packaged file is reopened and checked against its hash before publishing
the ZIP. No editor or client is launched. The script was exercised successfully
with the validated executable. The local final archive records its source commit.
Installed Titan assets, MediaPortal binaries/configuration, saved projects and
private runtime data are excluded. Local work output is now ignored by git.

The packaging script was also tested with an already existing ZIP and an
exclusively locked target. A PowerShell null-to-string conversion in the
replacement call was corrected using NullString.Value. Replacement now succeeds;
the intentional lock failure retains the prior ZIP and cleans the temporary
archive. These checks do not launch the packaged executable.

Recent GUI fields, pending-input guards and Revert Inputs remain manually
unverified. No editor GUI, MediaPortal, Computer Use or capture was launched.
The console core suite includes offscreen drawing tests; no UI smoke suite ran.
DirectX replacement and desktop automation remain deferred/suspended.
