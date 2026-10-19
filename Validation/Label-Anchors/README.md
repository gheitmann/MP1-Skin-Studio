# Video sample state and standalone label anchors

October 19, 2026.

Standalone label/fadelabel rendering now separates the XML text anchor from the designer selection rectangle. Zero or omitted dimensions no longer use fabricated designer extents for text placement. Center alignment uses horizontal and vertical text extents; right alignment uses the XML X position as the right edge. Explicit center dimensions and automatic layout extents are retained. Collection rows, buttons and multiline text keep their existing rendering path. GDI+ font measurement remains approximate.

Behavior was checked against the upstream [GUILabelControl rendering](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUILabelControl.cs) and [GUIFadeLabel](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIFadeLabel.cs). No upstream implementation was copied.

The explicit Video folder sample now specifies watchedcount=0, watchedpercent=0 and scrolling.up/down=no. In the installed Titan source this selects its unwatched icon and disables scroll-popup state. This is an editor sample, not a claim about live client properties. Other unknown inputs stay unknown.

## Fresh validation

248 core checks passed, including standalone center/right anchors through actual designer bounds. The targeted Titan probe passed: 211 controls, 115 hidden, 62 unresolved (previous sample: 87 unresolved); saved list focus 50 and content conditions survive project roundtrip. All 17 loaded original source hashes, XML signature and undo history remained unchanged. The offscreen production render was visually inspected: header clock/date remain at the right, selected-item text is centered in the footer and the explicit unwatched icon is visible.

The clock and date are fixed samples. Fonts, animation, clipping edge cases and maxWidth autosizing remain approximate; 62 unresolved conditions remain. No new interactive MP1 client run or export installation was performed. Prior installed-client validation remains historical.
