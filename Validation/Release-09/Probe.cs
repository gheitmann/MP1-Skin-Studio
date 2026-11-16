using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio;
using MP1SkinStudio.Live;

static class ReleaseProbe
{
    static XmlDocument Exchange(NamedPipeClientStream pipe, XmlDocument request)
    {
        LiveWire.Write(pipe, request);
        var reply = LiveWire.Read(pipe);
        if (reply.DocumentElement.GetAttribute("ok") == "false")
            throw new InvalidOperationException(reply.DocumentElement.GetAttribute("message"));
        return reply;
    }

    static XmlDocument OpenPage(NamedPipeClientStream pipe, int id)
    {
        var request = LiveWire.Document("activate");
        LiveWire.Attr(request.DocumentElement, "window", id);
        Exchange(pipe, request);
        for (int attempt = 0; attempt < 25; attempt++)
        {
            var reply = Exchange(pipe, LiveWire.Document("snapshot"));
            if (reply.DocumentElement.GetAttribute("window") == id.ToString(CultureInfo.InvariantCulture))
                return reply;
            Thread.Sleep(150);
        }
        throw new InvalidOperationException("The actual MediaPortal page differs from the requested window " + id);
    }

    static LiveItem ReadItem(XmlDocument snapshot, string token)
    {
        return snapshot.DocumentElement.SelectNodes("control").Cast<XmlElement>()
            .Where(element => element.GetAttribute("token") == token).Select(LiveItem.Read).Single();
    }

    static void VerifyPage(NamedPipeClientStream pipe, string root, string outDir, int id, string page)
    {
        var baseline = OpenPage(pipe, id);
        if (!String.Equals(Path.GetFileName(baseline.DocumentElement.GetAttribute("page")), page, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MediaPortal opened a different XML file for " + page);
        var project = new SkinProject(root);
        project.OpenPage(page);
        var binding = new LiveBinding(project, baseline);
        Exchange(pipe, binding.Verify(project));
        var editor = new SkinProject(root);
        editor.OpenPage(page);
        int parsedPosition;
        var candidate = editor.Controls.SelectMany(control => binding.Matches
            .Where(pair => String.Equals(pair.Key, control.Key, StringComparison.OrdinalIgnoreCase) &&
                pair.Value.Length == 1 && pair.Value[0].Id > 0 && pair.Value[0].Supported.Contains("posX"))
            .Select(pair => new { Control = control, Native = pair.Value[0] }))
            .First(entry => !entry.Control.AutomaticLayout && !entry.Control.IsBound("posX") &&
                Int32.TryParse(entry.Control.Get("posX"), out parsedPosition) &&
                (entry.Native.Kind == "label" || entry.Native.Kind == "button" ||
                 entry.Native.Kind == "listcontrol" || entry.Native.Kind == "thumbnailpanel"));
        string field = "posX";
        int oldPosition = Int32.Parse(candidate.Native.Value(field), CultureInfo.InvariantCulture);
        string desired = (oldPosition + 24).ToString(CultureInfo.InvariantCulture);
        editor.Edit(candidate.Control, new Dictionary<string, string> { { field, desired } }, "Release acceptance position");
        var drafts = new Dictionary<string, Dictionary<string, string>>();
        Exchange(pipe, binding.Frame(editor, drafts, 1));
        var changed = Exchange(pipe, LiveWire.Document("snapshot"));
        int actual = Int32.Parse(ReadItem(changed, candidate.Native.Token).Value(field), CultureInfo.InvariantCulture);
        if (Math.Abs(actual - oldPosition - 24) > 1 || changed.DocumentElement.GetAttribute("epoch") != binding.Epoch)
            throw new InvalidOperationException("Position change was not applied to the original native window.");
        editor.UndoChange();
        Exchange(pipe, binding.Frame(editor, drafts, 2));
        var restored = Exchange(pipe, LiveWire.Document("snapshot"));
        if (ReadItem(restored, candidate.Native.Token).Value(field) != candidate.Native.Value(field) || editor.Dirty)
            throw new InvalidOperationException("Undo did not restore the original native geometry.");
        Console.WriteLine("PASS " + page + " (" + id + "): " + binding.Matches.Count +
            " editor elements linked, " + binding.Items.Length + " native controls, " +
            candidate.Native.Kind + " #" + candidate.Native.Id + " moved and restored within one epoch.");

        using (var form = new StudioForm(root))
        {
            form.PrepareExternalPreview(root, page, "");
            form.RenderTo(Path.Combine(outDir, "Studio-" + id + ".png"));
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR", args[0]);
            var endpoint = LiveEndpoint.Discover().Single(item => item.ProcessId == Int32.Parse(args[1]));
            using (var pipe = new NamedPipeClientStream(".", endpoint.Pipe, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                pipe.Connect(5000);
                var hello = LiveWire.Document("hello");
                LiveWire.Attr(hello.DocumentElement, "nonce", endpoint.Nonce);
                Exchange(pipe, hello);
                if (args.Length == 3)
                {
                    var timer = Stopwatch.StartNew();
                    try
                    {
                        var active = OpenPage(pipe, Int32.Parse(args[2]));
                        Console.WriteLine("PASS activate and snapshot: " +
                            Path.GetFileName(active.DocumentElement.GetAttribute("page")) + " in " +
                            timer.ElapsedMilliseconds + " ms.");
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException("Activation test after " + timer.ElapsedMilliseconds +
                            " ms: " + error.Message, error);
                    }
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    VerifyPage(pipe, args[2], args[3], 34, "myHomePlugIns.xml");
                    VerifyPage(pipe, args[2], args[3], 6, "myvideo.xml");
                }
                Exchange(pipe, LiveWire.Document("disconnect"));
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
