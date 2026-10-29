using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class SaveSkinDialog:Form
    {
        readonly SkinProject project;readonly TextBox target=new TextBox();readonly ListBox files=new ListBox();readonly Label status=new Label();
        readonly Button save=new Button(),create=new Button(),choose=new Button(),refresh=new Button(),review=new Button(),close=new Button();
        SkinSavePlan plan;bool busy;
        public string SavedRoot {get;private set;}
        public SaveSkinDialog(SkinProject source)
        {
            project=source;Text="Save skin copy — MP1 Skin Studio";StartPosition=FormStartPosition.CenterParent;Size=new Size(900,590);MinimumSize=new Size(820,520);Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);
            var top=new Panel{Dock=DockStyle.Top,Height=142,Padding=new Padding(12)};
            var description=new Label{Dock=DockStyle.Top,Height=49,Text="Keep your edits in a separate complete skin copy. The current source and live preview continue unchanged.\r\nUpdates are checked against the previous save and backed up automatically."};
            target.Dock=DockStyle.Top;target.Height=27;target.ReadOnly=true;target.Text=source.Editor.SavedSkinRoot;target.BackColor=Color.FromArgb(20,26,35);target.ForeColor=ForeColor;target.AccessibleName="Saved skin copy location";
            var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,WrapContents=false};top.Controls.Add(actions);top.Controls.Add(target);top.Controls.Add(description);
            create.Text="New skin copy …";create.Width=151;create.Click+=(s,e)=>CreateCopy();choose.Text="Choose saved copy …";choose.Width=172;choose.Click+=(s,e)=>ChooseCopy();refresh.Text="Refresh checks";refresh.Width=133;refresh.Click+=(s,e)=>RefreshPlan();review.Text="Review XML …";review.Width=128;review.Click+=(s,e)=>{if(plan!=null)using(var dialog=new ChangeReviewDialog(()=>SkinSavePlan.Capture(project,String.IsNullOrWhiteSpace(target.Text)?null:target.Text).Review(),false)){dialog.Text="Review output skin changes";dialog.ShowDialog(this);}};
            actions.Controls.AddRange(new Control[]{create,choose,refresh,review});
            files.Dock=DockStyle.Fill;files.BorderStyle=BorderStyle.None;files.IntegralHeight=false;files.HorizontalScrollbar=true;files.BackColor=Color.FromArgb(20,26,35);files.ForeColor=ForeColor;files.AccessibleName="XML changes in saved copy";
            var footer=new Panel{Dock=DockStyle.Bottom,Height=122,Padding=new Padding(12)};status.Dock=DockStyle.Fill;status.Text="";var commands=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38,FlowDirection=FlowDirection.RightToLeft};close.Text="Close";close.Width=100;close.Click+=(s,e)=>Close();save.Text="Update saved copy";save.Width=169;save.Click+=async(s,e)=>await UpdateCopy();commands.Controls.AddRange(new Control[]{close,save});footer.Controls.Add(status);footer.Controls.Add(commands);
            foreach(var button in new[]{save,create,choose,refresh,review,close}){button.Height=30;button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(35,48,64);button.ForeColor=ForeColor;button.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);}
            Controls.Add(new DarkScrollHost(files));Controls.Add(top);Controls.Add(footer);AcceptButton=save;CancelButton=close;FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};RefreshPlan();
        }
        void RefreshPlan()
        {
            if(busy)return;files.Items.Clear();plan=null;
            try
            {
                plan=SkinSavePlan.Capture(project,String.IsNullOrWhiteSpace(target.Text)?null:target.Text);files.Items.AddRange(plan.Files.Cast<object>().ToArray());
                status.Text=plan.Target==null?"Choose New skin copy for the first save. You can update that output copy on later saves.":plan.HasConflicts?"Target conflicts found. Nothing will be overwritten. Review the files or create a new copy.":plan.ChangeCount==0?"The saved copy is up to date for the loaded session XML.":plan.ChangeCount+" XML file(s) ready. Update writes only these files and creates a ZIP backup of their previous bytes.";
            }
            catch(Exception ex){status.Text="Cannot use this saved copy: "+ex.Message;}
            save.Enabled=plan!=null&&plan.Target!=null&&!plan.HasConflicts&&plan.ChangeCount>0;review.Enabled=plan!=null&&plan.Files.Length>0;
        }
        void CreateCopy()
        {
            using(var dialog=new SkinCopyDialog(project,true))
            {if(dialog.ShowDialog(this)!=DialogResult.OK)return;target.Text=SavedRoot=dialog.CreatedRoot;RefreshPlan();status.Text="Complete skin copy saved: "+SavedRoot+"\r\nContinue editing here; the live connection and Undo history are retained.";}
        }
        void ChooseCopy()
        {
            using(var dialog=new FolderBrowserDialog{Description="Choose a skin previously created by Save skin copy",SelectedPath=Directory.Exists(target.Text)?target.Text:project.Root})
            {if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{SkinSavePlan.Capture(project,dialog.SelectedPath);target.Text=SavedRoot=dialog.SelectedPath;RefreshPlan();}catch(Exception ex){status.Text=ex.Message;}}
        }
        async Task UpdateCopy()
        {
            if(busy||plan==null||!save.Enabled)return;var captured=plan;busy=true;foreach(var button in new[]{save,create,choose,refresh,review,close})button.Enabled=false;status.Text="Saving reviewed XML and verifying output. Please wait …";
            string backup=null;Exception failure=null;
            try{backup=await Task.Factory.StartNew(()=>captured.Save());SavedRoot=target.Text;}catch(Exception ex){failure=ex;}
            finally{busy=false;foreach(var button in new[]{create,choose,refresh,close})button.Enabled=true;RefreshPlan();}
            status.Text=failure==null?(backup==null?"The saved copy is already up to date.":"Changes saved. Previous XML and save receipt are backed up in:\r\n"+backup):"Save did not complete: "+failure.Message;
        }
    }
    public sealed partial class StudioForm
    {
        void SaveSkinCopy()
        {
            if(project==null)return;
            if(PendingProperties())Apply();
            using(var dialog=new SaveSkinDialog(project))
            {
                dialog.ShowDialog(this);
                if(!String.IsNullOrEmpty(dialog.SavedRoot)){project.Editor.SavedSkinRoot=dialog.SavedRoot;if(!copiedSkins.Contains(dialog.SavedRoot))copiedSkins.Add(dialog.SavedRoot);RefreshSkinChoices();UpdateProjectCaption();ShowNotice("Output skin copy: "+dialog.SavedRoot+". Save Project also retains your Undo history and this output location.");}
            }
        }
    }
}
