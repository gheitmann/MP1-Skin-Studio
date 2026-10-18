# Attended real-page editor workflow

Date: October 18, 2026, Europe/Berlin. Product baseline: f95aea57a7c016fb8ef24a1a4241d25c22a0aac4.

The actual running WinForms editor was controlled using Computer Use. No production model calls were used to perform these UI edits.

1. Selected the existing STUDIO TEST heading in the element tree of the saved real Titan Settings project.
2. Replaced Text / data binding with EDITOR TEST and Position X with 192 in the property fields. Clicked Apply Properties. The canvas and element tree visibly updated; the project became dirty.
3. Clicked Save Project. The existing Core-Workflow.mp1skinproj was saved and the dirty marker cleared.
4. Clicked Open Project, selected that file in the native file dialog, and opened it. The project-restored status and EDITOR TEST preview were observed.
5. Clicked the heading directly on the canvas. Property fields confirmed X=192 and Y=235 after reopening.
6. Dragged it approximately 20 screen pixels to the right. With Snap to 10 px enabled, X/Y became 240/240; the small vertical adjustment is the enabled grid snap.
7. Clicked Undo: X/Y returned exactly to 192/235. Clicked Redo: 240/240 returned. Clicked Undo again to restore the saved candidate.
8. Clicked Export Changes as ZIP and saved UI-Workflow-Export.zip through the native save dialog. The exported status was observed.
9. Ran Verify-UI-Export.ps1 against the saved project and exported ZIP. All five artifact checks passed; see UI-Artifact-Results.txt.

The installed original settings.xml retains its earlier SHA256. The UI-created export matches the saved snapshot as decoded XML text and contains only settings.xml plus installation instructions. The earlier API-created export and isolated client skin remain separate; this new EDITOR TEST export was not loaded into MP1 during this checkpoint. The previous STUDIO TEST candidate already passed the installed-client display/navigation gate.

The owner confirmed normal MP1 was restarted and reported another DirectX startup error before it succeeded. The current native window was present in enumeration. No client input, startup retry, configuration change, or DirectX investigation was performed in this checkpoint.

This proves tree/canvas selection, property editing, direct dragging, grid snapping, undo/redo, project saving/reopening, and native-dialog ZIP export on one real page. It does not establish complete behavior across all MP1 control types. There was no production-code change; the previous 221 core / 203 UI suite results are historical, not rerun here. No private desktop captures, client configuration, full skin copy, or machine-specific saved project are committed.

Next core gate: exercise a complex real list/facade page and identify preview or editing limits that would prevent useful skin authoring. Auxiliary grouping/style conveniences and DirectX replacement remain deferred.
