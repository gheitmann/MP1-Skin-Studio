# Preview.3 live editing workflow acceptance

October 29, 2026. Fresh final editor with the installed MP 1.39 process and
LiveBridge 1.1. The 12 workflow checks in Results.txt pass over real named pipes.
WorkflowProbe starts Studio on the bundled demo, connects to MP, adopts its
current skin/theme/page, previews a geometry draft and reverts it. It verifies
pending-input protection, Cancel with unsaved model changes, unreadable XML,
same-page session retention, and actionable occupied-bridge guidance. All runtime
overrides are restored by disconnect; original skin XML is not written.

The installed Home page has no suitable unbound literal label, so this fresh
workflow uses geometry. Text and specialized collection rendering have separate
full-MP evidence under Live-Coverage. The existing save/copy implementation is
unchanged; its prior acceptance is recorded under Saved-Skin.

Compile WorkflowProbe.cs as a console executable beside the editor, referencing
MP1SkinStudio.exe, System, System.Core, System.Xml, System.Drawing and
System.Windows.Forms. Run with normal desktop rights while one MP with the
bridge is active and other Studio live clients are disconnected. The fixture
changes only a temporary preview. Its unsaved-session Cancel is driven by the
test's own WinForms event loop; it does not discard an owner's editor session.

Fresh core suite: 471 passed, 0 failed. The final source and executable retain
that result; no persistence/model code changed in this workflow package.
