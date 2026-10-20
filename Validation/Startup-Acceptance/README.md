# Integrated startup and package acceptance

Executed October 20, 2026. Project-file startup previously called the strict
resume method before Application.Run. A missing/drifted source could terminate
the launch without the matching-backup recovery available through Open Project.

Startup now queues the same recovery/install path after the editor's Shown event.
Native dialogs therefore have a visible owner. Cancel and load errors retain the
initial demo and a retry notice. A healthy load restores saved changes; relocation
retains its save marker. Showing the same form again cannot repeat the startup
action. The public ResumeProjectFile method also shares the recovery path.
Healthy opens no longer show an unnecessary location-save reminder, and the switch
warning covers unsaved editor state as well as unexported XML.

## Fresh acceptance

- Eight synthetic startup scenarios, run against the extracted package executable:
  healthy Unicode/space path, source conflict canceled, matching backup, wrong
  backup, missing root with backup, missing root canceled, corrupt snapshot and
  missing project. Native chooser/error callbacks are injected to avoid manual
  dialog loops. The actual queued event and form install/save paths are used.
- Checks include visibility before callbacks, one bounded folder request, failure
  preservation of the active demo/archive, correct original/relocated save state,
  exact XML export, restored Undo/Redo, successful save and usable retry via the
  public project-opening API. All 26 demo XML/assets remain unchanged.
- The extracted review ZIP runs all twelve full demo edit/save/reopen/exact-export/
  normal and restored Undo/Redo workflows, twelve clean-session workflows and the
  complete broad synthetic Windows-form suite. Separate reports record counts.
- The final payload check compares all 31 manifest files after the complete run.

An attended Windows check launched the actual packaged executable with a known
missing project argument. Its owned warning appeared over the intact demo. After
acknowledging it, the editor remained open and enabled; clicking Titan Demo loaded
20 elements with zero modified files and a clean title. The test editor was then
closed. The Computer Use skill performed the Windows interaction. No desktop
captures, private skin resources or runtime configuration enter the deliverable.

The 394 core checks and six real-Titan recovery workflows belong to the preceding
milestone and were not repeated for this startup/event change. No new installed
MP1 client test occurred. Earlier client acceptance remains scoped to its recorded
revision, page and exported sample. Native folder selection itself was not newly
exercised; its outcomes are covered by injected callbacks. These checks do not
establish complete renderer, theme or plugin equivalence.

## Reproduce

Build normally and create a review ZIP. Run the combined gate in a fresh directory:

```powershell
.\Validation\Package-Readiness\Run-Probes.ps1 -PackageZip C:\Review\MP1-Skin-Studio-Review.zip -OutputDirectory C:\Review\Integration -IncludeStartup
```

The helper uses the extracted executable and samples, runs broad UI/demo/session
checks, optionally adds startup scenarios and validates the final payload hashes.
Use -SkipFullUi for proportionate routine follow-ups. Add -IncludeCore only when
fresh core coverage is needed. Startup can also run independently:

```powershell
.\Validation\Startup-Acceptance\Run-Probe.ps1 -PackageDirectory C:\Review\Extracted -OutputDirectory C:\Review\Startup
```

The startup host uses disposable miniature source/backup fixtures. It sends no
desktop input. The broad UI host uses synthetic form events; some dialogs may
briefly appear. In this milestone, broad package acceptance and startup scenarios
ran separately against identical packaged executable bytes; the final hash gate
was also executed separately after both completed. Future runs use the single
combined helper. The script's PowerShell syntax was validated after integration.

Startup-Results.txt, UI-Results.txt, Package-Results.txt, Session-Results.txt and
Payload-Results.txt record this milestone. Final packaging updates revision notes
and independently verifies the payload manifest against the accepted candidate.
