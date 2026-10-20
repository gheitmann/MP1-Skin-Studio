using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class UnsavedSessionDialog : Form
    {
        public UnsavedSessionDialog(string action,string projectName,int modifiedFiles,bool pending)
        {
            Text="Unsaved editor changes";StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
            AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);ClientSize=new Size(620,270);
            var heading=new Label{Text="Save your project before you "+action+"?",Location=new Point(20,18),Size=new Size(580,36),Font=new Font(Font,FontStyle.Bold)};
            var details=new Label{Location=new Point(20,60),Size=new Size(580,100),Text=modifiedFiles+" modified XML file(s). The session also retains editor settings.\r\nSaving a project retains your work and preview state for continued editing.\r\n"+(pending?"Apply or revert pending property inputs first. Save is unavailable while inputs are pending.":"Discard continues without saving this session. Cancel keeps the editor open.")};
            var name=new Label{Text="Project: "+(String.IsNullOrEmpty(projectName)?"Not saved yet":Path.GetFileName(projectName)),Location=new Point(20,166),Size=new Size(580,30),AutoEllipsis=true};
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=64,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,12,12,12),WrapContents=false};
            var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Size=new Size(115,32)};
            var discard=new Button{Text="Discard changes",DialogResult=DialogResult.No,Size=new Size(150,32)};
            var save=new Button{Text="Save project",DialogResult=DialogResult.Yes,Size=new Size(150,32),Enabled=!pending};
            buttons.Controls.Add(cancel);buttons.Controls.Add(discard);buttons.Controls.Add(save);
            Controls.Add(heading);Controls.Add(details);Controls.Add(name);Controls.Add(buttons);
            AcceptButton=cancel;CancelButton=cancel;
        }
    }

    public sealed partial class StudioForm
    {
        internal bool ConfirmSessionTransition(Func<DialogResult> choose,Func<bool> save,Action<Exception> reportError)
        {
            if(!SessionNeedsSaving())return true;
            var choice=choose();
            if(choice==DialogResult.No)return true;
            if(choice!=DialogResult.Yes)return false;
            if(PendingProperties())
            {ShowNotice("Apply or revert pending property inputs before saving. The current session remains open.");return false;}
            try{return save()&&!SessionNeedsSaving();}
            catch(Exception error){reportError(error);return false;}
        }
        bool CanLeaveSession(string action)
        {
            return ConfirmSessionTransition(()=>
            {
                using(var dialog=new UnsavedSessionDialog(action,currentProjectFile,project.Sources.Values.Count(s=>s.Changed),PendingProperties()))
                    return dialog.ShowDialog(this);
            },()=>SaveEditorProject(false),error=>MessageBox.Show(this,error.Message,"Project was not saved",MessageBoxButtons.OK,MessageBoxIcon.Warning));
        }
    }
}
