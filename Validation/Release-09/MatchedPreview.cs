using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio;

static class MatchedPreview
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            var snapshot = new XmlDocument { XmlResolver = null };
            snapshot.Load(Path.Combine(args[1], "Matched-List.xml"));
            var root = snapshot.DocumentElement;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new StudioForm(args[0]))
            {
                form.PrepareExternalPreview(args[0], "myHomePlugIns.xml", "");
                var state = (PreviewState)typeof(StudioForm)
                    .GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                var sample = state.Project.Editor.Collection;
                sample.Mode = "list";
                sample.Items.Clear();
                foreach (XmlElement item in root.SelectNodes("item"))
                    sample.Items.Add(new PreviewItem {
                        Title = item.GetAttribute("title"), Detail = item.GetAttribute("detail") });
                sample.Focus = Math.Max(0, Math.Min(sample.Items.Count - 1, Int32.Parse(root.GetAttribute("selected"))));
                state.SetFocus(Int32.Parse(root.GetAttribute("focus")));
                state.UpdateCollectionValues();
                typeof(StudioForm).GetMethod("UpdateDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, null);
                form.RenderTo(Path.Combine(args[1], "Studio-Plugins.png"));
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
