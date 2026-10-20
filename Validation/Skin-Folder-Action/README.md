# Skin dropdown folder-selection command

Date: October 20, 2026. Parent: 3867413.

Select different Skin Folder is appended after a separator below discovered
skins. ChoiceBox supports optional menu actions without putting them in its
selection values. Only the SKIN selector attaches this action. It invokes the
same OpenFolder flow as the existing toolbar command; the dialog starts at the
current skin, is owned by the editor, and existing pending-property and unsaved
session guards remain in force. Cancel leaves the selected skin unchanged.

Fresh focused validation: 25 scenarios passed (UI-Results.txt), including the
actual skin-menu item callbacks, separated footer, reopening without duplicate
actions, selection stability and unchanged theme menu. All 488 installed XML
hashes stayed intact. Earlier 430 core, copy, demo/session, readability and client
reports are historical; the full suites were not repeated for this small change.
Final packaging verifies every payload hash and includes no installed skin assets.

Accepted executable SHA256:
3d07b8cbc25553b4323a391fa6f5a3ed400ea90ba02ca858bd71ae8821714ace

The new review window was started and its main layout inspected. A dropdown
click was interrupted by concurrent owner input; no new attended folder-dialog
acceptance is claimed. The existing folder-selection implementation is reused.
