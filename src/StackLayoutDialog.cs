using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class StackLayoutDialog : Form
    {
        readonly RadioButton vertical=new RadioButton{Text="Untereinander",Checked=true,AutoSize=true};
        readonly RadioButton horizontal=new RadioButton{Text="Nebeneinander",AutoSize=true};
        readonly NumericUpDown gap=new NumericUpDown{Minimum=0,Maximum=1000000,Width=120};
        readonly CheckBox collapse=new CheckBox{Text="Ausgeblendete Einträge lassen nachfolgende Elemente nachrücken",AutoSize=true};
        readonly CheckBox replace=new CheckBox{Text="Vorhandene Bindung oder andere Layoutregel ausdrücklich ersetzen",AutoSize=true};
        readonly Button accept=new Button{Text="Übernehmen",DialogResult=DialogResult.OK,Width=120,Height=34};
        public string Result {get{return "StackLayout("+gap.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+", "+(horizontal.Checked?"Horizontal":"Vertical")+", "+(collapse.Checked?"true":"false")+")";}}
        public bool ReplaceProtected {get{return replace.Checked;}}
        public StackLayoutDialog(SkinControl control)
        {
            Text="Gruppenlayout · "+control.Name;Font=new Font("Segoe UI",10);ClientSize=new Size(700,340);MinimumSize=MaximumSize=Size;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var title=new Label{Text="Automatische Anordnung der direkten Gruppeninhalte",AutoSize=true,Location=new Point(22,20)};Controls.Add(title);
            vertical.Location=new Point(22,62);horizontal.Location=new Point(230,62);Controls.Add(vertical);Controls.Add(horizontal);
            Controls.Add(new Label{Text="Abstand in Skin-Pixeln",AutoSize=true,Location=new Point(22,108)});gap.Location=new Point(230,105);Controls.Add(gap);
            collapse.Location=new Point(22,155);Controls.Add(collapse);replace.Location=new Point(22,195);Controls.Add(replace);
            Controls.Add(new Label{Text="Die Positionen der Kinder bleiben in der XML erhalten.\nDie Gruppe ordnet sie für die Vorschau und in MediaPortal automatisch an.",AutoSize=true,Location=new Point(22,230)});
            accept.Location=new Point(416,288);Controls.Add(accept);var cancel=new Button{Text="Abbrechen",DialogResult=DialogResult.Cancel,Location=new Point(548,288),Width=120,Height=34};Controls.Add(cancel);AcceptButton=accept;CancelButton=cancel;
            StackPreview.Rule rule;bool known=StackPreview.TryRead(control.Resolved("layout").Trim(),out rule);
            if(known){horizontal.Checked=rule.Horizontal;vertical.Checked=!rule.Horizontal;gap.Value=Math.Min(gap.Maximum,rule.Gap);collapse.Checked=rule.Collapse;}
            bool guarded=control.IsBound("layout")||(!known&&control.Get("layout").Trim().Length>0)||(known&&rule.Gap>gap.Maximum);
            replace.Visible=guarded;accept.Enabled=!guarded;replace.CheckedChanged+=(s,e)=>accept.Enabled=!guarded||replace.Checked;
        }
        internal void ChooseForTest(bool row,int spacing,bool hidden){horizontal.Checked=row;vertical.Checked=!row;gap.Value=spacing;collapse.Checked=hidden;}
        internal bool AcceptForTest {get{return accept.Enabled;}}
        internal void ReplaceForTest(){replace.Checked=true;}
        internal void SubmitForTest(){accept.PerformClick();}
    }
    public sealed partial class SkinProject
    {
        public bool SetStackLayout(SkinControl control,string value,bool replaceProtected)
        {
            RequireCurrent(control);if(IsLocked(control))throw new InvalidOperationException("Die Gruppe ist gesperrt.");
            if(control.Get("type")!="group")throw new InvalidOperationException("Zuerst eine Gruppe auswählen.");
            StackPreview.Rule result,previous;if(!StackPreview.TryRead(value,out result))throw new InvalidOperationException("Ungültiges StackLayout.");
            if(control.Node.Children.Count(n=>n.Name=="layout")>1||control.Node.Child("layout")!=null&&control.Node.Child("layout").Children.Count>0)throw new InvalidOperationException("Mehrdeutige oder verschachtelte Layout-Eigenschaft bleibt geschützt.");
            if(!replaceProtected&&(control.IsBound("layout")||control.Get("layout").Trim().Length>0&&!StackPreview.TryRead(control.Resolved("layout"),out previous)))throw new InvalidOperationException("Vorhandene Bindung oder andere Layoutregel zuerst ausdrücklich freigeben.");
            if(control.Get("layout")==value)return false;Edit(control,new System.Collections.Generic.Dictionary<string,string>{{"layout",value}},"Gruppenlayout ändern");return true;
        }
    }
    public sealed partial class StudioForm
    {
        void EditStackLayout()
        {
            if(PendingProperties())throw new InvalidOperationException("Offene Eigenschaftseingaben zuerst übernehmen.");
            if(selected==null||selected.Get("type")!="group")return;if(project.IsLocked(selected))throw new InvalidOperationException("Die Gruppe ist gesperrt.");
            using(var dialog=new StackLayoutDialog(selected))if(dialog.ShowDialog(this)==DialogResult.OK)
            {string key=selected.Key;bool changed=project.SetStackLayout(selected,dialog.Result,dialog.ReplaceProtected);RefreshProject(key);ShowNotice(changed?"Gruppenlayout übernommen. Die Kinder behalten ihre XML-Positionen; Rückgängig stellt die vorherige Regel wieder her.":"Gruppenlayout unverändert.");}
        }
    }
}
