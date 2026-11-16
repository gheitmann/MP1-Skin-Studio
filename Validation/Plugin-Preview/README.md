# Plugin overview preview

Open DefaultWideHD's myHomePlugIns.xml and click **Plugin example** beside
**View**. It sets up the My Notifier sample, including the selected row, author,
description and artwork from the skin's Media folder. **Sample Data** edits
the text values and **Collection** edits the row or artwork path. A loaded
language pack takes precedence over the example's writer label.

The sample updates the editor project state. The authored skin bindings and
Undo history are preserved. Artwork is only chosen from two known filenames
under the skin Media directory. Plugin content is modeled by the example;
the preset does not import the state of the running plugin.

List and playlist caption color follows the item's optional **marked** flag
separately from its focus highlight. Old collection entries default to
unmarked. Other plugin-specific color rules and visual masks remain approximate.

The final editor passed 491 core checks, including marked versus focused row
pixels, sample persistence and source preservation. Probe.cs drove the new
button on a complete isolated DefaultWideHD copy. The resulting visual check
showed the My Notifier art, author, description and white focused text as in
the earlier native MP 1.39 reference capture.

Compile Probe.cs beside the tested editor with System, System.Core,
System.Windows.Forms and a reference to MP1SkinStudio.exe. Pass an isolated
DefaultWideHD root and the desired PNG output path.
