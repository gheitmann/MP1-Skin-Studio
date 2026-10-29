# Live preview in MediaPortal 1

Live preview sends valid Studio inputs directly to the GUI controls already
displayed by MediaPortal. Text and geometry do not wait for Apply, XML export,
page reload or a MediaPortal restart. Canvas gestures stream while dragging.
The Studio canvas remains an approximate editing aid; the running MP window
shows the native control rendering and its real plugin values.

## Setup once

This build targets the installed **MediaPortal 1.39** SDK on Windows with .NET
Framework 4.x. Keep the complete extracted package together.

1. Close MediaPortal for the initial plugin installation.
2. Copy `LiveBridge/bin/MP1SkinStudio.LiveBridge.dll` into MediaPortal's
   `plugins/process` directory. Alternatively run
   `LiveBridge/Install-LiveBridge.ps1` from PowerShell with sufficient rights for
   that installation folder. Add `-RequestElevation` to request the normal
   Windows administrator confirmation. Use `-MediaPortalDirectory` for a custom location.
   Updating an existing DLL retains a dated backup. `-Uninstall` removes only
   this plugin. Neither operation edits skin XML or MediaPortal configuration.
3. Enable **MP1 Skin Studio Live** under process plugins if it is disabled.
4. Start MediaPortal once. Its standard configuration application must be
   closed, as required by MP's own startup check.
5. Open the **same skin directory and theme** in Studio. Use an independent skin
   copy in both programs when experimenting. Open the XML page displayed in MP.
6. Enable **Live** beside Toolbox, select the running MP instance and Connect.
   **Show page** optionally navigates MP to that XML window. Ordinary edits do
   not navigate, reload or restart it.

The status line identifies linking or source mismatches, invalid inputs and
unsupported properties. An acknowledgement time measures the request/response
round trip; it does not measure display latency or promise a fixed frame rate.

## Editing and keeping changes

Valid text/property drafts and canvas movement update the connected MP control
after a short coalescing interval. Intermediate invalid numbers retain the last
valid preview. Apply commits inputs to the editor model; it does not restart MP.
Revert Inputs removes uncommitted previews. Undo/Redo follows the editor model.

**Disconnect restores the native runtime state from before the live session.**
The saved output skin copy remains on disk. Editor changes remain available.
A dropped connection, an editor crash, a page
change or plugin shutdown also restores preview overrides. A busy MP GUI can
delay restoration until it processes messages again. Runtime previews are never
written to skin files by the plugin. Use **Save skin …** beside Live to create
and update a separate complete output skin with review, conflict checks and
backups. See [Save a complete skin copy](SAVE_SKIN_COPY.md). Save an editor
project to retain Undo history and the output location. Export
alone does not replace the live session's original runtime baseline.

## Supported scope

The bridge supports the native properties exposed by each MP control: position,
size, text, loaded font, text/diffuse/disabled color, supported alignments and
offsets, image/button textures, image aspect/crop settings, selected/visible
state, navigation and hyperlinks, and supported collection item dimensions.
MP language-number labels are resolved by MP's own localization service.
Bound labels and editor locks retain Studio's normal protections.

The snapshot advertises the properties actually writable on a control. Some
specialized plugin controls have read-only or private template properties; a
change to one of these is reported and is not silently approximated. Positions
managed by an automatic MP group layout cannot be overridden independently.
Font changes require that font to be loaded by MP. Texture paths must remain
relative to the active skin's Media folder. Shader/effect code, fonts.xml,
references.xml and arbitrary plugin behavior are not hot-reloaded.

Labels, buttons, images and ordinary groups can be inserted or removed live.
Structural changes require unambiguous sibling mapping; runtime-generated
elements and ambiguous IDs can require **Link element** or prevent that live
structural operation. Property edits remain available. This protection avoids
moving unrelated runtime controls. Copied controls with advanced XML outside
the supported live template fields should be verified through a normal export.

IDs are not treated as unique: descriptions and geometry disambiguate them,
including repeated ID 0 controls. Ambiguous edits require an explicit link.
Skin, theme, active page, original XML hashes and runtime control identity are
checked before changes. Switching page/theme invalidates the old mapping.

## Connection and diagnostics

Communication uses an authenticated local named pipe restricted to the current
Windows user. There is no network listener. Endpoint discovery files reside in
`%LOCALAPPDATA%/MP1SkinStudio/Live`. Normal desktop execution is required;
restricted automation sandboxes can block pipe connections even when both
processes run under the same account. Avoid running Studio and MP under
different users. The `MP1_SKIN_STUDIO_LIVE_DIR` process environment override
exists for isolated validation and is normally unnecessary.

Plugin startup/connection failures are logged with the `MP1 Skin Studio Live`
prefix in MP's own log. Studio displays protocol and validation errors inline.
The protocol limits packet sizes, control counts, nesting and new controls;
XML external entities and paths leaving the skin are rejected.

## Validation status

See `Validation/Live-Bridge/README.md` for executed checks and the distinction
between real MP startup, native SDK tests and Studio integration in a test host.
