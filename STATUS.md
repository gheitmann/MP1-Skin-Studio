# Development status and next steps

Date: October 29, 2026. Imported version: 0.9.

## Latest development milestone: native MediaPortal live editing

Studio now includes an optional Live toolbar and a MediaPortal 1.39 process
plugin. Valid property drafts and in-progress canvas gestures update existing
native MP controls over an authenticated same-user local pipe. Apply, Revert,
Undo, cancelled gestures and Disconnect follow the editor workflow without
writing skin files or restarting/reloading MP for supported edits. The bridge
restores native values, animation baselines and changed tree structure when
disconnected or invalidated. Unsupported/ambiguous operations remain explicit.
See [live setup and scope](docs/LIVE_PREVIEW.md).

Fresh validation: 455 core checks; 13 original-MP-SDK adapter checks; 11
Studio-to-native-SDK-host checks; authenticated transport and crash reset;
17 focused toolbox/geometry workflows; twelve packaged demo workflows, twelve
clean-session workflows and 229 broad UI checks. All 489 original installed
skin/config XML hashes remained unchanged. The final two live insertion tests
postdate the broad UI package pass and affect only live frame construction.

The isolated official MP client displayed the real test Home skin and loaded
the bridge, but its sandbox blocked pipe clients. Normal-rights transport and
the Studio/native-SDK host passed. A normal MP startup explicitly requires the
owner's open Configuration Studio to close. Its window was preserved, so the
complete visible Studio-to-running-MP comparison is still pending. Windows
also denied writing the plugin to Program Files without administrator consent.
The portable package contains the built DLL, guarded installer (including
optional Windows elevation), guides and checksums. No existing release or tag
was replaced; preview.2 remains the latest published release.

## Latest source milestone: complete visual editing workflow / preview.2

October 20, 2026: completed the remaining legacy-editor recommendations.
Toolbox shows an integrated side panel with draggable text/button/image tools
and filtered skin Media thumbnails. Explorer drops accept one active Media
image. Insertions use skin coordinates, zoom/pan and snapping, target the current
page XML, and undo atomically. Image dimensions/proportions are retained; pending
inputs, Navigation mode, external/overridden paths and out-of-skin drops are
rejected. Media files remain unchanged. The toolbar wraps and the side panel
retains a usable preview at minimum window size.

Grid/Snap options and spacing presets (1–200 skin pixels) persist in the editor
project. Visible grid density adapts to zoom. Separate position and size locks
inherit through groups, protect gestures/property edits/resets/atomic batches,
and follow structural undo/redo. Existing complete locks remain available.
Project format 5 preserves these settings; formats 1–4 remain readable and XML
exports do not contain editor settings. Together with grouped properties/help
and font preview, all four recommended feature areas are implemented.

Acceptance: 438 core checks, 229 broad UI checks, twelve complete demo workflows
and twelve clean-session workflows passed on the integrated development build.
Final OLE result reporting and responsive toolbox/header layout were then checked
with 17 targeted workflows plus 25 selector/scroll/menu scenarios; all 488
installed DefaultWideHD XML hashes remained unchanged. No broad rerun after
that UI polish, attended drag/drop or MP1 runtime comparison is claimed.
See [workflow tools validation](Validation/Workflow-Tools/README.md).

v0.9.0-preview.2 was published as a Windows pre-release at 00:07 Berlin time on
October 21 (2026-10-20T22:07:00Z). Its tag points to source
4c07ae9a400da2145dcef1627fe842a4b43d8d92. Windows ZIP and SHA256SUMS.txt are
uploaded; both GitHub digests match the local files. An anonymous ZIP download
matched SHA256 6b327e594ee346eb5263dc98836b54a615d4f059d10ebef685f2d051f5897661.
See [publication proof](Validation/Workflow-Tools/Publication.json).
This post-publication documentation update does not move the published tag or
rebuild its assets. v0.9.0-preview.1 and its assets/tag remain unchanged.

## Previous source milestone: grouped properties and visual font choice

October 20, 2026: started the legacy SkinEditor feature recommendations with
beginner-oriented property groups and font preview. Applicable fields now appear
under six collapsible categories, retaining drafts and their apply/reset/binding
protections when hidden. Group expansion persists during the editor session.
Keyboard focus and label hover show plain-language help below the properties.
The font picker filters skin font names/aliases, previews editable sample text
using canvas font sizing, and reports actual family/style/pixel size and missing
family substitution. Cancel leaves inputs intact; Use font fills a draft and
Apply/Undo retains the source-preserving editing path. Shared resources are not
rewritten. Drag/drop insertion and configurable snapping/separate move/resize
locks remain subsequent recommendations.

Fresh development acceptance: 430 core checks, 216 broad UI checks (eleven new),
twelve complete demo workflows and twelve clean-session workflows passed.
After the final filter label/sample-length/invalid-height polish, fifteen
targeted property/font cases and 25 selector/scroll/menu cases passed, with all
488 installed DefaultWideHD XML hashes unchanged. The broad suite precedes that
small final polish; it was not repeated. Synthetic form renders were inspected;
no attended desktop interaction or runtime comparison is claimed. The published
v0.9.0-preview.1 assets/tag remain unchanged. See
[property tools validation](Validation/Property-Tools/README.md).

## First public release published: v0.9.0-preview.1

