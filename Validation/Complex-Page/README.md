# Complex real-page model gate

Date: October 18, 2026, Europe/Berlin. Product baseline: bb735fc46cde9783329c0f89d174a8ebde2c07f3.

After the prior Windows automation connection failed, the owner reported a
system freeze requiring a hard reset. Causation is unconfirmed. The owner
authorized continuation with source code and tests only. No Computer Use calls,
window capture, client launch or editor GUI launch were made for this checkpoint.

Probe.cs is a separate console executable referencing the existing production
editor assembly. Loading that assembly does not invoke its program entry point.
It opens installed Titan myvideo.xml through the production SkinProject model.

Eleven reported stages passed (Results.txt):

- 211 controls from 17 sources, with five real facade subviews.
- Exactly one active list/small-icons/large-icons/filmstrip/coverflow template;
  finite positive tile geometry, one selected tile and matching view condition.
- 25 sample items, selected-item/count updates and scrolling to the last item,
  without skin XML changes or undo entries from preview-only state.
- List width 698 to 658 and text offset X 44 to 60, with edits confined to
  common.facade.video.default.xml. Other subview XML stays unchanged.
- Exact source-text undo/redo and project save/reopen with collection state.
- ZIP containing the edited shared XML and instructions only, with no sample
  titles leaked into the skin XML.
- Unchanged SHA256 hashes for all 17 loaded original sources.

This establishes the model/persistence/export path on a substantially more
complex real page. It does not establish visual equivalence, mouse selection,
runtime navigation, masks, animations, font fidelity or plugin-driven state on
this page. Shared-file edits affect other pages importing that file; the test
deliberately preserves that source relationship rather than flattening imports.

No production source changed. Previous full-suite counts remain historical;
this checkpoint ran only the dedicated integration probe. The saved project,
exported Titan XML and executable remain local; private machine paths and skin
assets are excluded from the committed report.

To reproduce, compile Probe.cs as a console executable next to MP1SkinStudio.exe
using the .NET Framework compiler. Reference MP1SkinStudio.exe, System,
System.Core, System.Drawing, System.Xml, System.IO.Compression and
System.IO.Compression.FileSystem. Pass the Titan root and a separate output
directory outside that root. Do not launch the editor GUI for this probe.

Next: inspect and test collection preview geometry and dynamic-condition
limitations at source/model level. Visual/runtime verification stays suspended,
and auxiliary tools and DirectX replacement remain deferred.
