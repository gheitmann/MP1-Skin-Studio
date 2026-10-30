# Special controls and shared definitions acceptance

October 30, 2026. Official MP1 SDK 1.39.0.0; development bridge 1.2.0.0.

Fresh final core suite: 478 passed. Native-Results.txt contains 53 focused native
checks, including checkbox binding/texture/dimension restoration, small/large
native gallery templates, exact effect-list restoration, bad-input rejection and
compound-frame atomicity. The existing extended adapter suite also passes its
64 checks with an initialized small-gallery template fixture.

Real-MP-Results.txt contains 17 complete-renderer checks. Changed checkbox images,
small-gallery sizing and a fade effect were visually inspected. The large-gallery
layout was inspected after restoration. A synthetic six-item gallery survives
small/large layout rebuilds, mode switching, reset and disconnect. Desired geometry
allows one skin pixel of scale conversion; restored captured values match exactly.
These checks preceded the final checkbox selected-expression adapter, which was
then separately exercised in the final 53 native checks. No all-skin compatibility
claim follows from this fixture.

Studio-Results.txt contains 11 full-MP Studio workflows: the Effects dialog applies
structured native effects, Undo restores them, Shared defaults changes an inherited
loaded font alias, local overrides remain intact, Undo restores the native alias,
and original source files remain unchanged. Acknowledgement-based waits avoid
polling snapshots faster than the coalesced frame timer can send a preview.

Prepare.ps1 compiles the fixture and probes only beneath this repository's work
folder. Use an isolated official client with a complete copied DefaultWideHD skin.
Set MP1_SKIN_STUDIO_LIVE_DIR to an isolated IPC directory for MP and probes.
SpecialNativeProbe takes a fresh output directory; it does not initialize a GPU.
Start the isolated client and run SpecialSceneProbe with the IPC and a fresh output
directory. It activates fixture window 90120 and holds small/large scenes for visual
inspection. The fixture's special-mode.txt command is test-only and is consumed
on the MP GUI thread. Run SpecialStudioProbe with the skin root, IPC and a fresh
output directory while window 90120 is active and no other Studio is connected.

The initial headless texture check exposed a missing unallocated-resource guard;
the final adapter updates unallocated image names without allocating graphics
resources. Allocated-image replacement was subsequently tested in the real MP
renderer. Failed intermediate runs are not published as acceptance evidence.

No installed skins, assets, owner configuration, native captures or private runtime
logs are published here. Source readers inspected the official GUICheckMarkControl,
GUIAnimation, GUIThumbnailPanel, GUIControl, GUIControlFactory, GUIFontManager and
GUIFont implementations and cross-checked field names against the installed SDK:
https://github.com/MediaPortal/MediaPortal-1/tree/master/mediaportal/Core/guilib

The normal GUIFontManager.LoadFonts API disposes the whole registered font set.
The individual GUIFont.Load path removes its native font registration and rebuilds
resources; its persistent cache key omits family/style. A safe arbitrary fonts.xml
hot-reload needs coordinated font identity, cache and window/control lifecycle
handling. This package does not replace that global registry. Loaded font aliases
and supported references.xml defaults preview on the connected page; new font
families/sizes, alias-table changes and cached unrelated pages still need a normal
MP skin reload/restart. See docs/LIVE_SPECIAL.md for user-facing scope.
