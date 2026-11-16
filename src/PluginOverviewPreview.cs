using System;
using System.IO;
using System.Linq;

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
            if (!Supports(project)) throw new InvalidOperationException("Open the Plugin overview to use its example.");
            var preview = project.Editor.Preview.Copy();
            foreach (var key in new[] { "#itemcount", "#selectedindex", "#selecteditem", "#selecteditem2", "#selectedthumb" })
                preview.Values.Remove(key);
            preview.Artwork.Remove("#selectedthumb");
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
            preview.Validate();
            list.Validate();
            project.Editor.ValidateFocus(project.Page, project.Theme, 50);
            project.Editor.Preview.SetFrom(preview);
            project.Editor.Collection.SetFrom(list);
            project.Editor.RememberFocus(project.Page, project.Theme, 50);
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
        void ApplyPluginOverviewPreview()
        {
            RequireAppliedProperties();
            PluginOverviewPreview.Apply(project);
            canvas.IsolateSelection = false;
            ApplyPreviewSettings(project.Editor.Preview);
            state.SetFocus(50);
            RefreshProject(null);
            ShowNotice("Plugin example applied. Use Sample Data and Collection to customize the image, text and list.");
        }
    }
}
