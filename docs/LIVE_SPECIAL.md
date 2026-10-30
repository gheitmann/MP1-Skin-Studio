# Live special controls — bridge 1.2 development

Bridge 1.3 additionally previews edited font families, sizes, styles and aliases
through the new Fonts dialog. See [live font definitions](LIVE_FONTS.md). The
1.2 validation below records the preceding special-control milestone.

This development build adds safely scoped checkbox, gallery, effect and shared
reference editing to the preview.3 workflow. It targets MP1 1.39. The published
preview.3 release is unchanged.

## Using the new controls

Connect and Open MP page. Checkbox properties now preview selected/unselected
image paths and mark width/height. Selected-state preview clears a previous native
expression temporarily and restores that expression on reset/disconnect. Protected
editor bindings still require explicit local replacement approval. Image paths
must stay within the skin; swapping animation XML templates remains unsupported.

Gallery properties distinguish small and large item, texture and thumbnail sizes.
Editing the visible mode updates its active dimensions and stored template; editing
the other mode changes its template without overwriting the visible layout. Native
items are retained. If both mode templates are indistinguishable, live changes stop
with an explicit message rather than guessing the active layout.

Select an element, then Element > Effects. Add fade, zoom or rotate effects with a
trigger, duration and start/end values. Existing local effects are retained as
structured entries. Local effects override shared ones; removing all local entries
restores inherited effects. Apply edits the model, previews supported effects in
MP and creates one Undo step. Effects on the Studio canvas remain static. WindowOpen
and currently applicable visibility/focus effects are queued without a page reload;
other triggers play when their native event occurs. Unsupported native overrides
are reported explicitly. Effect list objects are restored on disconnect.

Element > Shared defaults edits the selected element type's references.xml default
font, text color or checkbox images. Choose a font from the skin's loaded aliases.
Inherited values update across linked controls on the current page; explicit local
overrides stay intact. Shared edits have Undo and are included in Save skin copy.
Other pages are updated when opened and linked through the same editor session.

## Update and validation

Close MP once and install the included bridge 1.2 DLL with Install-LiveBridge.ps1.
Start MP again and connect this editor. The status must show bridge 1.2.0.0.
Thereafter supported edits do not require an MP restart or page reload.

Fresh checks: 478 core, 53 special native, 64 extended native, 17 complete-renderer
and 11 full-MP Studio workflow checks. Detailed scope is recorded under
Validation/Live-Special. Original installed skin XML remains untouched.

## Still outside this update

Bridge 1.3 supports installed Windows font families, heights, styles and alias
retargeting with private per-page previews. Global reference-cache replacement,
installing font files and arbitrary font schema remain outside this scoped path.
Plugin-specific effect implementations, shader/effect files and animation
XML texture templates are also outside the verified live scope.
