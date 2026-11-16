using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;

static class PluginPreviewProbe
{
    const BindingFlags PrivateFields = BindingFlags.Instance | BindingFlags.NonPublic;

    static T Field<T>(StudioForm form, string name)
    {
        return (T)typeof(StudioForm).GetField(name, PrivateFields).GetValue(form);
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new StudioForm(args[0]))
            {
                form.PrepareExternalPreview(args[0], "myHomePlugIns.xml", "");
                form.Show();
                form.Hide();
                form.Show();
                Application.DoEvents();
                var project = Field<SkinProject>(form, "project");
                string source = ProjectFile.XmlSignature(project);
                var example = Field<Button>(form, "pluginExample");
                if (!example.Visible || !example.Enabled) throw new Exception("Plugin example is unavailable.");
                example.PerformClick();
                var state = Field<PreviewState>(form, "state");
                if (state.FocusId != 50 || project.Editor.Collection.Items.Single().Image != "hover_my notifier.png")
                    throw new Exception("Plugin image or focus was not applied.");
                var descriptions = project.Controls.Where(c => c.Get("description") == "Plugin Description")
                    .Select(c => ReadablePreview.Caption(state, c)).ToArray();
                if (!descriptions.Contains("Writer:Team MediaPortal") || !descriptions.Contains("Notifier. (1.39.0.0)"))
                    throw new Exception("Plugin description bindings did not resolve.");
                if (ProjectFile.XmlSignature(project) != source || project.Dirty || project.Undo.Count != 0)
                    throw new Exception("Applying a visual example altered source XML.");
                Field<Button>(form, "previewInformationToggle").PerformClick();
                form.RenderTo(args[1]);
                Console.WriteLine("PASS DefaultWideHD Plugin example button, artwork, dynamic labels, list focus and unchanged source.");
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
