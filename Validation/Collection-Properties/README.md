# Editable collection dimensions

Date: October 19, 2026, Europe/Berlin. Baseline: b224feadb58968beb6597e69b4105fd0d373f784.

The model could edit collection row/image dimensions, but the property panel did
not expose them. This prevented a normal editor user from changing fundamental
list density and thumbnail sizing without editing XML elsewhere.

The property panel now appends compatible fields from CollectionProperties:

| Element type | Additional fields |
| --- | --- |
| List / playlist | Row height, row spacing, icon width/height |
| Filmstrip | Texture width/height, item width/height, thumbnail width/height |
| Thumbnail panel | Item and thumbnail width/height, separate large-icon sizes |

Facade wrappers and coverflow have no fields from this catalog. A concrete
subview must be selected in the element tree. For mixed selections, additional
fields appear only when every selected element supports them. Existing common
fields, including text offsets, remain available as before.

Dimensions accept positive integers; row spacing accepts nonnegative integers.
Changed fields pass through existing binding-replacement protection, locked-item
checks, local reset/provenance, one transaction, and undo/redo. Numeric validation
occurs before applying the transaction. No project format was changed.

## Fresh validation

- Fresh build and **225 core checks passed**, zero failures. New catalog tests
  cover supported types, exclusion of facade/coverflow, unique keys, labels,
  zero/negative/fractional/overflow/binding/nonnumeric input.
- The real Titan complex-page console probe passed all eleven stages after
  extension to change row height to 66 and row spacing to 12 alongside width
  and text offset. It verifies 66-pixel rows and 78-pixel pitch, preserved
  inactive-view XML, exact undo/redo, project save/reopen, ZIP contents and all
  original source hashes. See Complex-Page-Results.txt.
- The interface wiring is compiled and reviewed, but no property-panel clicks,
  visible layout inspection, UI smoke suite, capture or Computer Use were run.
  The new interface behavior remains pending manual verification under the
  owner's automation suspension. Model/catalog checks do not establish GUI QA.

No editor GUI or MediaPortal was launched. Earlier visual/runtime results apply
to their recorded candidates only. DirectX remains deferred.
