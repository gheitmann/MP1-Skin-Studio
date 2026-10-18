# Manually approved client test: October 18, 2026

The owner explicitly authorized another Computer Use attempt and offered manual approval, superseding the earlier stop for this attended test only. The already running installed MediaPortal client was used; no client restart or dependency changes were needed.

## Observed results

- Enumerated and activated the installed MediaPortal window.
- Observed and dismissed the existing information dialog: Bitte Disc einlegen.
- A coordinate click on Settings did not visibly change the selected Home entry. Keyboard navigation was then used successfully.
- Down moved Home focus from Disc abspielen through Musik and Bilder to Einstellungen; Return opened the Titan Settings page.
- The Settings page initially focused Allgemein. Up wrapped to the final Einstellungen mit PIN sperren entry; Down returned to Allgemein. Only focus was moved: the PIN control was not toggled and no settings were changed.
- Routine keyboard input and capture succeeded with the owner available to approve. This does not demonstrate that Always allow is retained or that upstream issue #55114 is fixed.

## Initial comparison with the editor

Generated an editor preview from the installed Titan settings.xml with German language resources and hidden unresolved conditions. No original skin XML was modified.

Observed correspondence: the menu begins at approximately skin X=122/Y=321 with button dimensions 385x58 and a 7-pixel vertical gap; the client first button appears at window X=122/Y=372, accounting for the approximately 51-pixel title/menu offset. The menu panel and focus texture are present in both views. These are manual visual/source observations, not an automated pixel comparison.

Observed differences: the editor preview omits the visible page title and clock/date area, and its text appears substantially smaller than in the running client. Several runtime menu labels also differ from raw language-resource labels (for example TV versus Mein TV). The cause has not yet been established. Do not treat font, draw-order or label-override explanations as verified findings.

A complete matched-state image comparison and the exported 7-to-15 spacing test remain pending. The preview uses sample time rather than the live clock. Desktop captures included an overlapping private window and are not committed. The generated editor image is also omitted from this source checkpoint.

The client was left on Settings with Allgemein focused. No original skin, live configuration or DirectX dependency was changed. DirectX replacement remains deferred.
