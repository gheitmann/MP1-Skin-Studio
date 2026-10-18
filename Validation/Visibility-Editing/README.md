# Visibility condition editing

Date: October 19, 2026, Europe/Berlin. Baseline: c27f9b96229b658091c1d83e8f03303d19f7f7e3.

Added the visible property to the existing property panel as Visibility
condition. It uses the same local provenance/reset, multiple-selection,
binding-replacement and locked-element protection as other editable properties.
Apply uses the existing transaction and reload path, so changed group conditions
are reflected in descendant visibility terms.

An empty expression adds no local condition restriction; parent/include rules
still apply. Unsupported runtime expressions are accepted as XML text and remain
unknown in the preview, rather than being guessed or rewritten. The tooltip
explains this limit. This is a condition editor, not a complete MP1 expression
syntax validator. The native Visibility enum and condition attributes are not
new editable fields in this checkpoint.

Fresh validation: **228 core checks passed, zero failures**. The added model
regressions exercise:

- Group visibility edits with an included child, preservation of the existing
  allowhiddenfocus attribute, and exact XML undo/redo with updated descendants.
- An unsupported control.hasthumb(50) condition staying unknown and surviving
  project save/reopen and ZIP export unchanged.

Core-Tests.txt records this run. The interface wiring compiles but was not clicked
or visually inspected. No UI smoke suite, editor GUI, client launch, Computer Use
or window capture was used. Manual UI and runtime verification remain pending
under the automation suspension. DirectX replacement stays deferred.
