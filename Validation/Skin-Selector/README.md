# Automatic skin selector and wider dark sidebar

Date: October 20, 2026. Parent: 7a9feef.

SKIN above THEME now lists immediate skin directories under the standard Windows
MediaPortal locations in CommonApplicationData, ProgramFilesX86 and ProgramFiles,
plus the current skin's siblings and the bundled demo. Candidates require
references.xml and at least one page XML; the current active folder is retained
even when it lacks references. Discovery does not recursively scan themes/drives,
parse every skin or change configuration/files. Missing/inaccessible locations
are skipped. Paths are deduplicated without case sensitivity and duplicate names
are disambiguated. Opening the selector refreshes newly added folders.

The selected skin loads through the existing session transition: pending fields
block switching, unsaved work offers Save/Discard/Cancel, a cancelled transition
restores the selector and a failed load retains the prior project. The current
page filename is preferred when present in the destination; otherwise normal
Home/BasicHome fallback applies. Theme selection remains independent. External
locations can still be opened with Open skin directory; no recent-folder list
or persistent application preference has been introduced.

The complete left column grows from 245 to 319 pixels (30%, rounded), with
anchored skin/theme/filter controls filling the extra space. Preview tools wrap
at minimum width. The page/tree divider is retained.

## Scrollbar implementation

DarkScrollHost places dark, owner-painted chrome over the native vertical and
horizontal bars of the main page list, tree, properties and information text.
Native content viewport/ranges and keyboard behavior are retained. Arrows, page
clicks, thumb dragging, wheel forwarding and hold-repeat operate without focus
or selection changes. The tree vertical thumb uses TopNode/visible rows; list,
panel and text thumbs use their existing native/managed scroll positions.
Geometry changes are coalesced through one pending UI callback, without an idle
polling timer. Native hooks and repeat timers release with their controls.

The implementation uses documented [GetScrollBarInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getscrollbarinfo)
geometry and [GetScrollInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getscrollinfo)
ranges. Classic horizontal scroll messages only carry 16-bit positions; an
exceptional list/tree horizontal range beyond 65,535 retains its OS scrollbar
instead of truncating positions. Dialogs keep their existing controls; this
milestone styles the four main content regions. No global Windows theme,
undocumented dark-mode API, client renderer or skin XML is modified.

## Fresh acceptance

- 420 core tests passed, including four discovery cases for direct children,
  nested/incomplete exclusions, custom siblings/demo, aliases/duplicate names
  and active folders without references.
- 22 focused form/menu/mouse scenarios passed: all six installed folders plus
  the demo; switching and live refresh; pending drafts; cancel/discard; invalid
  skin rollback; 319-pixel layout/default/minimum bounds; wrapped tools; arrows,
  wheel and page/tree thumb movement in both axes; property/text scrolling;
  selection/focus/draft/XML preservation and dark chrome on all four regions.
- Twelve existing integrated-page scenarios passed, including their menu
  callbacks, exact export/restored Undo/Redo, wheel routing and disposal checks.
  Their ancestor lookup now traverses the scrollbar wrapper rather than assuming
  a fixed number of parents.
- Ten readable-preview scenarios, twelve complete demo workflows and twelve
  clean-session workflows passed against the extracted package.
- All 488 installed XML, 26 demo assets and 31 manifest payload hashes remained
  unchanged. Default and minimum-size synthetic form renders were inspected.

Tests use synthetic forms/events, not new attended desktop input or installed
client acceptance. Historical caption-selection, broad 205 UI and responsiveness
reports are not presented as newly executed checks. The first scroll probe found
that a native TreeView thumb-position message did not move its viewport; the
production adapter now uses visible TopNode movement and the complete focused
suite passed. Harness startup also explicitly shows/activates its isolated form
so its focus-preservation assertions exercise a real focused field.

```powershell
.\Validation\Skin-Selector\Run-Probe.ps1 -PackageDirectory C:\ReviewPackage `
  -OutputDirectory C:\FreshOutput -SkinDirectory C:\DefaultWideHD
```

Accepted executable SHA256:
166be66da4dfa4139923bef177844fbdde18cea22b9378559321252b4762afd7

The final review ZIP retains that exact executable and sample bytes. Guides and
revision metadata are refreshed after acceptance, and its manifest is checked.
