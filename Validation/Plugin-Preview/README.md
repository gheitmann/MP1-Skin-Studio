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

## Native import

LiveProbe.cs uses a separate MediaPortal 1.39 process with Bridge 1.4 and an
isolated DefaultWideHD copy. It activates window 34, verifies that the bridge
rejects a stale epoch and reads the native list and properties. It then drives
**Use MP preview** and compares the imported labels, selection, focus and
artwork with the native data. It checks that the skin XML and Undo history
stay unchanged and that the offline example is available after disconnect.

Compile LiveProbe.cs against MP1SkinStudio.exe with System, System.Core,
System.Windows.Forms, System.Drawing and System.Xml. Point
MP1_SKIN_STUDIO_LIVE_DIR at the isolated bridge's endpoint directory. Pass its
process ID, the copied skin root and an existing output directory.
