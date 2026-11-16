# First steps and editor reliability validation

November 16, 2026, Windows with MediaPortal 1.39 and LiveBridge 1.3.0.0.

- The guided workflow starts on the bundled demo. It opens the guide in the
  minimum-size window, uses keyboard page search and opens the fixture page
  in a running isolated MediaPortal. It checks live property hints and changes
  a visible label through Edit selected text. The label changes in the same
  native window epoch, survives creation of a complete skin copy and returns
  to its original value in the running session after Undo. The isolated
  source page remains byte-identical.
- A fresh isolated MediaPortal session loads the saved skin and returns the
  edited label from disk. The complete copy contains 3,402 files including its
  save receipt. Only the isolated test client's configuration was redirected.
- Opening the guide at the minimum window size initially left a 505 × 35 pixel
  canvas. The automatically collapsed information area leaves 505 × 142 pixels
  with the same controls visible. The information heading toggles the panel.
- The first complete skin copy previously failed on the staging path even
  though the final path fit Windows' .NET Framework limit. The guide probe
  now creates the output at the same depth and verifies its edited XML and
  fonts.xml.
- The core suite passed 490 checks and the full desktop UI smoke suite passed
  229 checks. The caption hit-test regression covers
  visible and fully transparent labels over a later image. The editor's
  left-click, double-click and overlap menu share the same hit ordering.

Run `MP1SkinStudio.exe --check-skin "<copied-skin>" "<report>"` against
independent XML copies. The run checks byte identity, page/theme loading,
geometry edits, exact Undo and selected project/export round trips. It uses
the model and does not claim native rendering coverage for these skin variants.

| Skin | XML files | Page/theme variants | Edit/Undo checks | Project/export |
| --- | ---: | ---: | ---: | ---: |
| DefaultWideHD test copy with one fixture | 489 | 450 | 434 | 5 |
| Titan | 586 | 1,369 | 1,328 | 20 |
| MayaHD | 479 | 406 | 397 | 5 |

All three passed with zero XML, page or editing errors, and zero input XML
changes. All 488 monitored installed DefaultWideHD XML hashes also match the
original baseline. See [Results.txt](Results.txt) for the guided acceptance.

For the live probe, prepare an isolated client and skin with
`Validation/Live-Fonts/Prepare.ps1`. Start that client with its own
`MP1_SKIN_STUDIO_LIVE_DIR`. Compile Probe.cs next to the tested
MP1SkinStudio.exe, referencing the editor, System, System.Core,
System.Drawing, System.Windows.Forms and System.Xml. Pass the bundled demo
root, isolated IPC directory and an empty writable output directory as
arguments. The probe opens fixture window 90120 and saves a skin named
First Steps Output. The output location should have enough path space for
the copied skin's files.

To verify restored content from disk, start a fresh isolated client with the
saved output selected as its skin. Run `Probe.exe --verify-saved "<isolated IPC>"
"<saved skin root>"`. This reads the native label through the bridge after
opening the fixture page. Use copies for both runs.
