using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio;
using MP1SkinStudio.Live;

static class FirstStepsProbe
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Results = new List<string>();

    static T Field<T>(object owner, string name)
    {
        return (T)owner.GetType().GetField(name, Private).GetValue(owner);
    }

    static void Call(object owner, string name, params object[] args)
    {
        owner.GetType().GetMethod(name, Private, null, args.Select(arg => arg.GetType()).ToArray(), null).Invoke(owner, args);
    }

    static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Results.Add("PASS " + description);
        Console.WriteLine(Results.Last());
    }

    static void Wait(Func<bool> ready, string description)
    {
        var timer = Stopwatch.StartNew();
        while (!ready() && timer.ElapsedMilliseconds < 20000)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
        if (!ready()) throw new TimeoutException(description);
    }

    static void WithDialog<T>(Action open, Action<T> action) where T : Form
    {
        Exception failure = null;
        bool seen = false;
        using (var timer = new System.Windows.Forms.Timer { Interval = 30 })
        {
            var watch = Stopwatch.StartNew();
            timer.Tick += (sender, args) =>
            {
                var dialog = Application.OpenForms.OfType<T>().FirstOrDefault();
                if (dialog == null && watch.ElapsedMilliseconds < 12000) return;
                timer.Stop();
                try
                {
                    if (dialog == null) throw new TimeoutException("Expected " + typeof(T).Name);
                    seen = true;
                    action(dialog);
                }
                catch (Exception error)
                {
                    failure = error;
                    foreach (var form in Application.OpenForms.Cast<Form>().Where(form => !(form is StudioForm)).ToArray()) form.Close();
                }
            };
            timer.Start();
            open();
        }
        if (failure != null) throw failure;
        if (!seen) throw new InvalidOperationException(typeof(T).Name + " was not opened.");
    }

    static XmlDocument ActivateFixture()
    {
        var endpoint = LiveEndpoint.Discover().Single();
        using (var pipe = new NamedPipeClientStream(".", endpoint.Pipe, PipeDirection.InOut, PipeOptions.Asynchronous))
        {
            pipe.Connect(4000);
            var hello = LiveWire.Document("hello");
            LiveWire.Attr(hello.DocumentElement, "nonce", endpoint.Nonce);
            Exchange(pipe, hello);
            var activate = LiveWire.Document("activate");
            LiveWire.Attr(activate.DocumentElement, "window", 90120);
            Exchange(pipe, activate);
            XmlDocument snapshot = null;
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 10000)
            {
                var current = Exchange(pipe, LiveWire.Document("snapshot"));
                if (String.Equals(Path.GetFileName(current.DocumentElement.GetAttribute("page")),
                    "studio.live.special.xml", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot = current;
                    break;
                }
                Thread.Sleep(150);
            }
            Exchange(pipe, LiveWire.Document("disconnect"));
            if (snapshot == null) throw new InvalidOperationException("MediaPortal did not open the fixture page.");
            return snapshot;
        }
    }

    static XmlDocument Exchange(NamedPipeClientStream pipe, XmlDocument request)
    {
        LiveWire.Write(pipe, request);
        var response = LiveWire.Read(pipe);
        if (response.DocumentElement.GetAttribute("ok") == "false")
            throw new InvalidOperationException(response.DocumentElement.GetAttribute("message"));
        return response;
    }

    static XmlDocument Snapshot(StudioForm studio)
    {
        XmlDocument snapshot = null;
        var client = Field<StudioLiveClient>(studio, "liveClient");
        Action<XmlDocument> receive = response =>
        {
            if (response.DocumentElement.GetAttribute("command") == "snapshot")
                Interlocked.Exchange(ref snapshot, response);
        };
        client.Response += receive;
        try
        {
            client.Send(LiveWire.Document("snapshot"));
            Wait(() => Interlocked.CompareExchange(ref snapshot, null, null) != null, "Native snapshot did not arrive.");
            Wait(() => Field<bool>(studio, "liveVerified"), "Native page did not verify after snapshot.");
            return snapshot;
        }
        finally { client.Response -= receive; }
    }

    static string NativeLabel(XmlDocument snapshot)
    {
        return snapshot.SelectSingleNode("/live/control[@id='21203']/value[@key='label']").InnerText;
    }

    static void ConfirmFrame(StudioForm studio, Func<string, bool> expected)
    {
        Wait(() => expected(Field<string>(studio, "liveLastFrame") ?? "") &&
            Field<int>(studio, "liveAcknowledged") == Field<int>(studio, "liveRevision"),
            Field<Label>(studio, "liveNotice").Text);
    }

    static void VerifySave(StudioForm studio, string output, string expected)
    {
        WithDialog<SaveSkinDialog>(() => Field<Button>(studio, "firstStepsSave").PerformClick(), save =>
        {
            var create = Field<Button>(save, "create");
            Check(save.AcceptButton == create, "First save defaults to creating a complete skin copy");
            string copyFailure = null;
            using (var watchdog = new System.Windows.Forms.Timer { Interval = 100 })
            {
                var watch = Stopwatch.StartNew();
                watchdog.Tick += (sender, args) =>
                {
                    var copy = Application.OpenForms.OfType<SkinCopyDialog>().FirstOrDefault();
                    if (copy == null) return;
                    string status = Field<Label>(copy, "status").Text;
                    if (!status.StartsWith("Copy was not created:") && watch.ElapsedMilliseconds < 45000) return;
                    copyFailure = status;
                    copy.Close();
                };
                watchdog.Start();
                WithDialog<SkinCopyDialog>(() => create.PerformClick(), copy =>
                {
                    Field<TextBox>(copy, "parent").Text = output;
                    Field<TextBox>(copy, "name").Text = "First Steps Output";
                    Field<Button>(copy, "create").PerformClick();
                });
            }
            if (String.IsNullOrEmpty(save.SavedRoot)) throw new InvalidOperationException(copyFailure ?? "Copy not created.");
            Check(File.ReadAllText(Path.Combine(save.SavedRoot, "studio.live.special.xml")).Contains(expected) &&
                File.Exists(Path.Combine(save.SavedRoot, "fonts.xml")), "Saved copy contains edited XML and the skin resources");
            save.Close();
        });
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args[0] == "--verify-saved")
        {
            try
            {
                Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR", args[1]);
                var snapshot = ActivateFixture();
                Check(String.Equals(Path.GetFullPath(snapshot.DocumentElement.GetAttribute("skin")).TrimEnd('\\', '/'),
                    Path.GetFullPath(args[2]).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase) &&
                    NativeLabel(snapshot) == "First Steps native text",
                    "A fresh MediaPortal session loads the saved skin and edited text from disk");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        string output = args[2];
        Directory.CreateDirectory(output);
        try
        {
            Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR", args[1]);
            ActivateFixture();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var studio = new StudioForm(args[0]))
            {
                studio.Show();
                studio.Hide();
                studio.Show();
                studio.Size = studio.MinimumSize;
                Application.DoEvents();
                Field<Button>(studio, "firstStepsToggle").PerformClick();
                var guide = Field<Panel>(studio, "firstSteps");
                var canvas = Field<SkinCanvas>(studio, "canvas");
                using (var bitmap = new Bitmap(studio.Width, studio.Height))
                {
                    studio.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(output, "First-Steps.png"));
                }
                Check(guide.Visible && canvas.Height >= 100,
                    "Guide opens in the main window and leaves a usable canvas at minimum size" +
                    " (guide visible: " + guide.Visible + ", canvas: " + canvas.Size + ")");
                var search = Field<TextBox>(studio, "pageSearch");
                search.Text = "demo.";
                Call(search, "OnKeyDown", new KeyEventArgs(Keys.Enter));
                Check(Field<SkinProject>(studio, "project").Page == "demo.xml", "Ambiguous filename search leaves the active page open");
                search.Text = "demo.groups";
                Call(search, "OnKeyDown", new KeyEventArgs(Keys.Enter));
                Check(Field<SkinProject>(studio, "project").Page == "demo.groups.xml", "Enter opens a page by filename without its XML extension");
                studio.LoadProject(args[0], "demo.xml");
                Field<CheckBox>(studio, "liveToggle").Checked = false;
                var live = guide.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Single(button => button.Text == "Open page from MP");
                live.PerformClick();
                Wait(() => Field<bool>(studio, "liveVerified"), "Open MP page did not connect and verify: " + Field<Label>(studio, "liveNotice").Text);
                var project = Field<SkinProject>(studio, "project");
                Check(project.Page == "studio.live.special.xml" && Field<Panel>(studio, "liveHost").Visible,
                    "One guide action connects and opens the active MediaPortal page");

                var control = project.Controls.Single(item => item.Get("id") == "21203");
                Call(studio, "SelectControl", control);
                Call(studio, "UpdatePropertyHelp", "label");
                Check(Field<Label>(studio, "propertyHelp").Text.Contains("live preview available"), "Linked text property shows live availability");
                Call(studio, "UpdatePropertyHelp", "hyperlink");
                Check(Field<Label>(studio, "propertyHelp").Text.Contains("saved skin only"), "Unavailable property is identified before editing");
                var baseline = Snapshot(studio);
                string source = project.Resolve(project.Page);
                byte[] original = File.ReadAllBytes(source);
                const string edited = "First Steps native text";
                WithDialog<TextEditDialog>(() => Field<Button>(studio, "firstStepsText").PerformClick(), dialog =>
                {
                    Field<TextBox>(dialog, "editor").Text = edited;
                    Field<Button>(dialog, "accept").PerformClick();
                });
                ConfirmFrame(studio, frame => frame.Contains(edited));
                var native = Snapshot(studio);
                Check(NativeLabel(native) == edited && native.DocumentElement.GetAttribute("epoch") == baseline.DocumentElement.GetAttribute("epoch"),
                    "Guided text edit reaches the same native page without a reload");
                VerifySave(studio, output, edited);
                Call(studio, "UndoEditor", false);
                ConfirmFrame(studio, frame => frame.Length > 0 && !frame.Contains(edited));
                Check(NativeLabel(Snapshot(studio)) == NativeLabel(baseline), "Undo restores native text after saving the copy");
                Check(File.ReadAllBytes(source).SequenceEqual(original), "Editing, saving and Undo preserve the source page bytes");
                Call(studio, "DisconnectLive");
                Wait(() => Field<StudioLiveClient>(studio, "liveClient") == null, "Disconnect did not finish.");
            }
            return 0;
        }
        catch (Exception error) { Results.Add(error.ToString()); Console.Error.WriteLine(error); return 1; }
        finally { File.WriteAllLines(Path.Combine(output, "Results.txt"), Results); }
    }
}
