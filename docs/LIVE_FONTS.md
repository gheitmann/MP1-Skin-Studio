# Live font definitions — bridge 1.3 development

Open Fonts … in Studio's Live bar. Select a font definition and choose an installed
Windows family, skin height and bold/italic style. Apply definition updates the editor
canvas and supported linked MP elements without restarting MP or reloading its page.
This is one Undo step. A shared definition affects all its static uses and aliases;
explicitly different font definitions remain unchanged.

New font adds a separately named definition. Assign its name in element properties
when you want to use it. New alias creates a name that points to an existing font;
existing aliases can be retargeted. Cycles, duplicate names and locked affected
controls are rejected. The family list is searchable. No raw XML editing is needed.

Save Project retains the edited fonts.xml and Undo history. Save skin copy includes
the changed definitions in a separate complete output skin. Live preview does not
write the source fonts.xml. Undo, Revert and Disconnect retain their ordinary roles:
Undo restores model changes; Disconnect restores the original native MP fonts.

## How the preview stays isolated

MP's original font objects and aliases remain registered. Edited definitions get
private, reusable preview variants for the linked controls. Unrelated MP controls
keep their original fonts. Other pages receive previews when opened and linked in
the same editing session. The normal global font-loader is not invoked.

Requires the included bridge 1.3. Update it once with MP closed, then start MP and
reconnect. Ordinary font definition edits thereafter do not require another restart.
Older bridges reject definition previews with an update message while retaining
normal supported live properties. The published preview.3 release is unchanged.

## Bounds

Families must already be installed in Windows; the editor does not install fonts or
load arbitrary font binaries. Supported declarations are family, integer height 1–96
(in MP's 576-line scale), regular/bold/italic, new names and alias targets. Scaled size
is limited to 256 points. Font effects, arbitrary new XML schema and opaque dynamic
font expressions are outside this verified scope.

The preview cache holds up to 32 distinct variants per MP font registry. Repeated
parameters reuse the same variant. If full, save the project and restart MP before
creating more variants. Existing fonts are kept valid until MP's ordinary refresh
or shutdown. This bound prevents unbounded resource growth during experimentation.

Fresh validation: 489 core checks, 17 native cache/lifecycle checks, 12 complete-MP
font checks and 8 Studio-to-MP font workflows. See Validation/Live-Fonts for exact
scope. All original installed skin XML files remain unchanged.
