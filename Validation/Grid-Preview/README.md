# GridLayout preview validation

Executed on October 18, 2026. These reports are new runs, separate from the
historical version 0.9 and English migration reports.

Results: **200 core checks, 188 UI checks and 10 native checks passed**.
`Grid-Preview.png` is an editor render inspected after adjusting demo label contrast.

The preview supports fixed columns or rows, horizontal and vertical fill,
separate nonnegative spacing, group margins, child alignment and stretching,
nested automatic layouts, and derived rectangles for mouse selection.
Previewing never writes calculated positions to skin XML.

MP1-specific behavior is preserved:

- Positive rows take precedence over supplied columns.
- Cell dimensions come from the largest non-collapsed child.
- Hidden children retain cells; collapsed children still count towards the
  dimensions but skip the horizontal advance in their row.
- The XML factory ignores the third argument when exactly three are supplied.
  Use four arguments to specify horizontal and vertical spacing.

The parser accepts zero through five valid parameters. Invalid or unsupported
forms retain source geometry and generate a warning rather than silently
inventing a layout. Very large dimensions are processed per actual child,
without enumerating empty cells. Integer-coordinate rounding, font metrics,
dynamic runtime measurements and animations remain approximate.

## Original assembly comparison

`NativeGridProbe.cs` exercises `GUIControlFactory.ParseLayout` and `GridLayout`
from the installed, original MP1 **1.39.0.0** `Core.dll`. Ten checks cover
wrapping, measured sizes, orientation, row precedence, XML constructor
selection, visibility, margins, alignment and stretching.

Run against an isolated directory containing the original Core.dll, Utils.dll,
dependencies, MediaPortal.exe.config and a MediaPortalDirs.xml whose paths all
point to that isolated directory:

```powershell
.\Validation\Grid-Preview\Run-NativeGridProbe.ps1 -MP1Directory C:\IsolatedMP1
```

The script writes only its executable/config to that directory and the report
to this validation folder. Do not pass the live installation directory.
No original MediaPortal binaries or personal runtime logs are committed.

Implementation references:

- [Original GridLayout](https://github.com/MediaPortal/MediaPortal-1/blob/43675ce01fcc9a199107b7e9fa414d408a79fa32/mediaportal/Core/Drawing.Layouts/GridLayout.cs)
- [Original XML layout factory](https://github.com/MediaPortal/MediaPortal-1/blob/43675ce01fcc9a199107b7e9fa414d408a79fa32/mediaportal/Core/guilib/GUIControlFactory.cs)

## Desktop comparison blocker

The installed MP1 log confirms a Titan client startup and shutdown on October 18.
A visible client image/navigation comparison has **not** been completed.
The provided Windows automation runtime fails before listing any windows:
`failed to launch codex app-server: program not found`.
Reinitializing its JavaScript session reproduces the same error. This is a
host runtime issue, independent of editor window handling. No installation,
TV configuration or original skin files were changed.
