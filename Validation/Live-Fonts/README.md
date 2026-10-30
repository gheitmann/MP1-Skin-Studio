# Private font definition previews

October 30, 2026. MP1 1.39 SDK, bridge 1.3.0.0.

The editor changes fonts.xml in its source-preserving model, including installed
family, integer skin height, bold/italic and alias targets. Undo/project persistence
and output-copy saving use the ordinary XML transaction path. Model tests are part
of the final 489-check core suite; originals are not written by a live preview.

NativeProbe uses an injected private font factory to exercise cache identity,
capacity, original-registry preservation, font restoration and registry invalidation
without allocating renderer resources. The 17 passing checks are not visual proof.
The factory is a private implementation field changed only inside that test process;
there is no protocol switch that disables real initialization validation.

SceneProbe runs against a complete isolated official MP installation and the fixture
window 90120 from Live-Special. ScenePlugin adds observer metadata for the original
font object, family/size, registry count and native gallery item count. Twelve checks
cover shared private variants on label/checkbox/gallery, family/style identities,
cache reuse, original object preservation, unavailable-family rejection, reset and
disconnect. Changed larger text was visually inspected in the complete renderer.

StudioProbe drives the editor's own Fonts dialog and real live connection. Eight
checks cover applying a shared definition, all linked users, unrelated font retention,
no page reload, original disk preservation, project save/reopen and Undo. Its dialogs
are controlled only inside the owned probe's event loop. No owner session is discarded.

Build probes against the matching editor/bridge and official SDK. Use the existing
isolated client/skin under work/Live-Runtime; never install ScenePlugin in an owner's
MP. Copy the new bridge into that copied client's plugins/process. Compile ScenePlugin
as SkinStudio.SpecialFixture.dll there, replacing only the old isolated test fixture.
Compile SceneProbe with src/LiveProtocol.cs; compile StudioProbe beside the editor
with System, System.Core, System.Xml, System.Drawing and System.Windows.Forms.
Set MP1_SKIN_STUDIO_LIVE_DIR to a separate IPC directory for MP and both probes.
SceneProbe takes IPC and fresh result directory; StudioProbe takes skin root, IPC
and fresh result directory. Run the scene probe first to activate window 90120.

Implementation relies on existing MP font APIs; the engine is not replaced.
GUIFontManager.LoadFonts disposes the whole registry and is deliberately not called.
Private GUIFont instances use unique cache names incorporating family/height/style
and rendering scale. Original names/objects are never replaced. The existing SDK
system-font fallback path supplies glyphs; this avoids depending on scarce atlas
slots. MP text contexts are invalidated before using a changed private font.

Variants stay in MP's normal font registry until its usual resource refresh/shutdown,
so a cached visual cannot retain a disposed preview font. The pool is limited to 32
variants per registry; identical definitions reuse one entry. Registry changes start
fresh live epochs and drop obsolete cache identities. Native fonts that were replaced
by MP are never reassigned from obsolete saved pointers. Unavailable families and
scaled fonts above 256 points are rejected before any control is changed. A failed
frame may warm a valid bounded font-cache entry, but does not replace original fonts
or partially apply control overrides.

Primary source/API review was cross-checked against the installed SDK:
https://github.com/MediaPortal/MediaPortal-1/tree/master/mediaportal/Core/guilib
https://github.com/MediaPortal/MediaPortal-1/tree/master/DirectShowFilters/fontEngine/source

No installed skins, font files, cache files, desktop captures, owner config or runtime
logs are published as source evidence. Local captures are kept only in outputs.
