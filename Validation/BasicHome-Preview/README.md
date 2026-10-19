# BasicHome sample and static condition evaluation

October 19, 2026.

Element > BasicHome preview applies an explicit sample only on BasicHome.xml with window ID 35. It selects FanartHandler background mode, disables the Titan update indicator, sets stopped playback and restores the page default focus. Language and unrelated samples are retained. Clean mode hides remaining unverified content. No live fanart, latest-media values or runtime menu configuration are read. Save the editor project to retain the state.

The condition interpreter now evaluates supported boolean #(...) expressions with scoped defines. Boolean literals, not, and, or and plugin.isenabled are supported through the existing bounded expression evaluator. Missing operands, unsupported functions and nonboolean results remain unknown; function arguments are evaluated conservatively rather than inferring results from unresolved inputs. Static control.isvisible returns false when no target exists in the loaded model. Duplicate targets and cycles remain unknown; missing content lookups retain their previous unknown behavior.

Button text areas now subtract the vertical text offset once, leaving the remaining label height. Large BasicHome offsets previously produced zero height and hid all captions.

Primary behavior references: [GUIInfoManager](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIInfoManager.cs) for absent-control visibility; [GUIButtonControl](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIButtonControl.cs) for vertical label sizing. No upstream implementation was copied.

## Fresh validation

254 core checks passed. New coverage includes scoped expression includes/otherwise branches, boolean functions and unknown preservation, absent versus ambiguous/cyclic controls, invalid-page rejection without mutation, BasicHome project persistence and large button offsets. The earlier text-offset expectation was corrected from double to single vertical subtraction.

Probe.cs checked real Titan BasicHome in Default and Christmas. Both have six visible tile buttons, focus 4, exact content-signature/project/session roundtrip, 238 controls, 184 hidden and 11 unresolved. All 32 loaded original source hashes, source XML and undo history remain unchanged. Production renders were visually inspected in both themes: six tile pictures and German captions appear with TV focused. The saved state survives reopening.

No new interactive GUI/client test or export installation occurred. Runtime fanart, latest media, notifications, submenu animation and the eleven unknown conditions remain outside this sample. Previous multi-page and installed-client reports are historical.
