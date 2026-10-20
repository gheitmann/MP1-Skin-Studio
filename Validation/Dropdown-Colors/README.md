# Dropdown colors matching the scrollbars

Date: October 20, 2026. Parent: 841893e (folder-selection menu action).

ChoiceBox fields and StudioMenu popup backgrounds use RGB 20/26/35, matching the
scrollbar track. Popup selections use 63/78/96 and accents use 92/116/143, matching
the scrollbar thumb states. Text, arrows and checks are light. The shared menu
renderer also styles property/toolbar/context menus; high-contrast menus retain
the system renderer. No menu lifetime or callback-disposal logic changed.

View and MediaTip Example now use ChoiceBox, removing their native light combo
chrome. Collection mode and preview preset fields use the same dark background.
Index/value mappings, preview state, session guards and XML remain unchanged.

Fresh final checks on executable SHA256
006e9952398badbd40c6b9292a9d9ef2b484672816203418815a5bda69df1470:

- 25 selector/sidebar/menu/scroll scenarios passed (Selector-Results.txt).
- Ten readable-preview scenarios passed with the new ChoiceBox fields, including
  all example/view switches, pending-input protection, save/reopen/export, exact
  source preservation, isolation and minimum-width bounds (Readable-Results.txt).
- All 488 installed XML and 26 demo hashes stayed unchanged.
- Live Windows inspection confirmed the closed View field now matches the dark
  selectors. An open-menu click was interrupted by concurrent owner input;
  no new live open-menu acceptance is claimed. Original sessions were kept.

Earlier core/copy/demo/client results are historical. The review ZIP manifest
checks every payload entry; no installed skins or desktop captures are bundled.
