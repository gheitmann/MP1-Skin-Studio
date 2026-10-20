using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        Label zoomLabel;Button protectButton;
        Panel BuildViewBar()
        {
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,WrapContents=false,Padding=new Padding(8,1,0,0)};
            var minus=ButtonFor("−",()=>canvas.StepZoom(false));minus.Width=32;minus.Margin=new Padding(0,0,4,0);bar.Controls.Add(minus);
            zoomLabel=new Label{Text="Fit",Width=57,Height=34,TextAlign=ContentAlignment.MiddleCenter,ForeColor=ink};bar.Controls.Add(zoomLabel);
            var plus=ButtonFor("+",()=>canvas.StepZoom(true));plus.Width=32;bar.Controls.Add(plus);
            var fit=ButtonFor("Fit",canvas.FitView);fit.Width=85;bar.Controls.Add(fit);
            var native=ButtonFor("100 %",()=>{if(project!=null)canvas.NativeView();});native.Width=65;bar.Controls.Add(native);
            protectButton=ButtonFor("Element ▾",()=>ShowProtectionMenu(protectButton));protectButton.Width=101;bar.Controls.Add(protectButton);
            var check=ButtonFor("Audit",ShowAudit);check.Width=72;bar.Controls.Add(check);tips.SetToolTip(check,"Audit the open page and includes; jump directly to findings. Shortcut: F8.");
            var hint=new Label{Text="Mouse wheel: Zoom · Middle button: Pan",Width=213,Height=34,TextAlign=ContentAlignment.MiddleLeft,ForeColor=muted};bar.Controls.Add(hint);
            tips.SetToolTip(bar,"Mouse wheel over canvas: zoom around the pointer. Drag middle button: pan. Escape cancels dragging.");
            tips.SetToolTip(protectButton,"Change layer order, remove, lock or hide elements in the editor. Also available through the element-tree context menu.");
            canvas.ViewChanged+=()=>{UpdateViewTools();UpdateProjectCaption();};canvas.Resize+=(s,e)=>UpdateViewTools();
            tree.NodeMouseClick+=(s,e)=>
            {
                if(e.Button!=MouseButtons.Right||!(e.Node.Tag is SkinControl))return;
                if(PendingProperties()){SyncElementTreeSelection();ShowNotice("Apply pending property edits first.");return;}
                var control=(SkinControl)e.Node.Tag;
                if(!canvas.SelectedKeys.Contains(control.Key))SelectControl(control);
                var menu=ProtectionMenu();menu.Show(tree,e.Location);
            };
            return bar;
        }
        void UpdateViewTools(){if(zoomLabel!=null)zoomLabel.Text=canvas.ZoomText;}
        string EditorMark(SkinControl control)
        {return (project.IsLocked(control)?"[L] ":(project.IsPositionLocked(control)?"[P] ":"")+(project.IsSizeLocked(control)?"[S] ":""))+(project.IsEditorHidden(control)?"[H] ":"");}
        void ShowProtectionMenu(Control anchor)
        {if(project==null)return;var menu=ProtectionMenu();menu.Show(anchor,new Point(0,anchor.Height));}
        ContextMenuStrip ProtectionMenu()
        {
            var menu=StudioMenus.For(this,"protection");var controls=SelectedControls();
            bool locked=controls.Length>0&&controls.All(c=>project.Editor.Locked.Contains(c.Key));
            bool hidden=controls.Length>0&&controls.All(c=>project.Editor.Hidden.Contains(c.Key));
            var lockItem=menu.Items.Add(locked?"Unlock selection":"Lock selection",null,(s,e)=>SetProtection(true,!locked));lockItem.Enabled=controls.Length>0;
            AddGeometryLockMenu(menu,controls,"position");AddGeometryLockMenu(menu,controls,"size");
            var hideItem=menu.Items.Add(hidden?"Show selection again":"Hide selection in editor only",null,(s,e)=>SetProtection(false,!hidden));hideItem.Enabled=controls.Length>0;
            AddStructureCommands(menu);AddGroupCommands(menu);AddTextCommands(menu);AddVisibilityCommands(menu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Show all editor-hidden elements",null,(s,e)=>Attempt(()=>{RequireAppliedProperties();project.Editor.Hidden.Clear();RefreshProject(selected==null?null:selected.Key);})).Enabled=project.Editor.Hidden.Count>0;
            menu.Items.Add("Unlock all elements",null,(s,e)=>Attempt(()=>{RequireAppliedProperties();project.Editor.Locked.Clear();project.Editor.PositionLocked.Clear();project.Editor.SizeLocked.Clear();RefreshProject(selected==null?null:selected.Key);})).Enabled=project.Editor.Locked.Count+project.Editor.PositionLocked.Count+project.Editor.SizeLocked.Count>0;
            return menu;
        }
        void SetProtection(bool locking,bool enabled)
        {
            if(PendingProperties()){ShowNotice("Apply pending property edits before changing element protection or editor visibility.");return;}
            if(project==null)return;var flags=locking?project.Editor.Locked:project.Editor.Hidden;
            foreach(var control in SelectedControls()){if(enabled)flags.Add(control.Key);else flags.Remove(control.Key);}
            RefreshProject(selected==null?null:selected.Key);
            ShowNotice(locking?"Element protection updated. [L] in the tree means locked; child controls are also protected.":"Editor view updated. [H] in the tree means hidden; XML visibility is preserved.");
        }
        void UpdateProtectionTools()
        {
            bool locked=project!=null&&SelectedControls().Any(c=>project.IsLocked(c));
            foreach(var pair in fields){bool bound=(pair.Key=="posX"||pair.Key=="posY")&&SelectedControls().Any(c=>{PropertyValue p;return c.Properties.TryGetValue(pair.Key,out p)&&p.Expression.Length>0;});bool protectedValue=bound||SelectedControls().Any(c=>project.IsPropertyLocked(c,pair.Key));pair.Value.ReadOnly=protectedValue;pair.Value.ForeColor=protectedValue?muted:ink;}
            foreach(Control row in properties.Controls)foreach(Control item in row.Controls)if(item is Button)item.Enabled=!locked;
            unlock.Enabled=!locked;
            if(protectButton!=null)protectButton.Enabled=project!=null;
            UpdateResetTools();
        }
        public void PrepareViewportPreview()
        {
            PrepareMultiPreview();SetProtection(true,true);
            canvas.NativeView();
            ShowNotice("[L] = locked · Mouse wheel zooms · Middle button pans · Fit shows the full skin");
        }
        void AddGeometryLockMenu(ContextMenuStrip menu,SkinControl[] controls,string kind)
        {
            var flags=kind=="position"?project.Editor.PositionLocked:project.Editor.SizeLocked;bool active=controls.Length>0&&controls.All(c=>flags.Contains(c.Key));
            var item=menu.Items.Add((active?"Unlock ":"Lock ")+kind+" only",null,(s,e)=>Attempt(()=>SetGeometryProtection(kind,!active)));item.Enabled=controls.Length>0;((ToolStripMenuItem)item).Checked=active;
        }
        void SetGeometryProtection(string kind,bool enabled)
        {
            RequireAppliedProperties();var flags=kind=="position"?project.Editor.PositionLocked:kind=="size"?project.Editor.SizeLocked:null;if(flags==null)throw new InvalidOperationException("Unknown geometry lock.");
            foreach(var control in SelectedControls()){if(enabled)flags.Add(control.Key);else flags.Remove(control.Key);}RefreshProject(selected==null?null:selected.Key);
            ShowNotice("Geometry protection updated. [P] locks position; [S] locks size. Inherited group locks must be removed on the group.");
        }
        void RunViewportUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");canvas.Update();
            verify(!SessionNeedsSaving()&&!Text.EndsWith("*"),"Opening a skin keeps initial fallback focus clean");
            var anchor=new Point(canvas.Width/2+57,canvas.Height/2-31);var delta=canvas.ExerciseZoom(anchor,240);
            verify(Math.Abs(delta.X)<.01&&Math.Abs(delta.Y)<.01&&project.Editor.Zoom>1,"Mouse-wheel zoom preserves the logical point under the pointer");
            double oldPan=project.Editor.PanX;canvas.ExercisePan(70,30);
            verify(project.Editor.PanX>oldPan&&!project.Dirty&&project.Undo.Count==0,"Middle-button panning changes the view without XML edits or undo steps");
            verify(SessionNeedsSaving(),"View-only changes mark the project for saving");
            canvas.NativeView();verify(canvas.ZoomText=="100 %","The 100-percent button displays one skin pixel per canvas pixel");
            canvas.FitView();verify(project.Editor.Zoom==1&&project.Editor.PanX==0&&project.Editor.PanY==0&&!SessionNeedsSaving(),"Fit restores the initial view and saved-state status");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SetProtection(true,true);
            verify(project.IsLocked(selected)&&fields.Values.All(box=>box.ReadOnly)&&SessionNeedsSaving(),"Locked elements remain accessible in the tree with read-only properties");
            string lockedKey=selected.Key,originalX=selected.Get("posX");canvas.Update();canvas.ExerciseDrag(selected,50,40,false);
            verify(project.Controls.First(c=>c.Key==lockedKey).Get("posX")==originalX,"Canvas hit testing skips locked elements during dragging");
            while(project.Undo.Count>0)project.UndoChange();RefreshProject(lockedKey);SelectControl(project.Controls.First(c=>c.Key==lockedKey));
            bool rejected=false;fields["width"].Text="605";try{Apply();}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&!project.Dirty,"Element protection also blocks direct property application");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);rejected=false;
            try{AlignSelection("left");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&!project.Dirty,"A locked element blocks the entire alignment action");
            SetProtection(true,false);SetProtection(false,true);canvas.ShowHidden=true;
            verify(SelectedControls().All(c=>!canvas.ExerciseDrawn(c))&&!project.Dirty,"Editor hiding remains effective when showing conditionally hidden controls");canvas.ShowHidden=false;
            canvas.StepZoom(true);string file=Path.Combine(directory,"View.mp1skinproj");SaveSession(file);ResumeSession(file);
            verify(project.Editor.Hidden.Count==2&&project.Editor.Zoom>1&&!SessionNeedsSaving()&&!project.Dirty,"Opening projects restores hiding and zoom without XML edits");
            project.Editor.Hidden.Clear();RefreshProject(selected.Key);verify(SessionNeedsSaving(),"Showing elements after saving marks the editor project as changed");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
