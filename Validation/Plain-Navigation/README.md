# Ordinary group and standalone button navigation

Executed on October 20, 2026. Fresh results:

- **361 core checks passed**, including eight new checks for absolute child
  coordinates, standalone/group transitions, plain wrappers around independently
  positioned automatic children, unsafe automatic-parent translation, hidden and
  unknown branches, group/ungroup Undo/Redo, exact persistence/export/restored
  Undo/Redo and property-driven empty layout fallback protection.
- **Ten complete real Titan workflows passed** in Default and Christmas across
  Yes/No, Rating, generic notification, Home exit menu and Movies Settings.
  **124 routes accepted: 26 automatic and 98 explicit. 20 routes guarded**.
  Existing dash/missing-target mappings retain explicit failure behavior.
- **Ten distinct loaded original XML hashes unchanged**, captured before edits
  and checked after navigation, width edit, save/reopen, exact export and both
  normal/restored Undo/Redo workflows.
- **Four synthetic canvas KeyDown events passed through the Windows form**:
  851 → 853 → 854 → 852 → 851 across ordinary group branches. Focus and designer
  selection agree; XML/history stay unchanged and saved focus restores to 851.
  The production form render was visually inspected.

Real-dialog routes mainly verify existing explicit mappings; the 26 automatic
routes come from Movies Settings. Authored fixtures and demo.free.xml exercise
the new automatic ordinary-group routes. No new attended keyboard or installed
MediaPortal client run occurred. Native math is unchanged and its previous
comparison was not repeated.

## Supported behavior and limits

Absent/zero button mappings can now use bounded geometric fallback at page
level or through ordinary group wrappers. Plain groups preserve absolute child
coordinates and can nest plain wrappers or independently positioned automatic
groups. This follows the no-layout path in
[GUIGroup.Arrange](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIGroup.cs),
which returns before arranging children when no layout exists. This source
check is not a claim of complete live runtime equivalence.

An ordinary wrapper below any automatic ancestor remains unsupported: the
preview must not assume that moving that wrapper translates its children.
Unsupported layouts and property-driven empty layout fallbacks are rejected.
Nonfocusable plain siblings do not influence positions and need no invented
measurement. Automatic measurement subtrees still require known sizes.

Explicit nonzero mappings retain precedence, including invalid mappings.
Unknown visibility/geometry, duplicate IDs, unsupported focusable types, runtime
enable/hidden-focus rules and existing node/depth/coordinate bounds stay guarded.
Native ID-1 exclusion and editor-hidden filtering remain. Animation and runtime
transformations remain approximate. No skin XML links or positions are generated.

## Reproduce

Build with build.ps1, then run --selftest with an absolute output directory.
Run-Probes.ps1 compiles TitanProbe.cs and FormProbe.cs into a separate work
directory. Supply a Titan skin copy; no client process/configuration changes
are needed. Synthetic events target only the created test form, not the desktop.

```powershell
.\Validation\Plain-Navigation\Run-Probes.ps1 -SkinDirectory C:\SkinCopies\Titan
```

Core-Results.txt, Titan-Results.txt and Form-Results.txt are the captured fresh
reports. Generated project archives and demo renders remain in local work
storage, outside the repository validation folder and review package.
