# Explicit player condition samples

October 19, 2026.

The preview interpreter accepts explicit boolean samples for player.hasmedia, player.hasaudio, player.hasvideo, player.playing and player.paused. Missing samples remain unknown. No live player state is queried or changed. The Video folder preset explicitly chooses stopped playback; Element > Playback preview > Stopped applies only the five false flags, while Unverified removes only those overrides. Pending property edits must be applied first.

The flags appear in Sample Data as #preview.player.hasmedia, #preview.player.hasaudio, #preview.player.hasvideo, #preview.player.playing and #preview.player.paused. Values must be true or false. Individual flags are independent sample inputs; contradictory user-entered flags are not corrected automatically. Other player conditions remain unsupported. Samples are stored through the existing project snapshot and never inserted into exported XML.

[Upstream GUIInfoManager](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIInfoManager.cs) was consulted for condition names and stopped-player behavior. This implements explicit sample evaluation, not a replica of the native playback engine.

## Fresh validation

249 core checks passed. The new coverage checks absent samples, stopped conditions, boolean short-circuiting, case-insensitive explicit overrides, unsupported player conditions, invalid boolean rejection and removal preserving unrelated data. Existing folder save/reopen/export checks cover persistence and XML separation.

The targeted real Titan probe passed: 211 controls, 158 hidden, 19 unresolved (previous sample: 62 unresolved). List focus 50 and content results survive project roundtrip; all 17 loaded source hashes, XML signature and undo history remain unchanged. The full remaining unresolved-expression inventory is included in Titan-Results.txt.

No new interactive editor/client test or export installation was performed. Earlier installed-client results remain historical. Remaining unknowns involve skin options, runtime/cache artwork, idle time and folder/importer state. The reduction is specific to the explicit stopped-playback scenario.
