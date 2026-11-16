using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio;
using MP1SkinStudio.Live;

static class LivePluginPreviewProbe
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static T Field<T>(StudioForm form, string name)
    {
        return (T)typeof(StudioForm).GetField(name, Private).GetValue(form);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static XmlElement Exchange(NamedPipeClientStream pipe, string command, string attribute = null, string value = null)
    {
        var request = LiveWire.Document(command);
        if (attribute != null) LiveWire.Attr(request.DocumentElement, attribute, value);
        LiveWire.Write(pipe, request);
        return LiveWire.Read(pipe).DocumentElement;
    }

    static XmlElement OpenPluginOverview(LiveEndpoint endpoint)
    {
        using (var pipe = new NamedPipeClientStream(".", endpoint.Pipe, PipeDirection.InOut))
        {
            pipe.Connect(2000);
            Check(Exchange(pipe, "hello", "nonce", endpoint.Nonce).GetAttribute("ok") == "true", "Handshake failed.");
            Check(Exchange(pipe, "activate", "window", "34").GetAttribute("ok") == "true", "Page activation failed.");
            var snapshot = Exchange(pipe, "snapshot");
            Check(snapshot.GetAttribute("window") == "34" && snapshot.GetAttribute("pluginPreview") == "1", "Runtime capability is missing.");
            Check(Exchange(pipe, "plugin-preview", "epoch", "stale").GetAttribute("ok") == "false", "Stale capture was accepted.");
            var sample = Exchange(pipe, "plugin-preview", "epoch", snapshot.GetAttribute("epoch"));
            Check(sample.GetAttribute("ok") == "true", sample.GetAttribute("message"));
            Exchange(pipe, "disconnect");
            return sample;
        }
    }

    static void WaitFor(Func<bool> condition, string failure)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.ElapsedMilliseconds < 8000)
        {
            Application.DoEvents();
            Thread.Sleep(15);
        }
        Check(condition(), failure);
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            var endpoint = LiveEndpoint.Discover().Single(e => e.ProcessId == Int32.Parse(args[0]));
            var native = OpenPluginOverview(endpoint);
            File.WriteAllText(Path.Combine(args[2], "native-preview.xml"), native.OuterXml);
            Check(native.SelectNodes("item").Count > 0 && native.GetAttribute("plugindescription").Length > 0,
                "The native test page lacks plugin content.");
            Thread.Sleep(150);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new StudioForm(args[1]))
            {
                form.PrepareExternalPreview(args[1], "myHomePlugIns.xml", "");
                form.Show();
                Application.DoEvents();
                var project = Field<SkinProject>(form, "project");
                string source = ProjectFile.XmlSignature(project);
                var capture = Field<Button>(form, "livePluginExample");
                Check(capture.Visible && !capture.Enabled, "Capture should be disabled before connecting.");
                Field<Button>(form, "liveConnect").PerformClick();
                WaitFor(() => capture.Enabled, "The capture button did not enable: " + Field<Label>(form, "liveNotice").Text);
                capture.PerformClick();
                WaitFor(() => capture.Enabled, "The capture did not finish.");
                var state = Field<PreviewState>(form, "state");
                var list = project.Editor.Collection;
                var expected = native.SelectNodes("item").Cast<XmlElement>().ToArray();
                Check(list.Items.Select(item => item.Title).SequenceEqual(expected.Select(row => row.GetAttribute("title"))),
                    "Studio did not transfer the native list.");
                Check(list.Items.Select(item => item.Marked).SequenceEqual(expected.Select(row => row.GetAttribute("marked") == "true")),
                    "Studio did not transfer the native marked state.");
                Check(list.SelectedIndex == Int32.Parse(native.GetAttribute("selection")) &&
                    state.FocusId == Int32.Parse(native.GetAttribute("focus")), "Studio did not transfer native selection and focus.");
                foreach (string key in new[] { "pluginname", "pluginauthor", "plugindescription" })
                    Check(state.Values["#" + key] == native.GetAttribute(key), "Studio did not transfer " + key + ".");
                string artwork;
                Check(project.Editor.Preview.Artwork.TryGetValue("#selectedthumb", out artwork) &&
                    artwork == native.GetAttribute("artwork") && File.Exists(project.Resolve("Media/" + artwork)),
                    "Studio did not transfer native artwork.");
                Check(ProjectFile.XmlSignature(project) == source && !project.Dirty && project.Undo.Count == 0,
                    "Capturing the preview altered skin XML.");
                Field<Button>(form, "liveDisconnect").PerformClick();
                WaitFor(() => Field<Button>(form, "liveConnect").Enabled, "Studio did not disconnect.");
                Check(!capture.Enabled && Field<Button>(form, "pluginExample").Enabled, "Offline controls did not recover.");
                form.RenderTo(Path.Combine(args[2], "studio.png"));
            }
            Console.WriteLine("PASS native plugin content, artwork, selection, focus, stale epoch, offline fallback and unchanged skin XML.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
