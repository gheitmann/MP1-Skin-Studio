using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class SkinCopyDialog : Form
    {
        readonly SkinProject project;readonly TextBox name=new TextBox(),parent=new TextBox();
        readonly CheckBox include=new CheckBox(),open=new CheckBox();readonly Label status=new Label();
        readonly Button create=new Button(),browse=new Button(),cancel=new Button();readonly ProgressBar progress=new ProgressBar();
        CancellationTokenSource cancellation;bool busy;readonly bool saveMode;
        public string CreatedRoot {get;private set;}
        public bool OpenCopy {get{return open.Checked;}}
        public SkinCopyDialog(SkinProject source):this(source,false){}
        public SkinCopyDialog(SkinProject source,bool saving)
        {
            project=source;saveMode=saving;Text=saving?"Save changes to a new skin copy":"Copy skin / create a new skin";StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
            AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);ClientSize=new Size(650,380);
            BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);
            var info=new Label{Text="Create an independent copy with all XML, images, fonts and themes.\r\nThe source skin stays unchanged.",Location=new Point(20,15),Size=new Size(610,44)};Controls.Add(info);
            Controls.Add(new Label{Text="New skin name",Location=new Point(20,70),AutoSize=true});name.SetBounds(20,94,610,26);name.Text=Path.GetFileName(source.Root.TrimEnd('\\','/'))+" - Copy";Controls.Add(name);
            Controls.Add(new Label{Text="Create inside folder",Location=new Point(20,130),AutoSize=true});parent.SetBounds(20,154,505,26);parent.Text=Directory.GetParent(source.Root).FullName;Controls.Add(parent);
            browse.Text="Browse …";browse.SetBounds(535,152,95,29);browse.Click+=(s,e)=>{using(var dialog=new FolderBrowserDialog{Description="Choose the parent folder for the new skin",SelectedPath=parent.Text})if(dialog.ShowDialog(this)==DialogResult.OK)parent.Text=dialog.SelectedPath;};Controls.Add(browse);
            int changed=source.Sources.Values.Count(s=>s.Changed);include.Text="Include applied editor changes ("+changed+" XML files)";include.Checked=true;include.SetBounds(20,196,610,25);Controls.Add(include);
            open.Text="Open the new skin when copying finishes";open.Checked=true;open.SetBounds(20,226,610,25);Controls.Add(open);
            if(saveMode){include.Enabled=false;open.Checked=false;open.Enabled=false;open.Text="Continue editing the current source and live preview";status.Text="The output copy is saved separately. Your editor undo history and live connection continue.";}
            progress.SetBounds(20,261,610,12);progress.Style=ProgressBarStyle.Marquee;progress.Visible=false;Controls.Add(progress);
            status.SetBounds(20,281,610,42);status.Text=saveMode?"Save includes applied XML changes. The current source, Undo history and live connection continue.":"Pending property inputs must be applied first. Preview settings and undo history stay in the current project.";Controls.Add(status);
            create.Text="Create copy";create.SetBounds(375,333,125,32);create.Click+=async(s,e)=>await CreateCopy();Controls.Add(create);
            cancel.Text="Cancel";cancel.SetBounds(510,333,120,32);cancel.Click+=(s,e)=>{if(busy){cancellation.Cancel();status.Text="Cancelling and removing the unfinished copy …";}else{DialogResult=DialogResult.Cancel;Close();}};Controls.Add(cancel);
            foreach(var field in new[]{name,parent}){field.BackColor=Color.FromArgb(20,26,35);field.ForeColor=ForeColor;field.BorderStyle=BorderStyle.FixedSingle;}
            foreach(var button in new[]{browse,create,cancel}){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(35,48,64);button.ForeColor=ForeColor;button.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);}
            AcceptButton=create;CancelButton=cancel;
            FormClosing+=(s,e)=>{if(busy){e.Cancel=true;cancellation.Cancel();status.Text="Cancelling and removing the unfinished copy …";}};
        }
        async Task CreateCopy()
        {
            if(busy)return;
            try
            {
                SkinCopyPlan.ValidateName(name.Text);string destination=parent.Text,skinName=name.Text;var plan=SkinCopyPlan.Capture(project,include.Checked);var savePlan=saveMode?SkinSavePlan.Capture(project):null;
                cancellation=new CancellationTokenSource();busy=true;SetInputs(false);progress.Visible=true;status.Text="Copying the complete skin. Large media folders may take a moment …";
                CreatedRoot=await Task.Factory.StartNew(()=>savePlan==null?plan.CopyTo(destination,skinName,cancellation.Token):savePlan.CreateCopy(destination,skinName,cancellation.Token));
                busy=false;DialogResult=DialogResult.OK;Close();
            }
            catch(OperationCanceledException){status.Text="Copy cancelled. The source skin and existing folders are unchanged.";}
            catch(Exception error){status.Text="Copy was not created: "+error.Message;}
            finally{busy=false;if(cancellation!=null){cancellation.Dispose();cancellation=null;}if(!IsDisposed){SetInputs(true);progress.Visible=false;}}
        }
        void SetInputs(bool enabled){name.Enabled=parent.Enabled=browse.Enabled=create.Enabled=enabled;include.Enabled=open.Enabled=enabled&&!saveMode;}
    }
    public sealed partial class StudioForm
    {
        void ShowSkinCopy()
        {
            if(project==null)return;
            if(PendingProperties()){ShowNotice("Apply or revert pending property inputs before copying the skin.");return;}
            using(var dialog=new SkinCopyDialog(project))
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                copiedSkins.Add(dialog.CreatedRoot);
                RefreshSkinChoices();
                if(dialog.OpenCopy&&CanSwitch())InstallProject(new SkinProject(dialog.CreatedRoot){Theme=project.Theme},project.Page,null);
                ShowNotice("Skin copy created: "+dialog.CreatedRoot);
            }
        }
    }
}
