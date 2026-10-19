# Attended Titan Video UI workflow

Date: October 19, 2026. Production build from a1559315e6217df180c32e451cfe1920592b058f.

The owner explicitly reauthorized Windows automation after the earlier freeze and manually completed native dialogs that Computer Use could not target. No MediaPortal client was launched during this validation.

## Observed results

- Demo pending input: Position X 140 to 150 marked the project unsaved. Save Project refused unapplied inputs and preserved 150. The owner closed the notice manually. Revert Inputs restored 140, cleared the unsaved marker and disabled export.
- Opened installed Titan myvideo.xml: 211 controls. Selected the concrete listcontrol in common.facade.video.default.xml using element search.
- Changed width 698 to 658 with Apply Properties. One source was modified. Undo restored 698 and zero modified sources; redo restored 658 and one modified source.
- Saved Titan-Video-UI-20261019.mp1skinproj with manual dialog completion. Independent archive inspection found project.xml plus original and edited snapshots; their list widths were 698 and 658.
- Reopened the saved project through Open Project with manual dialog completion. Reselected listcontrol and observed width 658.
- Exported Titan-Video-UI-20261019-Changes.zip with manual dialog completion. Independent checks verified exactly the edited XML and instructions, exported width 658, and byte equality with the edited project snapshot.
- Installed common.facade.video.default.xml SHA256 still equals the original snapshot hash: 03368b5033588d83a7c88033cb3550bcccd12b48e7161d3c184eb9f6a9c4b852.
- Export archive SHA256: e20148f6e9c010a48cbc64bd50f588d949057cabf83398c091a1a554e8ba3fc0.

## Limits and next priority

This is an attended core UI workflow, not a fresh run of the historical UI test suite. Collection-specific height/spacing fields were present but were not edited in this run. Theme changes and the other pending-input guards remain manually unverified.

The complex page preview has many unresolved conditions and overlapping literal bindings/placeholders (97 unresolved conditions after list selection). It is not yet a trustworthy representation of the live page. Improving useful preview states and evaluating binding expressions is a core priority before auxiliary editing conveniences. This export has not been loaded in MediaPortal. DirectX replacement remains deferred.

Computer Use repeatedly rejected native-dialog buttons as a non-target window of the same executable, and sometimes lacked geometry until fresh screenshot observation. Manual completion was required; no custom automation workaround was used.
