using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        Panel firstSteps;
        Button firstStepsToggle, firstStepsText, firstStepsSave;
        Label firstStepsContext;
        string helpProperty;
        Panel previewInformation;
        Button previewInformationToggle;
        Control previewInformationContent;
        bool? previewInformationExpanded;

        Panel BuildFirstSteps()
        {
            var guide = new Panel
            {
                Dock = DockStyle.Top, Height = 77, Padding = new Padding(20, 0, 20, 7),
                BackColor = panel, Visible = false, AccessibleName = "First steps"
            };
            firstStepsContext = new Label
            {
                Dock = DockStyle.Top, Height = 28, ForeColor = muted, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var choose = ButtonFor("Choose skin", () => skinPicker.OpenChoices());
            var find = ButtonFor("Find page", FocusPageSearch);
            var findElement = ButtonFor("Find element", FocusElementSearch);
            firstStepsText = ButtonFor("Edit selected text …", () => EditElementText(selected));
            firstStepsSave = ButtonFor("Save skin copy …", SaveSkinCopy, true);
            var live = ButtonFor("Open page from MP", OpenPageFromLive);
            tips.SetToolTip(choose, "Choose an installed skin or the bundled demo.");
            tips.SetToolTip(find, "Filter page filenames. Press Enter to open an exact match or the only result (Ctrl+L).");
            tips.SetToolTip(findElement, "Filter controls on the active page (Ctrl+F).");
            tips.SetToolTip(firstStepsSave, "Create a complete copy for use in MediaPortal. Save Project keeps the editing session and Undo history.");
            tips.SetToolTip(live, "Connect to an already running MediaPortal and open the page it is showing.");
            actions.Controls.AddRange(new Control[] { choose, live, find, findElement, firstStepsText, firstStepsSave });
            guide.Controls.Add(actions);
            guide.Controls.Add(firstStepsContext);
            return guide;
        }

        void ToggleFirstSteps()
        {
            firstSteps.Visible = !firstSteps.Visible;
            firstStepsToggle.Text = firstSteps.Visible ? "Hide first steps" : "First steps";
            UpdatePreviewInformationSize();
            UpdateFirstSteps();
        }

        Panel BuildPreviewInformation()
        {
            previewInformation = new Panel { Dock = DockStyle.Bottom, Padding = new Padding(8, 4, 8, 4), BackColor = panel };
            previewInformationToggle = ButtonFor("", () =>
            {
                previewInformationExpanded = previewInformation.Height < 139;
                UpdatePreviewInformationSize();
            });
            previewInformationToggle.Dock = DockStyle.Top;
            previewInformationToggle.Height = 24;
            previewInformationToggle.TextAlign = ContentAlignment.MiddleLeft;
            diagnostics.Dock = DockStyle.Fill;
            diagnostics.Multiline = true;
            diagnostics.ReadOnly = true;
            diagnostics.ScrollBars = ScrollBars.Vertical;
            diagnostics.BorderStyle = BorderStyle.None;
            diagnostics.BackColor = panel;
            diagnostics.ForeColor = muted;
            diagnostics.Font = new Font("Segoe UI", 9);
            previewInformationContent = new DarkScrollHost(diagnostics);
            previewInformation.Controls.Add(previewInformationContent);
            previewInformation.Controls.Add(previewInformationToggle);
            return previewInformation;
        }

        void UpdatePreviewInformationSize()
        {
            if (previewInformation == null) return;
            bool expanded = previewInformationExpanded ?? ClientSize.Height >= (firstSteps.Visible ? 880 : 780);
            previewInformationContent.Visible = expanded;
            previewInformation.Height = expanded ? 139 : 32;
            previewInformationToggle.Text = expanded ? "▾  Preview information" : "▸  Preview information";
        }

        void UpdateFirstSteps()
        {
            if (firstSteps == null || !firstSteps.Visible) return;
            firstStepsSave.Enabled = project != null;
            bool canEditText = project != null && selected != null && canvas.SelectedKeys.Count == 1 &&
                project.InspectTextEdit(selected).Allowed;
            bool pending = PendingProperties();
            firstStepsText.Enabled = canEditText && !pending;
            string context = project == null ? "Choose a skin or open the page currently displayed in MediaPortal." :
                Path.GetFileName(project.Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) +
                "  ·  " + project.Page + "  ·  " +
                (pending ? "Apply Properties or Revert Inputs to finish editing this element." :
                 selected == null ? "Select an element in the preview or element tree." :
                 canEditText ? "Edit the selected text, or drag the element to move it. Undo reverses either change." :
                 "Use Properties to edit. Right-click the preview to pick between overlapping elements.");
            firstStepsContext.Text = context;
            tips.SetToolTip(firstStepsContext, context);
        }

        void FocusPageSearch()
        {
            if (pageSearch.Focus()) pageSearch.SelectAll();
        }

        void FocusElementSearch()
        {
            if (elementSearch.Focus()) elementSearch.SelectAll();
        }

        void UpdatePropertyHelp(string key)
        {
            helpProperty = key;
            propertyHelp.Text = PropertyPresentation.Help(key);
            if (!LiveWire.Fields.Contains(key))
            {
                propertyHelp.Text += "\nMediaPortal: saved skin only.";
                return;
            }
            if (!liveVerified || liveBinding == null || canvas.SelectedKeys.Count == 0) return;
            foreach (var control in SelectedControls())
            {
                string original = LiveBinding.OriginalKey(control);
                if (original == null) return; // Inserted controls use the live creation path.
                LiveItem[] matches;
                if (!liveBinding.Matches.TryGetValue(original, out matches))
                {
                    propertyHelp.Text += "\nMediaPortal: element needs a live link.";
                    return;
                }
                foreach (var match in matches)
                {
                    if (match.Supported.Contains(key)) continue;
                    propertyHelp.Text += "\nMediaPortal: saved skin only for this selection.";
                    return;
                }
            }
            propertyHelp.Text += "\nMediaPortal: live preview available.";
        }
    }
}
