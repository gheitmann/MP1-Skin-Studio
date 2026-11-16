using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using MediaPortal.Common.Utils;
using MediaPortal.GUI.Library;
using SharpDX.Direct3D9;

[assembly: CompatibleVersion("1.39.0.0")]

// Build only for the isolated validation client. This fixture captures the
// MediaPortal render surface, not the Windows desktop.
public sealed class NativeCapture : IPlugin, ISetupForm
{
    Timer timer;
    public string PluginName() { return "Skin Studio Font Fixture"; }
    public string Description() { return "Capture MediaPortal's native frame and list state for comparison"; }
    public string Author() { return "MP1 Skin Studio"; }
    public bool CanEnable() { return true; }
    public bool DefaultEnabled() { return true; }
    public bool HasSetup() { return false; }
    public void ShowPlugin() {}
    public int GetWindowId() { return 0; }
    public bool GetHome(out string text, out string image, out string focus, out string picture)
    { text = image = focus = picture = ""; return false; }
    public void Start()
    {
        timer = new Timer { Interval = 400 };
        timer.Tick += (sender, args) => CaptureRequestedPage();
        timer.Start();
    }
    public void Stop() { if (timer != null) timer.Dispose(); }

    void CaptureRequestedPage()
    {
        string directory = Environment.GetEnvironmentVariable("MP1_SKIN_STUDIO_CAPTURE_DIR");
        if (String.IsNullOrWhiteSpace(directory) || GUIWindowManager.ActiveWindowEx != 34) return;
        string request = Path.Combine(directory, "capture.request");
        if (!File.Exists(request)) return;
        File.Delete(request);
        try
        {
            var window = GUIWindowManager.GetWindow(34);
            var facade = window.GetControl(50) as GUIFacadeControl;
            if (facade == null || facade.Count == 0 || GUIGraphicsContext.DX9Device == null)
                throw new InvalidOperationException("Plugin overview has no active list or render device.");
            var snapshot = new XmlDocument();
            var root = snapshot.CreateElement("preview");
            snapshot.AppendChild(root);
            root.SetAttribute("focus", window.GetFocusControlId().ToString());
            root.SetAttribute("selected", facade.SelectedListItemIndex.ToString());
            for (int index = 0; index < Math.Min(50, facade.Count); index++)
            {
                var native = facade[index];
                var item = snapshot.CreateElement("item");
                item.SetAttribute("title", native.Label ?? "");
                item.SetAttribute("detail", native.Label2 ?? "");
                root.AppendChild(item);
            }
            lock (GUIGraphicsContext.RenderLock)
            using (var surface = GUIGraphicsContext.DX9Device.GetBackBuffer(0, 0))
                Surface.ToFile(surface, Path.Combine(directory, "MediaPortal-Plugins.png"), ImageFileFormat.Png);
            snapshot.Save(Path.Combine(directory, "Matched-List.xml"));
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(directory, "capture-error.txt"), error.ToString());
        }
    }
}
