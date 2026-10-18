using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void EditElementText(SkinControl control)
        {
            if(PendingProperties())throw new InvalidOperationException("Offene Eigenschaftseingaben zuerst übernehmen.");
            if(control==null)return;var info=project.InspectTextEdit(control);if(!info.Allowed){ShowNotice(info.Reason);return;}
            SelectControl(control);
            using(var dialog=new TextEditDialog(info,state))if(dialog.ShowDialog(this)==DialogResult.OK)
            {
                string key=control.Key;bool changed=project.SetControlText(control,dialog.Result,dialog.ReplaceProtected);RefreshProject(key);ShowNotice(changed?"Text übernommen. Mit Rückgängig vollständig wiederherstellen.":"Text unverändert.");
            }
        }
        void AddTextCommands(ContextMenuStrip menu)
        {
            menu.Items.Add(new ToolStripSeparator());var item=menu.Items.Add("Text bearbeiten …",null,(s,e)=>Attempt(()=>EditElementText(selected)));
            item.Enabled=SelectedControls().Length==1&&selected!=null&&project.InspectTextEdit(selected).Allowed;
        }
        bool AllowCanvasGesture()
        {
            if(!PendingProperties())return true;ShowNotice("Offene Eigenschaftseingaben zuerst übernehmen; die aktuelle Auswahl bleibt erhalten.");return false;
        }
        public void RenderTextEditorTo(string path)
        {var control=project.Controls.First(c=>c.Name=="Time");using(var dialog=new TextEditDialog(project.InspectTextEdit(control),state))dialog.RenderTo(path);}
    }
    public sealed partial class SkinCanvas
    {
        public Func<bool> AllowGesture;
        public event Action<SkinControl> TextRequest;
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);if(Project==null||e.Button!=MouseButtons.Left||NavigationMode||panning)return;
            dragging=false;dragRects.Clear();Capture=false;Cursor=Cursors.Default;Invalidate();
            if(AllowGesture!=null&&!AllowGesture())return;
            UpdateTransform();var p=Logical(e.Location);if(p.X<0||p.Y<0||p.X>=Project.Width||p.Y>=Project.Height)return;
            var hit=Project.Controls.LastOrDefault(c=>Drawn(c)&&BoundsOf(c).Contains(p));if(hit!=null&&TextRequest!=null)TextRequest(hit);
        }
        internal void ExerciseTextDoubleClick(SkinControl control)
        {var r=BoundsOf(control);var point=PointForTest(r.X+r.Width/2,r.Y+r.Height/2);OnMouseDown(new MouseEventArgs(MouseButtons.Left,2,point.X,point.Y,0));OnMouseDoubleClick(new MouseEventArgs(MouseButtons.Left,2,point.X,point.Y,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,2,point.X,point.Y,0));}
    }
}
