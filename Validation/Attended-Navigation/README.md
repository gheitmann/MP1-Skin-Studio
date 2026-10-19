# Attended editor navigation and saved-focus caption

October 19, 2026.

A fresh Windows editor run exercised actual checkbox/mouse input and arrow keys through Computer Use. BasicHome opened with TV (focus 4), Navigation was enabled, Down selected Radio (5), and Right selected the Plugins/Sonstiges tile (7). Visible selection, focused texture and source/property panel followed the route. Ctrl+S saved focus 7; closing and reopening the editor restored tile 7 without an unsaved marker.

The run exposed a caption refresh defect: focus changes were persistent editor changes but the title did not immediately show the unsaved asterisk. SelectControl now refreshes the caption after updating focus and diagnostics. The corrected build reopened at focus 7. After enabling Navigation, Left selected Radio (5) and the title displayed an asterisk. Ctrl+S removed it. Reading the saved manifest confirmed focusId 5, focusPage BasicHome.xml and zero changed XML sources.

The updated editor then opened the existing Default Settings matrix project. With Navigation enabled, Down moved from General (3) to GUI (4) through the automatic stack path; GUI became highlighted and the asterisk appeared. Ctrl+S cleared it. The saved manifest contains focusId 4 for settings.xml. This project already contained one position test edit; its edited XML remains exactly equal to the previous matrix export and its original source hash is unchanged. No source skin file was written.

The editor is left open on the saved Settings project with GUI focused. No native MP1 client run occurred. This is sampled interactive navigation/save/reopen validation; it does not cover every key, theme or runtime condition. The 258 core checks from the preceding milestone are historical and were not rerun for the caption-only refresh change.
