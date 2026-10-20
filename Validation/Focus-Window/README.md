# Collection focus alignment and dark native caption

Date: October 20, 2026. Parent: d31ffc0.

The reported DefaultWideHD myHome list has textureHeight 65, textYOff 30 and
textalign center. MP1 renders list captions with zero-height GUILabelControls;
center alignment subtracts half the measured text height from the Y anchor.
The editor previously used top alignment at that anchor and clipped text to the
remaining row height. It now uses the midpoint for fitting centered captions and
retains full font height for list/playlist captions. Overlong captions follow
the native left-alignment fallback. Other collection kinds and standalone label
geometry are unchanged. The collection viewport continues to clip deferred text.

Source references:
- [GUIListControl](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIListControl.cs), RenderText and AllocResources.
- [GUILabelControl](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUILabelControl.cs), centered alignment in Render.

The main window uses documented DWM dark-frame, caption, text and border colors
matching its header. The native frame remains responsible for caption buttons,
moving, resizing, snap and keyboard window commands. High contrast resets to
system colors. Unsupported attributes are harmlessly rejected on older systems;
missing entry points do not prevent the editor from starting. See Microsoft's
[DWMWINDOWATTRIBUTE reference](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

Fresh acceptance on executable SHA256
5d9c8e5e54d271dc4f98293a96cb169dcfe3dfb80b803327c2a64396f5a60b19:

- 422 core checks passed, including new pixel-rendered list/playlist midpoint,
  moved focused row, full glyph height and unchanged XML regressions.
- 23 focused sidebar/scroll scenarios passed, retaining dark overlay z-order,
  ranges, arrows/wheel/thumbs, selection, focus and session safeguards.
- Ten readable-preview, twelve complete demo and twelve clean-session scenarios
  passed against the extracted candidate; 488 installed XML, 26 demo assets and
  all 31 payload hashes stayed unchanged.
- Live Windows inspection confirmed the centered focused sample title, complete
  glyphs and dark native title bar on DefaultWideHD myHome. An attempted title-bar
  double click was followed by owner window activity, so maximize/restore is not
  claimed as attended acceptance. Owner windows and unsaved sessions were kept.

The executable in the delivered ZIP and live review window matches this hash.
Final packaging refreshes documentation/revision metadata and verifies every
payload entry. Prior broad UI, integrated-page and client evidence is historical.
No installed skin assets, desktop captures or private logs are committed.
