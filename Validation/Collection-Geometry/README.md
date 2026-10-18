# Filmstrip text geometry correction

Date: October 18, 2026, Europe/Berlin. Baseline: be9fb2b28d0f33e20472ae495b39ee40a020f5f7.

Source inspection of installed Titan's common.facade.video.default.xml found
textYOff=-2000 in the filmstrip. The editor previously placed every filmstrip
title beneath the thumbnail with fixed padding, ignoring textXOff/textYOff and
using thumbnail dimensions instead of the label's texture dimensions.

The upstream [GUIFilmstripControl.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIFilmstripControl.cs)
positions the label vertically at item Y + textureHeight + textYOff, and uses
textureWidth/textureHeight for label dimensions. Its left-aligned label X uses
item X + textXOff. The preview geometry now follows these values for filmstrip
titles. Negative offsets remain negative so the existing collection viewport
clip can exclude intentionally offscreen labels. Thumbnail, grid and coverflow
geometry are unchanged. XML is not rewritten by this calculation.

## Fresh validation

- The new standalone geometry probe failed against the previous executable at
  its first position assertion before the fix.
- After the fix, all six console geometry checks passed, including independent
  thumbnail/text bounds and the actual installed Titan negative-offset case.
- All eleven complex real-page model/persistence/export stages passed again.
- Fresh final build and the core suite passed: **222 checks, zero failures**.
  RunCore.cs calls SelfTests.Run directly through a console executable without
  invoking the editor application entry point. The standard suite includes
  offscreen drawing checks, but does not show editor windows or run UI smoke
  tests. Reports are in this directory.

No Computer Use, window capture, editor GUI or MediaPortal launch was performed.
The 203 UI checks from the previous milestone were not rerun. This is a numeric
geometry correction; no new visual/runtime equivalence is claimed. Right/center
alignment anchoring, masks, animation, font metrics, and coverflow 3D behavior
remain separate preview limitations.

Compile Probe.cs and RunCore.cs as console executables referencing the production
MP1SkinStudio.exe assembly. Probe.cs additionally references System.Core and
System.Drawing; pass a scratch output directory and the installed Titan root.
RunCore.cs takes an output directory for test-results.txt. Outputs must stay
outside the original skin directory.
