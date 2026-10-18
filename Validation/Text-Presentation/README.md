# Text presentation correction

Date: October 18, 2026. Baseline: c899a7364a5688ff432293a296da204f15acaf6e.

The attended Titan Settings comparison exposed missing title/clock text and
undersized text in the editor. These were editor-rendering defects; no original
skin XML, live client configuration or DirectX dependency was changed.

## Changes and evidence

The editor used to draw each control's text immediately. Titan imports its header
before an opaque background image, so the later image covered the already drawn
header. The editor now queues text for a final font pass, retaining text order
and each call's clipping region. Designer selection overlays are drawn afterward.
Collection clipping is retained rather than replaced by the final global clip.

MP1's font manager scales the legacy height against 576 lines and truncates it
to an integer. GUIFont then constructs a System.Drawing.Font in points. The
editor previously treated that value as pixels, omitting the 96/72 conversion.
The corrected size uses integer point scaling followed by conversion to skin
pixels at a normalized 96 DPI, independent of canvas zoom. For example a height
of 12 at 1080 lines becomes 22 points, approximately 29.33 pixels.

Primary implementation references, inspected October 18:

- [GUIFontManager.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIFontManager.cs): LoadFonts scaling/truncation and Present font submission.
- [GUIFont.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIFont.cs): Create uses System.Drawing.Font's point unit.

These sources explain the corrected static rendering behavior. They do not
establish pixel equivalence to the installed client or replace a full runtime
comparison.

## Validation

Fresh Windows build succeeded. **213 core checks and 199 UI checks passed**,
both with exit code 0. Full new reports: Core-Tests.txt and UI-Tests.txt.

New regression tests render an imported title before a later opaque image and
verify the title remains visible, while the background still renders. Another
test changes the graphics clip after queuing text and verifies the original clip
is retained. Font checks cover point conversion and integer truncation.

The installed Titan settings.xml was rendered again with German resources. The
new image was visually inspected: title, sample clock and sample date now appear,
and text is larger while button geometry remains unchanged. No new MediaPortal
window automation was needed. The earlier attended client observation is the
reference; there was no fresh pixel-aligned client capture.

Remaining limitations: unavailable font families are substituted, time/date and
plugin visibility use sample state, runtime menu-label overrides may differ, and
animations/masks remain approximate. The exported spacing-change client test is
still pending. The approval persistence issue is unresolved; DirectX replacement
remains explicitly deferred.
