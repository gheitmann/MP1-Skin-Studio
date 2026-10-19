# Installed MP1 Video export validation

Date: October 19, 2026. Source build: 609d3a0c2156315d56d20ffba060a1493b64e2f0.

## Candidate

Probe.cs opens installed Titan myvideo.xml, changes the concrete imported list width from 698 to 578 and the shared header to STUDIO EXPORT TEST, then saves the project and exports. Candidate validation passed: exactly two XML files plus instructions, exported text matches the project, and all 17 loaded original source hashes remain unchanged. The shared header intentionally also affects Home in this test.

The two exported files were applied only to a full isolated installed skin copy named SkinStudio-Video-Test-20261019. No Titan file was overwritten.

## Fresh attended Windows observations

The installed MediaPortal client loaded the isolated copy through Settings > GUI > Skin. Home and the Video page displayed STUDIO EXPORT TEST. The Videos folder, count 1 and selected row appeared; its focus band was visibly narrower than original Titan. Left opened the Video side menu and Right returned to the list. The client remained responsive.

Titan was then selected again through the same UI. The selection button displayed Titan. Home showed its normal title and the Video page showed Videos with the original wider focus band. The client was left on this original Video page.

## File preservation

A fresh SHA-256 comparison against the previously preserved full Titan copy checked all 2,050 original files: zero differences. The isolated installed copy also contains 2,050 files and differs from Titan only in common.time.xml and common.facade.video.default.xml.

## Scope

This is a sampled export/display/navigation smoke test. It does not establish full preview fidelity, all themes, plugins or movie playback. The prior 245 core, 25 expression and 11 Titan model checks are historical for this runtime checkpoint and were not rerun. No production source changed here. DirectX replacement remains deferred.
