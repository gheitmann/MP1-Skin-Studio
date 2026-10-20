# Nested automatic layout navigation

Executed on October 20, 2026. Fresh results:

- **353 core checks passed**: nine new tests exercise cross-branch routes,
  entering/leaving a nested grid, hidden/collapsed ancestors, unknown visibility,
  unsupported wrappers, nonfocusable measurement siblings, the 16-level bound,
  explicit mapping priority and exact save/export/restored Undo/Redo. The former
  nested-grid rejection check now verifies supported nested navigation.
- **Four synthetic canvas KeyDown events passed through the production Windows
  form**, traversing 801 → 803 → 804 → 802 → 801 in demo.navigation.xml. Focus
  and designer selection agree. XML/history remain unchanged; saved focus
  restores to 801. The production form render was visually inspected.
- **Six complete real Titan regression workflows passed again**, across Movies,
  TV and Equalizer in Default and Christmas: width editing, save/reopen, exact
  export, normal and restored Undo/Redo. **162 automatic routes accepted,
  74 conservatively guarded; 13 loaded original XML hashes unchanged**.

The real Titan pages exercise existing non-nested navigation; they do not
establish nested runtime compatibility. Nested acceptance uses authored
fixtures and the bundled scene. Native bearing/distance math is unchanged;
the previous 6,561 comparisons were not repeated. No new attended keyboard
session or installed MediaPortal client run occurred.

## Behavior

Supported nested StackLayout/GridLayout groups share the existing arranged
static rectangles and geometric candidate-selection rule. A direct stack button
can enter a nested group; buttons in separate nested branches can navigate to
one another. Simple direct linear stacks retain their prior fallback. Explicit
nonzero mappings always win, including explicit invalid targets.

All automatic ancestors are checked, then participating root measurement
subtrees are recursively validated before candidate selection. Nonfocusable
siblings must have known geometry because they influence measured dimensions.
Hidden/collapsed ancestors exclude their entire target branch. Native ID-1
subtree exclusion and editor-hidden filtering remain in effect. Prospective
focus probes restore the original focus on success or failure.

Every participating group must use a supported automatic layout. Ordinary
wrappers, unsupported layouts, unknown visibility or measurement inputs,
ambiguous IDs, unsupported focusable types and runtime enable/hidden-focus
rules remain guarded. Hierarchies are limited to 16 group levels, pages to
2,048 logical controls, coordinates to one million, with the unchanged native
initial distance cutoff. No XML navigation links or calculated positions are
written. Geometry, runtime sizing and animation remain static approximations.

## Reproduce

Build with build.ps1 and run --selftest with an absolute output directory.
Run-Probes.ps1 compiles the two integration probes into an isolated output
directory. Supply an existing Titan skin copy; no MediaPortal process or
configuration changes are required. Form events are injected directly into the
test form, rather than sent to the desktop. Generated projects and demo renders
remain local working files, and are not shipped with the package.

```powershell
.\Validation\Nested-Navigation\Run-Probes.ps1 -SkinDirectory C:\SkinCopies\Titan
```

Core-Results.txt, Titan-Results.txt and Form-Results.txt contain the fresh reports.
The bundled demo uses repository-owned XML and existing Studio textures only.
