# First public release verification

October 20, 2026. The owner authorized public repository visibility and the first
release. GitHub access confirmation and manual asset upload were completed by
the owner; the saved release was then published and verified.

| Item | Verified result |
| --- | --- |
| Repository | gheitmann/MP1-Skin-Studio, public; anonymous API read succeeded |
| Release | v0.9.0-preview.1, published pre-release, not a draft |
| Release ID | 418891670 |
| Published | 2026-10-20T21:23:22Z |
| Tag target | 633170cbab3aff5ce18c5398cf59c3d4f6c0618b |
| Windows ZIP | MP1-Skin-Studio-v0.9.0-preview.1-win.zip, 426273 bytes |
| ZIP SHA256 | be50eb6a5cc11f4dab5b469f2d900c48b97f113c3aa6b15f94ac23d70c699706 |
| Checksum asset | SHA256SUMS.txt, 108 bytes |
| Checksum asset SHA256 | c0ff0295ab6f343b643ec293f598854b60b3a0574a3dc6e9fdc1d58925aadb11 |
| Payload | 32 manifest-verified files plus review README and manifest |
| Anonymous download | Succeeded; downloaded ZIP SHA256 matches the release asset and local ZIP |

Before publication, all 540 existing tracked files matched the GitHub tree and
the targeted credential-pattern scan found no matches. Historical runtime test
archives were inventoried; the downloadable release includes only the editor,
bundled demo, license and guides. No installed skin or desktop capture is added.

Publication only adds release documentation and packaging of the already accepted
executable (SHA256 006e9952398badbd40c6b9292a9d9ef2b484672816203418815a5bda69df1470).
The latest 25 selector/sidebar and ten readable-preview scenarios retain their
previous checkpoint; functional suites were not rerun just for publication.
Post-publication documentation commits do not move the release tag or assets.

[Public release](https://github.com/gheitmann/MP1-Skin-Studio/releases/tag/v0.9.0-preview.1)
