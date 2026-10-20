# Bounded automatic grid navigation

Executed on October 20, 2026. Fresh results:

- **344 core checks passed**, including twelve new grid-navigation checks.
- **Six complete Titan workflows passed** across Movies, TV and Equalizer in
  Default and Christmas: width edit, save/reopen, exact XML export, ordinary and
  restored Undo/Redo, with saved simulated focus retained.
- **162 automatic routes accepted; 74 routes conservatively guarded** across
  five pages in each theme. Hidden TV sources and unresolved Keypad/Remote label
  height prevent those routes. A guarded route is not a runtime-error finding.
- **13 distinct loaded original XML hashes unchanged**, captured before edits
  and checked after workflows. Installed skin files were not edited.
- **6,561 native bearing and distance comparisons passed** against installed
  original MP1 Core.dll 1.39.0.0. Only static math methods were invoked through
  reflection; this does not test the complete live GUIControl.Navigate method.
- **Six synthetic canvas KeyDown events passed through the production form**:
  901 → 902 → 904 → 903 → 905 → 904 → 902. Designer selection and simulated focus
  agree. XML/history remain unchanged; saved project restores focus 902.
  The production demo form render was visually inspected, including blue focus
  and readable captions. A final footer shortening was checked in that targeted
  form probe; unchanged engine suites were not repeated for wording.

No new attended keyboard session or installed MediaPortal client run occurred.
Previous image and attended acceptance reports remain historical.

## Behavior and limits

An absent/zero arrow mapping on a direct button in a top-level GridLayout uses
the current arranged static bounds. Nonzero explicit mappings retain precedence,
including explicit missing/hidden targets; they never fall back silently.

Selection follows the native rounded top-left bearing, direction filters,
rounded distance, document order and simultaneous score replacement rule.
It can choose diagonals in an incomplete row; ties choose the later candidate.
This is not a new row/column index algorithm. The source was compared with
[GUIControl.Navigate, CalcBearing, CalcDistance and FlattenHierarchy](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIControl.cs).
The locally installed original library independently confirms the math.

Eligible standalone buttons and direct buttons in top-level StackLayout or
GridLayout peer groups participate, preserving cross-group candidates. Native
ID-1 subtree exclusion is retained. Known-hidden/editor-hidden candidates are
skipped. Existing prospective-focus visibility checking remains scoped and
restores focus even on rejection. No XML links are generated.

Unique positive IDs, known visibility and finite static geometry, alignment
and margins are required. Nested groups, unsupported focusable types including
collections, runtime enable/hidden-focus rules and unknown inputs are rejected.
The logical page is capped at 2,048 controls and geometry at one million;
the native initial 10,000-distance cutoff remains. Linear stack fallback stays
separate. Animation, runtime-sized content, plugin-generated controls and live
layout rounding remain outside the equivalence claim.

The bundled demo uses five ordinary buttons with explicit focused textures.
No original Titan XML, textures, references, language files or MP1 configuration
are included in this validation folder or the review package. Local project
archives and form renders remain in the working directory.

## Reproduce

Build with `build.ps1`, then run `--selftest` with an absolute output directory.
`Run-Probes.ps1` compiles the three console probes into an isolated output
directory and runs the Titan, original-math and synthetic-form checks. Supply a
skin copy and the original Core.dll path; the probes do not launch MediaPortal.

```powershell
.\Validation\Grid-Navigation\Run-Probes.ps1 `
  -SkinDirectory C:\SkinCopies\Titan `
  -MP1Core 'C:\Program Files\Team MediaPortal\MediaPortal\Core.dll'
```

`Core-Results.txt`, `Results.txt`, `Native-Results.txt` and `Form-Results.txt`
are the fresh captured reports, with the guarded cases explicitly recorded.