Publication verified on October 20, 2026: the repository is public and [v0.9.0-preview.1](https://github.com/gheitmann/MP1-Skin-Studio/releases/tag/v0.9.0-preview.1) is published as a pre-release. Its tag points to source 633170cbab3aff5ce18c5398cf59c3d4f6c0618b. Both the versioned Windows ZIP and SHA256SUMS.txt are uploaded; GitHub asset digests match the local files. An anonymous repository read and anonymous ZIP download succeeded, and the downloaded ZIP matched SHA256 be50eb6a5cc11f4dab5b469f2d900c48b97f113c3aa6b15f94ac23d70c699706. This post-publication documentation update does not change the release tag or assets. See [publication verification](Validation/First-Public-Release/README.md).

October 20, 2026: the owner requested public repository visibility and the first GitHub release. The release carries the accepted 0.9 prototype executable and bundled demos, with a versioned Windows ZIP, SHA256 checksum and release notes. README and repository audience conventions are updated. Before publication, all 540 tracked files matched the GitHub tree; the targeted credential-pattern scan found no matches. No installed skins or private runtime logs are added to the release package. Latest functional acceptance remains the 25 selector/sidebar and ten readable-preview scenarios; earlier tests retain their recorded scopes. Repository visibility and release publication are verified externally after this documentation checkpoint.

## Latest source milestone: choose another skin folder from the dropdown

Dropdown styling in this delivery also matches the scrollbar palette: RGB 20/26/35 backgrounds, 63/78/96 selections, 92/116/143 accents and light text/checks/arrows. All StudioMenus share the renderer; View and MediaTip Example now use the existing ChoiceBox instead of light native combo boxes. Collection mode and sample preset backgrounds match too. Fresh final validation passed 25 selector/sidebar scenarios and ten readable-preview scenarios; installed XML and demo hashes remained unchanged. The live closed View field was inspected; concurrent owner input interrupted the open-menu click, so no live open-menu acceptance is claimed. See [dropdown colors validation](Validation/Dropdown-Colors/README.md).

October 20, 2026: added Select different Skin Folder at the bottom of SKIN, separated from discovered skins. It opens the existing folder picker at the current skin and reuses pending-property and save/discard/cancel guards. The action is not stored or selected as a skin. Theme choices remain unchanged.

Fresh validation: 25 focused skin/sidebar/menu/scroll scenarios passed, including footer placement, rebuilding without duplicates and independent theme choices. All 488 installed XML hashes remained unchanged. Earlier 430 core, copy/dialog, complete-skin and package workflow results remain historical for this localized menu change. See [folder-action validation](Validation/Skin-Folder-Action/README.md).

## Previous source milestone: copy a skin as an independent new base

October 20, 2026: Copy skin beside SKIN opens a dark dialog for a new folder name and destination parent. It copies every skin file and empty subfolder, including images, fonts and themes. Applied XML changes can be included (default) or the disk version can be used. Work runs on a background task with a cancellable progress indicator. The source, current editor state and undo history remain unchanged. Existing targets and destinations inside the source are rejected; links are rejected, edited-source conflicts stop the copy, and failed/cancelled staging folders are removed. The completed skin is validated before publication and registered in the session dropdown, including external destinations. Optional opening uses existing pending-input/save/discard/cancel protections and retains the current page/theme; editor-only settings and undo history stay with the original session.

Fresh final acceptance: 430 core checks (eight new copy cases), eleven copy-dialog workflows, twelve complete demo workflows and twelve clean-session workflows passed; payload hashes passed. The skin/sidebar suite also passed 23 scenarios during this change. A complete DefaultWideHD copy validated all 3,399 copied bytes hashes against the source, checked all 3,399 originals unchanged and reopened myHome with 262 controls; the final follow-up changed only sidebar spacing to fit the new button. Live Windows inspection confirmed the final button and readable copy dialog without creating another installed skin. Original owner sessions were preserved. Previous readability/broad/client reports remain historical. See [skin-copy validation](Validation/Skin-Copy/README.md).

## Previous source milestone: aligned collection captions and a dark window caption

October 20, 2026: single-line list/playlist captions now honor the zero-height centered label anchor used by MediaPortal, so text sits inside its focus texture instead of below it. Font height is retained independently of the remaining row height, avoiding clipped glyphs. Collection clipping, XML positions, row texture geometry and focus navigation remain intact. The main native Windows caption, border and text use the header palette through documented DWM attributes, retaining standard caption buttons, moving, sizing and keyboard behavior. High contrast restores system caption colors; unsupported systems retain their native appearance.

Fresh acceptance: 422 core checks (two new rendering regressions), 23 sidebar/scroll scenarios, ten readable-preview scenarios, twelve complete demo workflows and twelve clean-session workflows passed. All 488 installed XML, 26 demo assets and 31 payload hashes were unchanged. Live Windows inspection confirmed the DefaultWideHD myHome focused title centered inside its blue texture, complete glyphs and the matching dark native caption. Maximize/restore input was attempted but subsequent owner window activity prevented a conclusive observation; it is not claimed as attended acceptance. Prior integrated-page/broad/client reports remain historical. See [focus/window validation](Validation/Focus-Window/README.md).

## Previous source milestone: visible dark scrollbars on the Windows desktop

October 20, 2026: corrected DarkScrollHost sibling z-order so its painted bars cover the native content windows. Previously, synthetic DrawToBitmap renders appeared dark while the actual desktop still showed light Windows bars. The content now explicitly sits behind the overlays; native viewport and scrolling behavior are unchanged.

Fresh acceptance: 420 core tests and 23 focused skin/sidebar/scrolling scenarios passed, including a new overlay z-order regression assertion. All 488 installed XML hashes remained unchanged. Attended Windows inspection confirmed dark horizontal/vertical bars in the page list and element tree, and dark properties/information bars; a subsequent owner-driven page change retained them. No successful automated desktop scrolling is claimed: an input attempt was interrupted by concurrent owner input and state was refreshed. Previous broad suites remain historical. See [scrollbar correction validation](Validation/Scrollbar-ZOrder/README.md).

## Previous source milestone: automatic skin selection and wider dark sidebar

October 20, 2026: added SKIN above THEME. It discovers immediate skin folders under standard Windows MediaPortal locations, current siblings and the bundled demo; refreshes when opened; deduplicates paths and disambiguates names. Skin changes reuse existing pending-input/save/discard/cancel safeguards, prefer the same page filename when available and restore the selector on cancellation/load failure. The left column expands from 245 to 319 pixels, with wider anchored controls. Main page/tree/properties/information scrollbars now use dark chrome over their native ranges and viewport; arrows, thumbs and wheel preserve selection/focus. Preview tools wrap at minimum width. Native hooks/timers release on disposal; no idle polling or client/configuration writes.

Fresh acceptance: 420 core checks, 22 skin/sidebar/scrolling scenarios, twelve integrated-page scenarios including menu callbacks, ten readable-preview scenarios, twelve full demo workflows and twelve clean-session workflows passed. All 488 installed XML, 26 demo assets and 31 payload hashes remained unchanged. Default/minimum form renders were inspected. The installed list currently contains DefaultWideHD, PureVisionHD 1080 XMAS 2025, three SkinStudio test copies and Titan, plus the bundled demo. Prior caption/broad/client and performance reports remain historical. See [skin/sidebar validation](Validation/Skin-Selector/README.md).

## Previous source milestone: caption-sized text selection

October 20, 2026: fixed the reported MediaTip.xml Album Artists outline spanning the next row/value column. Labels without dimensions had inherited the designer's synthetic 400-by-64 frame. Single-line label/fadelabel outlines now follow the actual caption ink with its current font, alignment and clipping; mouse selection, overlap menus and double-click agree with that outline. Caption dragging retains position transactions and snapping. Text field Width/Height remain explicit property edits; the misleading text resize corner is removed. Non-text surfaces and multiline textboxes keep their existing field geometry. Runtime layout and source XML dimensions are unchanged.

The actual MediaTip Album Artists outline measured about 159 by 22 units instead of 400 by 64, with the adjacent Artists row independently selectable. Fresh checks passed 416 core tests, 58 targeted real-MediaTip/designer/search/text UI assertions, ten readability scenarios, twelve complete demo workflows and twelve clean-session workflows. All 488 installed XML and 31 package payload hashes remained unchanged; demo hashes also passed the readable suite. The synthetic form render was inspected. Prior full 205 UI and renderer/load measurements remain historical. See [caption selection validation](Validation/Text-Selection/README.md).

## Previous source milestone: faster XML loading and canvas visibility passes

October 20, 2026: XML source scanning now checks comment/CDATA/PI openers only at the current tag rather than searching the remaining file repeatedly. The normal validated XML reader, mapping, encoding and byte-preserving edits remain intact. Scoped visibility passes reuse completed root results by control occurrence only within one canvas paint or pointer inspection, retaining recursive cycle/work-budget behavior and releasing results before callbacks or subsequent frames.

DefaultWideHD serial medians: complete BasicHome load 3121 to 1536 ms, warm paint 445 to 295 ms; myHome load 494 to 139 ms, paint 131 to 103 ms; MyMinesweeper load 1322 to 267 ms, paint 85 to 68 ms. All 488 byte/span/DOM mappings match. Nine comparison images match the baseline replay pixel-for-pixel; the first baseline capture differed at five pixels in diagnostic BasicHome, and the same difference occurred when rerunning the unchanged baseline. Three new core tests cover scope/cycle/freshness and special XML scanning. Fresh acceptance passed 414 core checks, 205 broad UI assertions, ten readability scenarios, twelve full demo workflows and twelve clean-session workflows; all 488 installed XML, 26 demo assets and 31 payload hashes stayed unchanged. See [render/load validation](Validation/Render-Load/README.md) for current acceptance and limits. Complex BasicHome still takes about 1.5 seconds to prepare and 0.3 seconds to repaint; remaining tree/condition/image work is explicit.

## Previous source milestone: faster selection and pointer inspection

October 20, 2026: profiling DefaultWideHD identified repeated native element-tree updates as the dominant selection cost. Updates now batch drawing and leave unchanged labels/colors intact. Filtered branches are assembled before native insertion. Diagnostics evaluate visibility once per control. Pointer selection, navigation target picking and overlap lists share one measured layout within an inspection, releasing it before callbacks; focus/sample/XML changes cannot reuse it across events. No persistent visibility cache or source-file changes were introduced.

Fresh extracted-package acceptance: 411 core checks, 205 broad UI assertions, ten DefaultWideHD readability scenarios, twelve complete demo workflows and twelve clean-session workflows passed. All 488 installed XML, 26 demo assets and 31 package payload hashes remained unchanged. The broad UI test now explicitly verifies the clean default and diagnostic layers instead of assuming the pre-readability default.

See [responsiveness validation](Validation/Responsiveness/README.md) for baseline comparisons, current package checks and remaining canvas-painting costs. The readable preview with explicit MediaTip variants remains included. Earlier attended client evidence remains historical.

## Previous source milestone: readable editing views and MediaTip variants

October 20, 2026: integrated View now offers Edit layout, temporary selection/group isolation and diagnostic All layers. New skins open in Edit layout with short unresolved captions and known visibility. Recognized MediaTipOverlay.xml offers Music/Video/Pictures/Folder/Radio examples, displaying one authored block instead of five overlaid sets. Original bindings remain in property fields and exports; runtime conditions and audit uncertainty are not rewritten. View/example state is independently validated and saved in projects; isolation clears on page/theme/session change. Legacy projects retain diagnostic/clean intent. Existing BasicHome/media-folder presets return to Edit layout.

Fresh extracted-package acceptance: 411 core checks (nine new readability tests), ten targeted DefaultWideHD form workflows, twelve complete demo workflows and twelve clean-session workflows. All 488 installed XML, 26 demo assets and 31 payload hashes stayed unchanged. Music/Pictures/minimum renders were inspected. Attended Windows observed Music, confirmed Video through the Example selector and saved the preview-only project; independent archive inspection found layout/video metadata and no XML snapshots. The new MediaTip-Readable editor stays open; prior owner sessions were preserved. See [readable preview validation](Validation/Readable-Preview/README.md). Other pages/plugins use generic isolation; no complete plugin/client fidelity claim. Prior broad/browser/review/transition/startup reports remain historical.

## Previous source milestone: integrated pages/XML list with mouse-wheel scrolling

October 20, 2026: replaced the page dropdown with a permanent sidebar list, literal filename filter, count/no-results state and a resizable divider above the element tree. Hover-wheel routing scrolls visible rows without selecting another page or stealing property focus. Partial deltas and boundaries are handled; routing unregisters on disposal and excludes other forms/outside areas. Pending page selections restore the active row with status guidance instead of a modal. Existing failed-load/session preservation remains in force.

Fresh extracted-package acceptance: twelve targeted integrated-page scenarios, twelve current menu assertions, twelve complete demo workflows and twelve clean-session workflows. All 488 installed XML, 26 demo source/asset and 31 package payload hashes stayed unchanged. Default/minimum form renders were inspected. The new packaged editor is open; current Windows state shows the owner already filtering plugin (five of 449 files), with myHome preview retained. No automated input followed the user-input notice, preserving filter/zoom/session; actual desktop wheel input is not newly claimed. See [integrated page validation](Validation/Integrated-Pages/README.md). Prior core/broad/review/transition/startup and large-menu reports remain historical. XML editing, renderer and export logic are unchanged.

## Previous source milestone: DefaultWideHD large-selector responsiveness

October 20, 2026: reproduced DefaultWideHD's 449-file PAGE / XML selector blocking for 9,242 ms initially and 25,479–26,519 ms on reopen. Repeated layout during insertion and old-item removal caused the delay. Batched clearing/population reduces measured opens to 73–82 ms with one layout, retaining all choices, checkmarks, callback and cancellation/ownership behavior. No XML editing, renderer or export logic changed.

Fresh acceptance: seven targeted selector/form workflows, 12 existing menu assertions, twelve complete demo workflows and twelve clean-session workflows. DefaultWideHD Home/Video/Settings changes, save/reopen/export/restored Undo/Redo passed; all 488 installed XML and 26 demo hashes stayed unchanged. A final preservation gate verified all 31 package payload hashes. The selector host completed separately after correcting its DemoRoot fixture setup; the same executable was used throughout. Attended Windows verified list opening/selection, reopen/Escape and XML-tree collapse/expand in a separate corrected editor. The owner editor remained open. See [selector validation](Validation/Large-Selectors/README.md). Prior 402 core, ten XML-review, 18 transition, eight startup and broad 204 UI suites remain historical.

## Previous source milestone: session-wide XML change review

October 20, 2026: Element > Review XML changes / Ctrl+Shift+E now opens a read-only snapshot of all modified session XML, including shared includes, theme sources and earlier pages. File filtering, original/edited panes, first difference and complete large-file sections support review before export. Exact original/edited byte hashes and encoding/BOM details accompany refreshable on-disk source checks. Pending fields and preview-only state remain excluded; XML, selection, drafts, history and save/export markers are preserved.

Fresh extracted-package acceptance: 402 core checks (eight new review regressions), ten targeted synthetic review-form/menu/shortcut workflows, twelve complete demo workflows and twelve clean-session workflows. All 31 payload hashes and 26 demo source/asset hashes stayed unchanged. The synthetic dialog render was visually inspected. No new attended desktop/native picker or installed-client run; previous 204 broad UI, 18 transition and eight startup scenarios remain historical. Renderer, XML editing and export behavior are unchanged. See [change review validation](Validation/Change-Review/README.md).

## Previous source milestone: save before closing or switching sessions

October 20, 2026: closing or switching sessions offers Save project / Discard changes / Cancel for unsaved XML or editor state. Saving continues only after successful persistence and a clean session. Canceled Save As and failed saves preserve work/history; Discard does not clear the active model before a replacement loads. Enter/Escape cancel. Pending inputs remain protected, with Save disabled in the close dialog and session switching blocked until Apply/Revert.

Fresh extracted-package gate passed 18 targeted synthetic form scenarios, twelve complete demo workflows, twelve clean-session workflows and eight startup scenarios. All 31 payload hashes remained unchanged after the combined run. Existing-file saves and actual modal/FormClosing button events were exercised; native Save As cancellation is injected. No attended native picker or new installed-client run is claimed. Broad 204 UI and 394 core checks remain historical; model/render/export logic and demo assets are unchanged. See [transition acceptance](Validation/Session-Transitions/README.md).

## Previous delivery milestone: current edit-to-client acceptance

October 20, 2026: the current executable from source 07e8ef2 passed a fresh attended real-Titan workflow. Its saved Video project opened in Windows, list Width changed to 590 through the form, Apply/Save succeeded and a separate process reopened/exported that exact saved edit. Two XML payloads and instructions, byte/hash identity, restored Undo/Redo and all 17 loaded original source hashes passed. A fresh complete runtime copy displayed the shared header and shorter Video focus band; Left/Right menu navigation passed. Original Titan was restored through the client UI, with all 2,050 original file hashes unchanged.

This accepts the bounded first-use Default Home/Video sample on the actual current executable. Documentation now reflects the fresh client result and preserves the boundary for other intended pages/themes/plugins. No editor code changed; previous 204 UI, eight startup, twelve demo/twelve session and 394 core checks remain historical instead of being rerun. Final packaging compares all unchanged runtime payload with the accepted ZIP. See [current runtime review](Validation/Current-Runtime-Review/README.md).

## Previous source milestone: resilient project startup and integrated package acceptance

October 20, 2026: starting with a saved project now waits for a visible editor and uses the same matching-backup recovery as Open Project. Cancel, corrupt/missing project and unsuitable backup leave a usable demo with retry guidance instead of ending startup. Showing the editor again does not repeat the action. Healthy opens no longer request a redundant location save; the switch warning covers all unsaved editor state. A new packaged readiness guide defines accepted scope and remaining final visual/client review.

Fresh validation: eight extracted-package startup scenarios, twelve complete demo workflows, twelve clean-session workflows and 204 broad synthetic Windows-form checks. All 31 manifest payload hashes are checked after the full integration run. Attended Windows verified the actual missing-project launch, owned warning, retained demo and working Titan Demo reload. The previous 394 core checks and six full Titan recovery workflows remain historical; no new client run or native folder selection is claimed. See [startup and integration acceptance](Validation/Startup-Acceptance/README.md). The combined helper now supports optional startup checks and a final manifest-preservation gate.

## Previous source milestone: recover saved work against matching skin backups

October 20, 2026: Open Project now offers one matching-backup folder selection when the saved root is unavailable or a modified original has drifted/disappeared, including when the old root still exists. The conflicting relative XML is named; source hash/integrity/path/encoding checks remain mandatory. Cancel and rejected backups preserve the active session. A changed root remains visibly unsaved until a successful project save; export/Undo cannot clear it. Subsequent opening uses the saved replacement root directly.

Fresh validation: 394 core checks; six synthetic Windows-form Titan BasicHome/Video/Settings workflows across Default/Christmas, each recovering three modified XML files with canceled/wrong-backup and pending-field preservation, exact export, Undo/Redo, locked-save preservation, location persistence and clean direct reopen. All 18 exported payload checks passed. All 2,050 files match in the original test baseline and both complete skin copies after the intentionally drifted fixture is restored. The extracted package passed twelve complete demo workflows plus twelve clean-session workflows with 30 unchanged payload hashes. See [project recovery validation](Validation/Project-Recovery/README.md). No new attended native-dialog or installed-client acceptance; those prior reports remain historical. Matching covers modified XML originals, not all backup dependencies.

## Previous source milestone: verified exports and source-conflict preservation

October 20, 2026: XML export now compares every modified source against its original loaded bytes before generation and again before publishing the archive. Missing/externally edited originals stop export while preserving the existing ZIP, pending changes and Undo/Redo. Every generated archive entry is read back and compared byte-for-byte before atomic replacement. The existing instructions entry now records relative paths, original/exported byte counts and SHA-256 hashes, active page/theme and shared-include scope. ZIP contents remain changed XML plus one instructions file.

Fresh validation: 389 core checks; six complete Default/Christmas Titan BasicHome, Video and Settings workflows with three changed source files each, exact-byte/hash validation, project save/reopen and normal/restored Undo/Redo; 45 loaded original XML hashes unchanged. The extracted package also passed twelve full demo workflows and twelve clean-session workflows with 30 unchanged payload hashes. Details are recorded in [verified export validation](Validation/Verified-Export/README.md). Prior attended Windows, reporting and installed-client reports remain historical; no new runtime rendering claim is made. Checks cover modified source files, not all dependencies or subsequent external changes.

## Previous source milestone: saved audit and preview reports

October 20, 2026: Audit and Ctrl+F8 now save their current shown filter as text or self-contained HTML, with page/theme, scope, total/shown counts, source locations and complete details. Shared filtering keeps display and saved output consistent. XML-derived markup is encoded, empty filters remain explicitly bounded, and atomic replacement preserves old reports on failure. No XML, editor state or runtime behavior changes during reporting.

Fresh validation: 383 core checks; 144 saved real-Titan dialog snapshots across three pages, two themes, two report kinds, both formats and filters; 45 unchanged original XML hashes. Final package passed twelve full demo workflows and twelve clean-session workflows with 30 unchanged payload hashes. Attended Windows opened/canceled the new native save dialog and returned to a clean editor; actual writing is covered by automated dialog/core checks. Prior broad UI/client acceptance remains historical. See [report validation](Validation/Finding-Reports/README.md).

## Previous delivery milestone: integrated Windows and runtime acceptance

October 20, 2026: the current packaged editor passed an attended four-arrow nested navigation roundtrip. A fresh two-file Video export from this executable loaded in the installed MP1 client on a complete isolated skin copy: header marker, narrower list and side-menu Left/Right passed. Titan was restored through its UI. All 2,050 original Titan file hashes match the preserved baseline; the runtime copy differs in exactly the two intended XML files.

Fresh package gate: 378 core checks, twelve complete demo workflows and twelve clean-session workflows passed; 30 payload hashes stayed unchanged. The broad 204 UI checks remain historical. A new Quick start covers all twelve demos and the complete edit/project/export/runtime-copy workflow. Package checks can now skip the broad UI suite and optionally include core checks to avoid unnecessary repetitions. No engine feature changed. See [integrated acceptance](Validation/Release-Acceptance/README.md). Preview fidelity and plugin/playback coverage remain bounded.

## Previous source milestone: honest slider and spin arrow limits

October 20, 2026: preview arrows no longer treat every slider/spin XML link as a reachable runtime navigation edge. Slider Left/Right remains reserved for unsimulated value adjustment; spin arrows on its orientation axis remain unverified without inner-button/value state. Unknown/dynamic spin orientation blocks all arrows. Explicit cross-axis routes retain existing target checks; raw navigation editing/audit and exports remain intact. Ctrl+F8 now explains missing slider/spin rendering and interaction. Checkmark findings now have a specific message rather than the generic text label.

Fresh validation: 378 core checks, ten complete real Titan edit/save/reopen/export/normal and restored Undo/Redo workflows, 28 accepted explicit routes, 92 guarded routes and 12 unchanged original XML hashes. Sixteen synthetic Windows canvas arrow events verify unchanged focus/signature/status on guards, working cross-axis links, informational diagnostics and saved focus. All twelve extracted-package demo workflows passed again, with 29 unchanged payload hashes. The preceding 204 UI and twelve clean-session checks remain historical; unchanged broad UI suites were not repeated. No new attended input or installed-client run. See [control-action validation](Validation/Control-Actions/README.md).

## Previous source milestone: clean startup and extracted-package acceptance

October 20, 2026: opening a skin/project now captures its baseline after initial designer selection, including fallback focus on pages without defaultcontrol. This removes a false unsaved marker while retaining later focus/view/XML save detection. A viewport regression assertion covers clean initial focus. The overlap-selection UI assertion now checks unchanged XML plus correct persisted focus, rather than wrongly requiring all editor metadata to stay unchanged when selecting another focusable control.

Fresh acceptance uses the compiled editor from an extracted review ZIP: 204 automated Windows-form UI checks, twelve complete demo load/preview/edit/save/reopen/export/normal and restored Undo/Redo workflows, and twelve clean-open/zoom/Fit/save/reopen checks passed. All 29 payload hashes stayed unchanged before/after matrix and UI workflows. Final package metadata/guide were updated afterward; the accepted executable and samples are unchanged. The preceding 370 core/model checks remain historical and were not repeated for this form initialization fix. No new attended input or installed-client run. See [package acceptance and reproduction](Validation/Package-Readiness/README.md).

## Previous source milestone: checkmark previews and mixed navigation

October 20, 2026: native checkmark controls now render selected/unselected local textures with static mark sizing, label-side placement and focus/unfocused text colors. Known true/false expressions use explicit preview data; unresolved selection stays unknown with Ctrl+F8 diagnostics. Six checkmark-only properties provide normal editing and reset. Dimension aliases preserve existing XML spelling and reject ambiguous edits. Mixed button/checkmark scenes use bounded geometric navigation, including linear stacks, without activation or selection toggling. demo.checkmark.xml supplies selected/unselected/unverified examples.

Fresh validation: 370 core checks, eight complete real Titan workflows, 172 accepted explicit routes, 28 guarded routes and 12 unchanged loaded original XML hashes. Six synthetic Windows canvas arrow events exercise the mixed demo; all six property fields, selected-expression Apply and save/reopen passed. Production state and edited form renders were inspected. Real pages verify explicit mapping/editing regression; authored fixtures and the demo cover new automatic mixed navigation. No new attended input or installed-client run. See [validation and bounds](Validation/Checkmark-Preview/README.md).

## Previous source milestone: ordinary group and standalone button navigation

October 20, 2026: absent/zero button mappings now use bounded geometric navigation in ordinary groups and at page level. Ordinary wrappers retain absolute child positions, can nest other ordinary wrappers, and can contain independently positioned automatic layouts. Ordinary wrappers moved by an automatic ancestor remain guarded. Property-driven empty layout fallbacks are not mistaken for plain groups. Group/ungroup and structural Undo/Redo preserve navigation. demo.free.xml supplies a self-contained four-button scene.

Fresh validation: 361 core checks, ten complete real Titan edit/save/reopen/export/normal and restored Undo/Redo workflows, 124 accepted routes (26 automatic and 98 explicit), 20 guarded routes and ten unchanged original XML hashes. Four synthetic Windows canvas arrow events cross ordinary group branches; saved focus restores. The production demo render was inspected. The real dialogs mainly verify existing explicit mappings; authored fixtures and the bundled demo exercise new automatic plain-group routes. No new attended keyboard or installed-client run. Native math and prior matrices remain historical. See [validation and limits](Validation/Plain-Navigation/README.md).

## Previous source milestone: nested layout navigation

October 20, 2026: automatic geometric navigation now traverses supported nested StackLayout/GridLayout hierarchies and crosses their branches. A top-level stack button can enter or leave a nested layout. All participating measurement subtrees are validated recursively; ordinary/unknown layout wrappers stay unsupported. Hidden/collapsed ancestors exclude descendant targets. Explicit mappings retain precedence and simple linear stack behavior stays separate. demo.navigation.xml supplies a self-contained nested grid/stack scene.

Fresh validation: 353 core checks passed, including nine new nested-navigation checks and the updated nested-grid acceptance check. Four synthetic canvas KeyDown events passed through the Windows form across both branches, with matching focus/selection and saved focus restored. The production render was inspected. Six complete real Titan regression workflows passed again, with 162 accepted routes, 74 guarded routes and 13 unchanged loaded original XML hashes. These Titan pages cover existing non-nested behavior; nested behavior is covered by authored fixtures and the bundled demo. The prior native bearing/distance comparison was not repeated because that math is unchanged. No new attended keyboard or installed-client run. See [validation and limits](Validation/Nested-Navigation/README.md).

## Previous source milestone: bounded automatic grid navigation

October 20, 2026: absent/zero arrow mappings on direct buttons in top-level GridLayouts now use native-style rounded top-left bearings, distances and document-order selection. Explicit mappings retain precedence. Eligible standalone buttons and direct top-level stack/grid peers participate. Unknown visibility, ambiguous IDs, unsupported focusable types and geometry stop the route without mutation. demo.grid.xml now provides visible focus textures on all five buttons and navigation instructions.

Fresh validation: 344 core checks, six complete real Titan edit/save/reopen/export/normal and restored Undo/Redo workflows, 162 accepted automatic routes, 74 conservatively guarded routes and 13 unchanged loaded original XML hashes. All 6,561 bearing/distance comparisons agree with the original installed Core.dll. Six synthetic canvas KeyDown events passed through the production Windows form handler; the bundled demo render was inspected. This was automated form testing, with no new attended keyboard or installed-client run. See [validation and bounds](Validation/Grid-Navigation/README.md).

## Previous source milestone: aligned and cropped image previews

October 20, 2026: image previews honor align/valign and ordered legacy centered settings, plus proportional fit, centered crop and crop from top. Image-only property fields expose the three fit/crop flags. Unknown settings receive informational diagnostics. demo.images.xml supplies six bundled comparisons without external assets. Collection fitting behavior remains separate.

Fresh validation: 332 core checks, 14 real Titan workflows, 98 bounded image plans and 55 unchanged loaded original XML hashes. Attended Windows flag editing/Apply/save/Undo/Redo and saved-archive inspection passed. Final image-specific alignment label wording was compiled afterward without repeating unchanged suites. No new installed-client run. See [validation and limits](Validation/Image-Presentation/README.md).

## Previous source milestone: Windows acceptance and self-contained filmstrip demo

October 20, 2026: attended Windows acceptance passed for the Video PIN Unlocked menu, protected position fields even with binding replacement enabled, arrow navigation and save/close/reopen from the extracted review ZIP. The saved scene retains item 4 and unlocked state with no XML snapshots or derived-focus overrides.

Fresh package validation loads all eight full demo pages and passes the new self-contained filmstrip navigation/save/reopen/export/undo workflow; all 22 bundled source/assets stay unchanged by the probe. A new demo.filmstrip.xml makes property-driven positions directly reviewable without an installed skin. The three authored menu buttons now have readable white centered captions. The engine binary is unchanged; the preceding 322 core checks and six Titan workflows were not repeated. No new installed-client run. See [acceptance and limits](Validation/Review-Acceptance/README.md).

## Previous source milestone: filmstrip coordinates and image position bindings

October 20, 2026: supported static filmstrip facades expose selected thumbnail coordinates; overlays with image posX/posY property attributes follow those coordinates. Integer cint/add/sub expressions and explicit Video PIN samples complete the tested Titan filmstrip scene while keeping unknown PIN state unverified. Property-driven position fields remain protected; XML bindings and fallbacks are preserved.

Fresh validation: 322 core checks, six real Titan filmstrip workflows, 17 unchanged original source hashes and six production canvas renders. A pre-delivery Windows build restored the saved nine-item filmstrip with zero unresolved visibility in accessibility diagnostics; the new PIN submenu and position fields were not newly exercised through attended input. No fresh installed-client run. See [validation and limits](Validation/Filmstrip-State/README.md).

## Previous source milestone: explicit sample artwork

October 20, 2026: Sample Artwork provides searchable, explicit local image/empty/original scenarios for image texture expressions. Rendering and Control.HasThumb follow the chosen samples; XML bindings, exports and XML undo history remain unchanged. Samples persist across save/reopen and retain normal theme asset precedence. Ctrl+F8 also explains unavailable visible artwork. Duplicate IDs remain unverified.

Fresh validation: 311 core checks, eight real Titan artwork workflows, 23 unchanged loaded original XML hashes, and attended Windows menu/search/empty/Apply/save acceptance. A final button docking correction was compiled and exercised in Windows after the model runs. No fresh installed-client run. See [validation and limits](Validation/Artwork-Preview/README.md).

## Previous source milestone: explained and bounded preview states

October 20, 2026: matching Video/Music/Pictures folder presets, explicit idle samples and Ctrl+F8 explanations make unresolved conditions inspectable without changing XML. Bounded condition evaluation retains unknown states. Canonical source cache paths fix mixed-slash alias consistency through save/reopen and undo/redo. Pending property drafts are preserved when a finding jump is blocked.

Fresh validation: 300 core checks, 22 complete real Titan workflows, two idle transitions and 57 unchanged loaded original source hashes. Attended Windows report/search/draft protection passed. No fresh installed-client run. See [validation and limits](Validation/Preview-Reliability/README.md).

## Previous source milestone: collection interaction and complete workflow coverage

October 19, 2026: Navigation arrows browse sample items in lists, icon grids, filmstrip and coverflow; boundary arrows retain explicit control-link testing. Focused collection appearance follows simulated focus. Selection stays visible, list row/detail geometry is corrected, clean preview suppresses invented placeholders, selected-thumbnail metadata follows the row and page preparation clears stale derived values. Active facade views expose their concrete property fields and regain canvas keyboard focus after dialog changes.

Fresh model/render validation: 286 core checks and 70 complete real Titan page/theme/view workflows passed; 33 distinct loaded original source hashes unchanged. Final form-only changes were compiled and tested through attended Windows list/grid/coverflow navigation, side-menu focus, save and close/reopen. Saved XML snapshot hashes are unchanged by UI preview actions. The passing model suite/matrix were not repeated for those form-only changes. No new installed-client run. See [validation and limits](Validation/Collection-Interaction/README.md).

## Previous source milestone: self-contained group duplication

October 19, 2026: Duplicate copies top-level groups and nested ordinary groups with new IDs, internal navigation/default-control/known visibility references and one structural undo step. Inherited style references are overridden locally; originals and shared styles remain unchanged. Insert/Duplicate now restore selection through Undo/Redo. ID allocation also reserves static default/visibility references in unopened files. Unsupported shared/template/dynamic cases fail before source mutation.

Fresh build: 272 core checks passed. Complete Default/Christmas Titan Settings clone/navigation/save/reopen/export/undo workflows passed, with 361 original source hashes unchanged per workflow. Attended Windows copying, selection, Undo/Redo, save and close/reopen passed; the saved Christmas selection was also checked. No fresh installed-client run. See [validation and limits](Validation/Group-Duplication/README.md).

## Previous source milestone: active page/theme save detection

October 19, 2026: switching the active page or theme now marks the editor project as unsaved. Returning to the previous location restores its signature if no other state changed. Saved projects reopen at the selected location without modifying skin XML or export state. Fresh build: 263 core checks and four real Titan view roundtrips passed; 34 loaded source hashes unchanged. No fresh interactive UI/client run. See [validation](Validation/Active-View/README.md).

## Previous source milestone: independent page/theme focus states

October 19, 2026: editor projects retain separate focus states for each page/theme rather than only the latest page. Restoring an existing slot leaves the editor signature unchanged. Legacy single-focus projects migrate. 261 core checks and four real Titan focus states across BasicHome/Settings and Default/Christmas passed, including exact save/reopen and 34 unchanged original source hashes. No new interactive UI/client run occurred. See [validation](Validation/Focus-Map/README.md).

## Previous source milestone: attended navigation and focus caption

October 19, 2026: actual Windows-editor checkbox/arrow navigation passed on BasicHome and Settings. Saved focus survived closing/reopening BasicHome; automatic General→GUI navigation passed in Settings. A missing caption refresh was corrected: focus changes now display an unsaved asterisk, cleared by Ctrl+S. Saved manifests and unchanged XML were checked. The prior 258 core checks are historical; no full-suite rerun or MP1 client run occurred for this caption-only change. See [attended validation](Validation/Attended-Navigation/README.md).

## Previous source milestone: bounded automatic stack navigation

October 19, 2026: the preview handles absent/zero directions along linear StackLayout button menus while preserving explicit mappings. A complete eleven-button Settings down/up loop and both BasicHome theme routes passed, together with 258 core checks. No XML links are generated. This is a bounded subset of native geometric navigation; nested/grid/free-position search remains unsupported. No new interactive UI/client run occurred. See [validation](Validation/Stack-Navigation/README.md).

## Previous source milestone: focus-based preview navigation

October 19, 2026: existing arrow navigation now uses simulated focus and the active facade template. Prospective target visibility is checked without leaking rejected focus changes; unavailable routes produce status notices. 256 core checks and real Titan BasicHome routes in Default/Christmas passed, including saved final focus and unchanged source hashes. Settings explicit edges passed; automatic group-generated directions remain unsupported and are the next concrete gap. No new interactive UI/client run occurred. See [validation](Validation/Preview-Navigation/README.md).

## Previous source milestone: usable BasicHome sample

October 19, 2026: BasicHome now has an explicit saved preview preset. Static absent-control visibility and supported boolean expression conditions resolve correctly; large button text offsets no longer erase captions. 254 core checks and real Titan Default/Christmas sample roundtrips passed. Both rendered themes show six tile pictures and German labels with TV focused; eleven conditions remain unverified. All 32 loaded original source hashes are unchanged. No new interactive client run occurred. See [validation](Validation/BasicHome-Preview/README.md).

## Previous source milestone: page/theme workflows and button focus

October 19, 2026: button focus appearance now follows simulated MP1 focus independently of designer selection. 250 core checks and twelve edit/save/reopen/export/undo workflows across six pages in Default and Christmas passed. All loaded source hashes stayed unchanged. Settings rendering was visually inspected after the fix. BasicHome theme assets load, but dynamic menu content remains incomplete. No new installed-client run occurred. See [matrix validation](Validation/Page-Theme-Matrix/README.md).

## Previous source milestone: explicit stopped playback preview

October 19, 2026: explicit samples now evaluate five common player conditions. The Video folder preset chooses stopped playback; Element > Playback preview can apply or remove that scenario independently. Absent flags stay unknown. 249 core checks and the targeted real Titan probe passed. In this scenario 19 conditions remain unresolved, down from 62. All 17 loaded source hashes remain unchanged. No new interactive client test occurred. See [validation and remaining unknowns](Validation/Playback-Preview/README.md).

## Previous source milestone: Video sample state and text anchors

October 19, 2026: standalone labels now use native-style XML text anchors instead of fabricated designer extents. The Video folder sample explicitly sets unwatched and idle-scroll state. 248 core checks and the targeted Titan probe passed; the production offscreen render was visually inspected. The explicit sample leaves 62 unresolved conditions, down from 87. All 17 loaded source hashes remained unchanged. No new MP1 client test was run. See [validation](Validation/Label-Anchors/README.md).

## Previous source milestone: reproducible clean Video preview

October 19, 2026: saved projects retain page-scoped preview focus. The Video folder preset now reopens with the list focused and hidden menu closed. Clean mode omits unresolved labels and missing image placeholders while preserving diagnostics and XML. 247 core checks and the targeted real Titan probe passed; the reopened GUI was visually inspected. All 17 loaded Titan source hashes remained unchanged. The unresolved-condition count remains 87; this improves presentation and persistence rather than inventing runtime values. See [validation](Validation/Preview-Session/README.md). DirectX work is handled in a separate chat.

## Latest installed-client validation milestone: Video export

October 19, 2026: the exported two-file Video candidate loaded in the installed MP1 client using an isolated skin copy. The header marker, narrower list focus band and side-menu keyboard navigation passed. Titan was restored in the UI and its normal Video page verified. A fresh comparison checked all 2,050 original Titan files with zero differences; the isolated copy differs in exactly the two intended XML files. See [runtime validation](Validation/Video-Runtime-20261019/README.md). This does not establish complete preview fidelity or playback coverage.

## Previous source milestone: known preview content

October 19, 2026: static known-content conditions and nested localization now work in previews. Fresh Windows build: 245 core checks, 25 expression checks and eleven real Titan model stages passed. The isolated folder state has 87 unresolved conditions, down from 89. A production offscreen render was visually inspected at that source checkpoint; the later runtime checkpoint above validates a separate exported candidate. See [content validation](Validation/Preview-Content/README.md). Windows access was confirmed in earlier attended checkpoints; DirectX remains deferred.

## Current priority: reliable core editing

The owner prioritizes practical edit/save/reopen/export workflows and substantial autonomous work blocks. Routine follow-up steps do not require another continuation prompt. Necessary checks run at integrated milestones.

Current integration validation is recorded at the top of this file. The packaged quick start and readiness guide describe a practically usable bounded review candidate. Preserve the distinction between fresh integration checks and older core/Titan/client reports; completion counts are evidence for their named workflows, not a measured product-completion percentage.

The fresh sampled Default Home/Video client gate is accepted for the current executable. Other intended edits still need their own final visual review and client acceptance before general release. Preview fidelity remains bounded: additional focusable/control/facade types, plugin states, masks and animation may need future work. Keep these feature gaps explicit rather than silently expanding a first usable version into full renderer parity. Auxiliary global-style and cross-group movement tools remain lower priority.

Historical milestones follow; older suspension notes describe the state at their recorded time. Attended Windows automation was subsequently reauthorized and exercised successfully.
## Baseline

Imported from the complete locally extracted `MP1-Skin-Studio-0.9` release package. Includes source code, tests, samples, build script, existing license, historical validation reports and runtime test materials. Installers, compiled binaries, error logs and temporary working directories are excluded.

All documentation and validation descriptions use English. Intentional multilingual fixtures and original external schema identifiers are preserved. New documentation, commit messages, issues and pull requests use English unless requested otherwise.

## English migration

Repository paths, documentation, application messages and test descriptions use English.
See [migration notes](docs/ENGLISH_MIGRATION.md). Fresh validation: **189 core checks and 185 UI checks passed**, with reports in [Validation/English-Migration](Validation/English-Migration). Build and runtime-export XML integrity checks also passed. New projects should start in English,
with a private GitHub repository and a maintained status document.

## GridLayout milestone

Added static GridLayout measurement and arrangement, including Titan's vertical
fill, row precedence, margins, alignment, stretching and MP1 visibility behavior.
Calculated rectangles drive rendering and mouse selection without editing XML.
Nested stack/grid groups and undo refresh are covered. The existing group-layout
dialog edits StackLayout; a separate grid dialog now edits GridLayout rules.

Fresh validation: **200 core checks, 188 UI checks and 10 native GridLayout checks
passed**. The native checks ran against the installed original MP1 1.39 Core.dll,
using a separate runtime copy and isolated directory configuration. See
[reports and reproduction notes](Validation/Grid-Preview/README.md).
The demo `Samples/Titan-Sample/demo.grid.xml` and its rendered screenshot were
visually inspected. Two remaining mixed-language compiler/error messages were
also translated into English.

## Group creation milestone

Added **Group selected controls** to the Element menu. Consecutive siblings on
the open XML page can be wrapped in a neutral ordinary group, including nested
group subtrees. Child XML, positions and IDs are preserved. Undo/redo restores
structure and selection; project persistence retains control identities and flags.
Automatic layouts, locked descendants, shared-source edits and behavioral group
defaults are protected.

Fresh validation: **211 core checks and 199 UI checks passed** after a successful
Windows build. See [group creation validation](Validation/Group-Creation/README.md).
These editor tests did not use Computer Use or interact with MediaPortal.

## Text presentation milestone

Corrected the two editor defects found during the attended Titan comparison:
imported title/clock/date text is now presented after control textures, and font
scaling converts the integer MP1 point size to pixels. Deferred text retains its
original clip, including collection viewports. No skin XML is rewritten.

Fresh build, **213 core checks and 199 UI checks passed**. The installed Titan
Settings preview was rendered and visually inspected: title, sample clock/date
are visible and text sizing is closer to the earlier client observation.
See [text presentation validation](Validation/Text-Presentation/README.md).
No new client input was used. Font substitution, runtime labels/plugin state and
a full matched-state image/exported-spacing comparison remain open.

## Neutral ungroup milestone

Added **Ungroup selected group** with direct-child promotion, preserved nested
XML/comments/processing instructions, structural identity remapping and exact
in-session undo/redo of XML and selection. Neutral wrappers created by the
grouping tool are supported. Behavioral/nonneutral groups, layout ancestors,
locks, hidden wrappers, source boundaries and known group-ID references are
protected. Saved projects retain the resulting snapshot and original-baseline
undo, under the existing project-format contract.

Fresh build: **221 core checks and 203 UI checks passed**. See
[ungroup validation](Validation/Ungroup/README.md). No MediaPortal input or
configuration changes were needed. The DirectX task remains deferred.

## Historical remaining work (October 18)

The preceding milestone added an **Edit grid layout** dialog with column
and row counts, independent spacing, fill direction, binding replacement
protection, and exact undo/redo. Child XML positions are preserved. Fresh
validation: **203 core checks and 195 UI checks passed**; the dialog render was
visually inspected. See [Grid dialog validation](Validation/Grid-Dialog/README.md).

1. Inspect and test collection geometry and dynamic-condition limitations at source/model level. The complex Titan Video model/persistence/export gate now passes; visual/runtime verification is suspended after the reported system freeze. See Validation/Complex-Page/README.md.
2. Improve preview fidelity for complex collections, font metrics, image masks, dynamic conditions and animations. Coverflow is currently a static approximation.
3. Extend design tools: move controls between groups and provide convenient global style editing.
4. Continue usability and stability checks on large real-world skins.

Latest client observation (October 18, approximately 19:43 local time): the user
manually started installed MP1 successfully and the Titan Home screen was
captured. Activating that window was rejected with `Computer Use was not
approved to use mediaportal`. Desktop input authorization currently blocks the
remaining settings/navigation comparison; no additional input was attempted.

## Latest continuation checkpoint

October 18, 2026, 20:04–20:05 Europe/Berlin: resumed from GitHub commit
`4d7764d` and verified all 125 tracked baseline files by Git blob hash.
A fresh build succeeded; fresh core validation is in
[Resume checkpoint](Validation/Resume-20261018/README.md). Editor source was not changed.

Activation of the user's running MediaPortal succeeded and Titan Home was
observed, but the repeated approval loop recurred despite Always allow.
All Computer Use calls have been stopped, including activation and capture,
under the owner's explicit stop instruction. Do not retry during this continuation.
Track the blocker at [openai/codex#55114](https://github.com/openai/codex/issues/55114).
Settings rendering, focus/navigation and spacing comparison remain pending.
The DirectX dependency replacement task remains deferred.

## Attended client retry

October 18, after 22:26 Europe/Berlin: the owner explicitly authorized another
attempt with manual approval. Activation and keyboard input succeeded; Titan
Settings opened, and first/last focus wrapping was verified without toggling
settings. Persistent Always-allow behavior remains unverified. The earlier stop
remains the default for unattended window-control retries.

An initial installed-skin/editor comparison found missing title/clock content and
smaller text in the editor. Full matched-state image comparison and exported
spacing validation remain pending. See [attended retry](Validation/Client-Comparison/Attended-Retry.md).
No original skin or client configuration was changed; DirectX remains deferred.

## Deferred backlog

- [Issue #1](https://github.com/gheitmann/MP1-Skin-Studio/issues/1): assess removal
  or replacement of legacy DirectX runtime dependencies in MediaPortal 1.
  Explicitly deferred by the user; do not investigate or implement now.

## Rules for continuing work

- Preserve original skins; use isolated copies and exports.
- Do not export preview-only state as skin XML changes.
- Distinguish inherited test reports from newly executed validation.
- The missing chat project list is independent of the source code saved here.
- Local window control was unavailable during import. Do not claim a visible client comparison has been completed.

## Real-page UI feasibility gate

October 18, 2026: attended editor control verified tree/canvas selection,
property changes, direct drag with grid snapping, exact undo/redo, project save
and reopen, and native-dialog ZIP export on installed Titan Settings. Five
independent artifact checks passed; original settings.xml is unchanged.
See [UI workflow](Validation/Core-Workflow/UI-Workflow.md). No product code
changed and the previous full-suite counts were not rerun. The UI-created
EDITOR TEST candidate was not loaded into MP1; the earlier STUDIO TEST candidate
already passed installed-client display/navigation.

The owner confirmed normal MP1 restarted successfully after another reported
DirectX startup error. No startup or DirectX investigation was performed.
The next core gate is a complex real list/facade page, with auxiliary features
still deferred. Historical authorization/startup blockers above are superseded
only by their explicitly recorded attended successes.

## Complex-page console gate and automation suspension

October 18, 2026: after the failed window-control attempt, the owner reported
that the PC became practically unresponsive and required a hard reset. Cause
is unconfirmed. Windows automation is suspended: no app launches, capture or
Computer Use retries. The owner authorized source-code and test work only.
This restriction supersedes earlier attended-retry permissions and is recorded
in AGENTS.md. DirectX replacement remains deferred.

The dedicated console probe passed eleven stages on real Titan myvideo.xml:
211 controls, 17 loaded sources, five facade views, selected sample data and
scrolling, isolated imported-list edits, exact undo/redo, project persistence,
ZIP export and unchanged original-source hashes. No production source changed.
See [complex-page model gate](Validation/Complex-Page/README.md).

The complex page's visible rendering and runtime behavior remain unverified.
Prior full-suite counts were not rerun. Continue with model-level investigation
of preview limitations; auxiliary conveniences remain lower priority.

## Filmstrip text geometry correction

October 18, 2026: fixed filmstrip title geometry ignoring textXOff/textYOff and
texture label dimensions. Installed Titan uses textYOff=-2000 intentionally;
its titles now remain outside the collection viewport in calculated geometry.
A regression probe failed before the fix and passed all six checks afterward.
The complex-page probe passed all eleven stages again. Fresh build and core
suite: 222 passed, zero failed. See
[geometry validation](Validation/Collection-Geometry/README.md).

No Windows automation, capture, GUI launch or MediaPortal launch was used.
Core tests ran through a console entry point; the UI suite remains suspended.
Visual equivalence is not established by numeric checks. Continue source/model
work on dynamic conditions and collection preview limits; DirectX stays deferred.

## Known-value string visibility conditions

Continuation begun October 18, 2026: the preview now evaluates two-argument
string.contains and string.starts when operands are known, using captured
sample/define scopes. Native trimming, case normalization and empty-search
semantics also apply to equality. Unknown values/functions remain unresolved.
The actual Titan scrolling popup was checked with explicit yes/no/missing flags.

Fresh console validation: ten dedicated checks and 223 core checks passed,
zero failures. See [string-condition validation](Validation/String-Conditions/README.md).
No app GUI, MediaPortal, capture or Computer Use was launched. UI/runtime
verification remains suspended; DirectX replacement remains deferred.

## Collection property editing

October 19, 2026: added compatible property-panel fields for list row height,
spacing, icon sizes, filmstrip texture/item/thumbnail sizes and small/large
thumbnail-grid sizes. Shared catalog definitions control type applicability and
numeric validation. Mixed selections expose only fields supported by all items;
existing protection, binding replacement, reset and transaction paths are reused.

Fresh build: 225 core checks passed. The extended real Titan console probe
passed all eleven stages, including row geometry after height/spacing changes,
exact undo/redo, project persistence and exported shared XML. Original source
hashes remain unchanged. See
[collection property validation](Validation/Collection-Properties/README.md).

The new GUI wiring compiles but has not been exercised or visually inspected.
No GUI/client launch, window capture or Computer Use was performed. Continue
model-level core work; manual/UI verification remains pending and DirectX deferred.

## Failed page loads preserve the working session

October 19, 2026: OpenPage now restores the existing active page on a loading
exception, including control identity, source cache, metadata and fonts. Missing,
malformed and out-of-root page requests no longer clear the working session.
A new regression failed before the fix and passes afterward. Fresh build and
console core suite: 226 passed, zero failed.

Nine real Titan multi-page console checks also passed: shared facade edits
appear on both video pages, three edited XML sources survive page switching,
cross-page undo/redo and save/reopen, and export contains all changes. Original
files remain unchanged. See [page loading validation](Validation/Page-Loading/README.md).
No GUI/client launch, capture or Computer Use was performed. Visual/UI/runtime
verification remains pending and DirectX replacement stays deferred.

## Visibility condition editing

October 19, 2026: added Visibility condition to the property panel, reusing
local reset/provenance, binding protection, multi-selection and transaction
behavior. Unknown runtime expressions are preserved and remain unresolved in
preview. Parent/include conditions continue to apply to empty local conditions.

Fresh console core run: 228 checks passed, zero failures, including new group
visibility/descendant/attribute/undo tests and unsupported-condition project/ZIP
preservation. See [visibility editing](Validation/Visibility-Editing/README.md).
The UI wiring is compiled but remains manually unverified. No editor/client GUI,
capture or Computer Use was used; the automation suspension and deferred DirectX
task remain in force.

## Unapplied property input guards

October 19, 2026: extended existing pending-field protection to page/theme and
session switching, save/export, undo/redo, insert/duplicate/alignment/navigation,
and editor protection/visibility refresh actions. Selector values are restored
on blocked changes; pending input marks the session unsaved and closing confirms
discard. No draft values are automatically committed or silently saved.

Fresh final build and console core suite: 228 passed, zero failed. This verifies
compilation and existing model regressions, not the new GUI event paths.
Manual acceptance cases are recorded in
[pending-input validation](Validation/Pending-Properties/README.md).
No GUI/client launch, capture or Computer Use was performed. UI validation
remains pending under the automation suspension; DirectX stays deferred.

## Combined session reliability checkpoint

October 19, 2026: session preparation now loads/validates pages, theme lists and
preview state before replacing the active editor project. Theme preparation
restores the prior theme on failure and preserves base edits/history on success.
Revert Inputs restores uncommitted fields without changing XML or undo history.

Fresh final build: 233 core checks and 39 targeted real-skin/model checks passed.
The latter include installed Christmas/default theme preparation and three-file
edit/save/reopen/export. Forced project/ZIP replacement failures retain usable
previous files, unchanged in-memory edits and no leftover temporary archives.
See Validation/Session-Reliability/README.md. No GUI/client launch, capture or
Computer Use was performed; interface paths remain manually unverified.

## Cached source ordinals, safe batch merging and review package

October 19, 2026: cached XML control ordinals remove repeated prefix scans while
loading a page. A bounded 1,600-control benchmark improved from 101.75 ms to
4.61 ms median for cached model loading; this does not measure GUI performance.
EditMany now merges compatible entries for one control, rejects conflicting
values before mutation and validates stale duplicates. A rejected batch retains
redo history; corrected edits after rollback remain usable.

Fresh build: 237 core checks and 39 targeted console checks passed. All original
real-skin sources remain unchanged. package-review.ps1 produced and verified a
manual review ZIP (24 files plus notes and SHA256 manifest) without launching it.
See Validation/Model-Performance/README.md. Recent GUI paths remain manually
unverified; desktop automation and DirectX work remain suspended/deferred.

## Windows continuation validation

October 19, 2026: restored GitHub commit `4fbc846` with all 198 tracked file hashes verified. Fresh Windows .NET Framework build, 237 core checks and 25 preview-expression checks passed. The editor launched and its Titan sample was observed through Computer Use. Broader interaction, persistent permission behavior and MP1 runtime checks remain pending; no client was launched and no original skin changed. See [Windows checkpoint](Validation/Resume-Windows-20261019/README.md). DirectX stays deferred.

## Windows test environment access confirmed

October 19, 2026: attended access test launched the editor and installed MP1, opened/closed the editor Sample Data dialog, moved Titan Home focus from TV to Videos and opened the real Videos page. Successive inputs and captures completed without an observed approval loop. This validates current-session access only; no export was installed and original skins were not edited. See [access test](Validation/Windows-Access-20261019/README.md). DirectX remains deferred.

## Attended Titan Video preview comparison

October 19, 2026: fresh complex-page probe passed all eleven stages on a complete isolated Titan copy. Attended UI checks loaded myvideo.xml, selected the concrete list, hid unverified conditions, loaded German language without prefixes and saved a preview-only project. Full SHA256 comparison: 2,050 skin files, zero mismatches. The list selection closes the simulated side menu and localization fixes numeric title IDs, but unknown metadata and six movie sample rows still differ from the client's single Videos folder. No export was installed; runtime comparison remains pending. See [comparison findings](Validation/Titan-Video-Comparison-20261019/README.md).

## Video folder preview milestone

October 19, 2026: added Element > Video folder preview and fixed initialization of derived collection values after rebuilding preview state. One Videos folder and explicit empty movie metadata are preview-only; unrelated unknown values stay unresolved. Fresh final Windows validation: 239 core checks, 25 expression checks and eleven real Titan model stages passed. Attended UI confirmed the folder, count 1 and matching selected title; save/reopen retained sample and language state. 89 unresolved conditions and remaining placeholders prevent a claim of visual equivalence. No export was installed. See [folder preview](Validation/Video-Folder-Preview/README.md).

## Known content and localized expressions

Added bounded Control.HasText/HasThumb evaluation for known preview content and L(...) resolution from saved language snapshots. Unknown/ambiguous inputs remain unresolved. Fresh validation: 245 core checks, 25 expression checks, eleven real Titan model stages and the targeted before/after probe passed. Hidden controls increased from 89 to 91, unresolved conditions decreased from 89 to 87; XML and all 17 loaded sources stayed unchanged. See Validation/Preview-Content/README.md. No fresh interactive UI/client test or export installation occurred.




