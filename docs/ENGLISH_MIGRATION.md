# English migration

Date: October 18, 2026.

The repository now uses English paths, documentation, validation descriptions,
application captions, error messages and generated artifact names.

## Main path changes

| Previous path | Current path |
| --- | --- |
| Pruefungen | Validation |
| Pruefungen/MP1-Nativ | Validation/MP1-Native |
| Runtime-Abgleich | Runtime-Comparison |
| Samples/Titan-Auszug | Samples/Titan-Sample |
| README.txt | docs/USER_GUIDE.md |
| demo.ebenen.xml | demo.layers.xml |
| demo.gruppen.xml | demo.groups.xml |
| demo.sammlungen.xml | demo.collections.xml |
| demo.sprache.xml | demo.language.xml |

Related code references, sample texture names and export instructions were updated.
The XML data in the prepared Titan runtime export remains byte-identical.

Search accepts English `type:` and `source:` filters. Existing German aliases remain
accepted for compatibility with earlier queries.

Dedicated language and Unicode fixtures intentionally retain multilingual text.
External MediaPortal XML identifiers and original external skin filenames are not
translated because their exact identity is part of compatibility testing.

Historical reports were translated while preserving their results.
Fresh migration checks are saved separately in Validation/English-Migration:
189 core checks and 185 UI checks passed, and the application builds successfully.
The final core-only rebuild changed an escaped XML test expectation; production
application code is identical to the successful UI-test build.

The UI harness re-shows the editor after hidden process startup. Keyboard UI tests activate the editor before setting focus so that a previously
closed test window cannot leave the F2 test without a keyboard target.

The original local release folders and Git history remain available.
