using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public static class PluginOverviewPreview
    {
        const string WriterSample = "#studioPluginWriter";

        public static bool Supports(SkinProject project)
        {
            return project != null && project.WindowId == 34 &&
                String.Equals(Path.GetFileName(project.Page), "myHomePlugIns.xml", StringComparison.OrdinalIgnoreCase) &&
                project.Controls.Any(control => control.Get("type") == "facadeview" && SkinProject.ControlId(control) == 50);
        }

        public static void Apply(SkinProject project)
        {
            var preview = Prepare(project);
            preview.Values["#pluginname"] = "My Notifier";
            preview.Values["#pluginauthor"] = "Team MediaPortal";
            preview.Values["#plugindescription"] = "Notifier. (1.39.0.0)";
            preview.Values[WriterSample] = "Writer:";
            PlaybackPreview.Stopped(preview);

            string artwork = new[] { "hover_my notifier.png", "hover_my plugins.png" }
                .FirstOrDefault(path => File.Exists(project.Resolve("Media/" + path))) ?? "";
            var list = new CollectionSettings { Mode = "list", Focus = 0 };
            list.Items.Clear();
            list.Items.Add(new PreviewItem { Title = "My Notifier", Detail = "Team MediaPortal", Image = artwork });
            Apply(project, preview, list, 50);
        }

        internal static int ApplyLive(SkinProject project, XmlElement captured)
        {
            var preview = Prepare(project);
            foreach (var name in new[] { "pluginname", "pluginauthor", "plugindescription", "time", "date" })
                preview.Values["#" + name] = captured.GetAttribute(name);
            preview.Values[WriterSample] = captured.GetAttribute("writer");
            string artwork = Image(project, captured.GetAttribute("artwork"));
            if (artwork.Length > 0) preview.Artwork["#selectedthumb"] = artwork;

            var rows = captured.SelectNodes("item");
            int total = LiveWire.Int(captured, "count", rows.Count, Int32.MaxValue);
            if (rows.Count > 50 || rows.Count != Math.Min(total, 50))
                throw new InvalidDataException("Invalid MediaPortal plugin list.");
            int focus = LiveWire.Int(captured, "focus", Int32.MinValue, Int32.MaxValue);
            if (project.LogicalControls().Count(c => SkinProject.Focusable(c) && SkinProject.ControlId(c) == focus) != 1)
                focus = 50;
            var list = new CollectionSettings
            {
                Mode = "list",
                Focus = LiveWire.Int(captured, "selection", 0, Math.Max(0, rows.Count - 1))
            };
            list.Items.Clear();
            foreach (XmlElement row in rows)
                list.Items.Add(new PreviewItem
                {
                    Title = row.GetAttribute("title"),
                    Detail = row.GetAttribute("detail"),
                    Image = Image(project, row.GetAttribute("image")),
                    Marked = row.GetAttribute("marked") == "true"
                });
            PlaybackPreview.Stopped(preview);
            Apply(project, preview, list, focus);
            return total;
        }

        static string Image(SkinProject project, string path)
        {
            if (path.Length == 0) return "";
            ArtworkPreview.Validate(new Dictionary<string, string> { { "#selectedthumb", path } });
            return File.Exists(project.Resolve("Media/" + path)) ? path : "";
        }

        static PreviewSettings Prepare(SkinProject project)
        {
            if (!Supports(project)) throw new InvalidOperationException("Open the Plugin overview to use its example.");
            var preview = project.Editor.Preview.Copy();
            foreach (var key in new[] { "#itemcount", "#selectedindex", "#selecteditem", "#selecteditem2", "#selectedthumb" })
                preview.Values.Remove(key);
            preview.Artwork.Remove("#selectedthumb");
            return preview;
        }

        static void Apply(SkinProject project, PreviewSettings preview, CollectionSettings list, int focus)
        {
            preview.Validate();
            list.Validate();
            project.Editor.ValidateFocus(project.Page, project.Theme, focus);
            project.Editor.Preview.SetFrom(preview);
            project.Editor.Collection.SetFrom(list);
            project.Editor.RememberFocus(project.Page, project.Theme, focus);
            project.Editor.ShowUnresolved = false;
            project.Editor.PreviewMode = "layout";
        }

        internal static bool TryWriterSample(SkinProject project, int id, out string text)
        {
            text = null;
            return id == 200 && Supports(project) && project.Editor.Preview.Values.TryGetValue(WriterSample, out text);
        }
    }

    public sealed partial class StudioForm
    {
        int pluginCaptureGeneration = -1, pluginCaptureRevision;

        bool CanReadPluginPreview()
        {
            return PluginOverviewPreview.Supports(project) && liveVerified && liveBinding != null &&
                liveBinding.SupportsPluginPreview && liveContext == LiveContext();
        }

        void ReadPluginPreview()
        {
            RequireAppliedProperties();
            if (!CanReadPluginPreview() || pluginCaptureGeneration == liveGeneration) return;
            var request = LiveWire.Document("plugin-preview");
            LiveWire.Attr(request.DocumentElement, "epoch", liveBinding.Epoch);
            LiveWire.Attr(request.DocumentElement, "revision", ++pluginCaptureRevision);
            liveClient.Send(request);
            pluginCaptureGeneration = liveGeneration;
            UpdatePluginPreviewButton();
        }

        void ReceivePluginPreview(XmlElement result)
        {
            int revision;
            if (!Int32.TryParse(result.GetAttribute("requestRevision"), out revision) ||
                revision != pluginCaptureRevision) return;
            bool current = pluginCaptureGeneration == liveGeneration;
            pluginCaptureGeneration = -1;
            UpdatePluginPreviewButton();
            if (!current || !CanReadPluginPreview()) return;
            if (result.GetAttribute("ok") == "false")
            {
                ShowNotice("Cannot read MediaPortal preview: " + result.GetAttribute("message"));
                return;
            }
            if (result.GetAttribute("epoch") != liveBinding.Epoch) return;
            try
            {
                RequireAppliedProperties();
                int total = PluginOverviewPreview.ApplyLive(project, result);
                ShowPluginOverviewPreview();
                ShowNotice("MediaPortal preview imported · " + project.Editor.Collection.Items.Count + " of " +
                    total + " rows. Click Use MP preview again to refresh.");
            }
            catch (Exception ex) { ShowNotice("Cannot use MediaPortal preview: " + ex.Message); }
        }

        void ApplyPluginOverviewPreview()
        {
            RequireAppliedProperties();
            PluginOverviewPreview.Apply(project);
            pluginCaptureGeneration = -1;
            ShowPluginOverviewPreview();
            ShowNotice("Plugin example applied. Use Sample Data and Collection to customize the image, text and list.");
        }

        void ShowPluginOverviewPreview()
        {
            canvas.IsolateSelection = false;
            ApplyPreviewSettings(project.Editor.Preview);
            state.RestoreFocus();
            RefreshProject(null);
        }
    }
}
