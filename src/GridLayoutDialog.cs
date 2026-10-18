using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class GridLayoutDialog : Form
    {
        readonly NumericUpDown columns=Number(),rows=Number(),xGap=Number(),yGap=Number();
        readonly CheckBox vertical=new CheckBox{Text="Fill down columns first",AutoSize=true};
        readonly CheckBox replace=new CheckBox{Text="Explicitly replace an existing binding or different layout rule",AutoSize=true};
        readonly Button accept=new Button{Text="Apply",DialogResult=DialogResult.OK,Width=120,Height=34};
        readonly bool guarded;
        static NumericUpDown Number(){return new NumericUpDown{Minimum=0,Maximum=Int32.MaxValue,Width=145};}
        public string Result {get{return "GridLayout("+String.Join(", ",new[]{columns.Value.ToString(CultureInfo.InvariantCulture),rows.Value.ToString(CultureInfo.InvariantCulture),xGap.Value.ToString(CultureInfo.InvariantCulture),yGap.Value.ToString(CultureInfo.InvariantCulture),vertical.Checked?"Vertical":"Horizontal"})+")";}}
        public bool ReplaceProtected {get{return replace.Checked;}}
        public GridLayoutDialog(SkinControl control)
        {
            Text="Grid layout - "+control.Name;Font=new Font("Segoe UI",10);ClientSize=new Size(700,390);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            Controls.Add(new Label{Text="Arrange direct group contents in a grid",AutoSize=true,Location=new Point(22,20)});
            var inputs=new[]{columns,rows,xGap,yGap};var labels=new[]{"Columns","Rows (0 = calculate from columns)","Horizontal spacing in skin pixels","Vertical spacing in skin pixels"};
            for(int i=0;i<inputs.Length;i++){int y=62+i*38;Controls.Add(new Label{Text=labels[i],AutoSize=true,Location=new Point(22,y+3)});inputs[i].Location=new Point(355,y);Controls.Add(inputs[i]);inputs[i].ValueChanged+=(s,e)=>UpdateAccept();}
            vertical.Location=new Point(22,220);Controls.Add(vertical);replace.Location=new Point(22,258);Controls.Add(replace);
            Controls.Add(new Label{Text="A positive row count takes precedence over columns.\nChild positions remain in the XML; undo restores the previous rule.",AutoSize=true,Location=new Point(22,294)});
            accept.Location=new Point(416,342);Controls.Add(accept);var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(548,342),Width=120,Height=34};Controls.Add(cancel);AcceptButton=accept;CancelButton=cancel;
            StackPreview.GridRule rule;bool known=StackPreview.TryReadGrid(control.Resolved("layout").Trim(),out rule);
            columns.Value=known?rule.Columns:1;rows.Value=known?rule.Rows:0;
            if(known){xGap.Value=(decimal)rule.XGap;yGap.Value=(decimal)rule.YGap;vertical.Checked=rule.Vertical;}
            guarded=control.IsBound("layout")||!known&&control.Get("layout").Trim().Length>0;
            replace.Visible=guarded;replace.CheckedChanged+=(s,e)=>UpdateAccept();UpdateAccept();
        }
        void UpdateAccept(){accept.Enabled=(columns.Value>0||rows.Value>0)&&(!guarded||replace.Checked);}
        internal void ChooseForTest(int cols,int rowCount,int horizontalGap,int verticalGap,bool down){columns.Value=cols;rows.Value=rowCount;xGap.Value=horizontalGap;yGap.Value=verticalGap;vertical.Checked=down;}
        internal bool AcceptForTest {get{return accept.Enabled;}}
        internal void ReplaceForTest(){replace.Checked=true;}
        internal void SubmitForTest(){accept.PerformClick();}
    }
    public sealed partial class SkinProject
    {
        public bool SetGridLayout(SkinControl control,string value,bool replaceProtected)
        {
            RequireCurrent(control);if(IsLocked(control))throw new InvalidOperationException("The group is locked.");
            if(control.Get("type")!="group")throw new InvalidOperationException("Select a group first.");
            StackPreview.GridRule result,previous;if(!StackPreview.TryReadGrid(value.Trim(),out result))throw new InvalidOperationException("Invalid GridLayout.");
            if(control.Node.Children.Count(n=>n.Name=="layout")>1||control.Node.Child("layout")!=null&&control.Node.Child("layout").Children.Count>0)throw new InvalidOperationException("Ambiguous or nested layout properties remain protected.");
            if(!replaceProtected&&(control.IsBound("layout")||control.Get("layout").Trim().Length>0&&!StackPreview.TryReadGrid(control.Resolved("layout").Trim(),out previous)))throw new InvalidOperationException("Explicitly allow replacement of existing bindings or other layout rules first.");
            if(control.Get("layout")==value)return false;Edit(control,new System.Collections.Generic.Dictionary<string,string>{{"layout",value}},"Change grid layout");return true;
        }
    }
    public sealed partial class StudioForm
    {
        void EditGridLayout()
        {
            if(PendingProperties())throw new InvalidOperationException("Apply pending property edits first.");
            if(selected==null||selected.Get("type")!="group")return;if(project.IsLocked(selected))throw new InvalidOperationException("The group is locked.");
            using(var dialog=new GridLayoutDialog(selected))if(dialog.ShowDialog(this)==DialogResult.OK)
            {string key=selected.Key;bool changed=project.SetGridLayout(selected,dialog.Result,dialog.ReplaceProtected);RefreshProject(key);ShowNotice(changed?"Grid layout applied. Children retain their XML positions; undo restores the previous rule.":"Grid layout unchanged.");}
        }
    }
}
