# Save a complete skin copy

**Save skin …**, beside Toolbox and Live, keeps your XML edits in a separate
complete skin. The current source, Studio undo history and active MediaPortal
preview continue. Valid pending property inputs are applied through the usual
binding/lock checks before opening the save dialog.

## First save

1. Click **Save skin …**, then **New skin copy …**.
2. Enter the new skin name and choose an existing writable parent directory
   outside the current source. Existing folders are never replaced.
3. Create the copy. All skin images, fonts, themes and XML are copied, including
   applied edits from shared files and earlier loaded pages. Large copies run
   in the background and support Cancel.

This workflow deliberately keeps the editor on its current source. You do not
have to switch MP to the output skin or reconnect after each save. If you want
to switch the running MP installation to the saved skin later, install/select
that complete copy through MP's normal skin workflow. A saved folder outside
MP's skin directory is not automatically installed or selected in MP.

## Later saves

Continue editing and click **Save skin …** again. The file list compares editor
bytes to the last saved output. Use **Review XML …** to compare actual target
bytes with the desired XML, including exact SHA256 hashes. **Update saved copy**
writes only the reviewed changed files. **Refresh checks** detects external
changes; another check is performed immediately before replacement.

The output directory is remembered by **Save Project** and restored when that
editor project is opened again. **Choose saved copy …** reconnects a previously
created output manually. Only copies created by this save workflow carry the
source-bound receipt needed for updates. Ordinary existing skins and copies
created through the older Copy skin dialog are not overwritten; use New skin
copy for their first managed save.

Undo/Redo remains available after saving. If you Undo a previously saved edit,
the next save can put the original bytes back into the output copy. The file
review includes that reversal even when Studio's source XML is no longer dirty.
Only files loaded in this editor session are candidates for subsequent updates;
an output file from an earlier session is not implicitly reverted unless its
source has been loaded. Save Project retains loaded edits and their history.

## Conflicts and backups

Updates never force an overwrite. A changed/missing/read-only target, different
source baseline, wrong source identity, linked path or concurrent save blocks
the operation. Review the conflict or create a new output copy. The current
source and editor changes remain available.

Each update stores the previous XML bytes and copy receipt in a ZIP below
`.MP1SkinStudio/backups` inside the output skin. The ZIP preserves relative
paths. A normal failed multi-file update rolls back the files already replaced.
If another program changes files during rollback, the save reports the recovery
location and retains recovery files instead of overwriting that program's data.
The backup folder and receipt do not add XML pages to the editor list.

To recover an older output, stop using that output copy in MP, retain its current
state separately, then restore the desired backup ZIP's entries into the same
output root, including `.mp1skinstudio-copy`. Refresh the save dialog afterwards.
Recovering a backup does not modify the original source or Studio undo history.

## Live preview and editor projects

Saving the output copy does not make temporary runtime overrides permanent in
the current MP skin. Disconnect still restores that runtime's original state.
Your saved output remains on disk. This separation avoids a page reload and
keeps the original XML baseline available to both Studio and MP.

**Save Project** stores editing state and Undo history. **Save skin …** stores a
usable complete output skin. Export Changes remains available for distributing
only changed XML. Preview-only samples and editor settings are never written
into the output XML. The optional saved-output location uses the existing
project format 5; older format-5 readers may ignore that location.
