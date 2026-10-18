using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunTextEditTests(string root)
        {
            Test("Text edits preserve unknown XML and support byte-exact undo",()=>
            {var p=Demo();var film=Named(p,"Movies");string before=film.Source.Text;Check(p.SetControlText(film,"Cinema & Café <heute>",false)&&Named(p,"Movies").Get("label")=="Cinema & Café <heute>"&&Named(p,"Movies").Source.Text.Contains("Cinema &amp; Café &lt;heute&gt;"),"Text was not saved correctly.");Check(Named(p,"Movies").Node.Child("editorUnknown")!=null&&p.Undo.Count==1,"XML extension or atomic step was lost.");p.UndoChange();Check(Named(p,"Movies").Source.Text==before,"Undo is not exact.");p.RedoChange();Check(Named(p,"Movies").Get("label")=="Cinema & Café <heute>","Redo loses text.");});
            Test("Data bindings are replaced only with explicit approval",()=>
            {var p=Demo();var c=Named(p,"Time");var info=p.InspectTextEdit(c);Check(info.Bound&&info.NeedsApproval&&!p.SetControlText(c,info.Raw,false),"Binding or unchanged value was detected incorrectly.");Reject(()=>p.SetControlText(c,"Abend",false));Check(!p.Dirty&&p.Undo.Count==0,"Rejected change writes XML.");p.SetControlText(c,"Abend",true);Check(Named(p,"Time").Get("label")=="Abend","Approved value was not set.");});
            Test("Numeric text remains protected as a possible language-resource ID",()=>
            {var p=ResetFixture(root,"text-numeric");var c=p.Controls[0];p.Edit(c,Set("label","123"),"Sprach-ID");c=p.Controls[0];Check(p.InspectTextEdit(c).LanguageId,"Numeric ID was not detected.");Reject(()=>p.SetControlText(c,"New text",false));p.SetControlText(c,"New text",true);Check(p.Controls[0].Get("label")=="New text","Approval had no effect.");});
            Test("Inherited text is overridden locally without changing styles or references",()=>
            {string folder=Path.Combine(root,"text-inherited");Directory.CreateDirectory(folder);Write(folder,"references.xml","<controls><style Name=\"Title\"><label>#title</label></style></controls>");Write(folder,"main.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id></control></controls></window>");var p=new SkinProject(folder);p.OpenPage("main.xml");string refs=p.Source(p.Resolve("references.xml")).Text;Check(p.InspectTextEdit(p.Controls[0]).Bound,"Inherited binding is not protected.");Reject(()=>p.SetControlText(p.Controls[0],"Title",false));p.SetControlText(p.Controls[0],"Title",true);Check(p.Controls[0].Node.Child("label")!=null&&p.Source(p.Resolve("references.xml")).Text==refs,"Style was changed.");p.UndoChange();Check(p.Controls[0].Get("label")=="#title"&&p.Controls[0].Node.Child("label")==null,"Incorrect inherited state after undo.");});
            Test("Text editing rejects locks, groups, images and stale controls",()=>
            {var p=Demo();var film=Named(p,"Movies");p.Editor.Locked.Add(film.Key);Reject(()=>p.SetControlText(film,"Neu",false));p.Editor.Locked.Clear();p.SetControlText(film,"Neu",false);Reject(()=>p.SetControlText(film,"Alt",false));p.OpenPage("demo.groups.xml");Check(!p.InspectTextEdit(Named(p,"Panel")).Allowed&&!p.InspectTextEdit(Named(p,"Media section")).Allowed,"Unsupported type was allowed.");});
            Test("Invalid or oversized text preserves source and history",()=>
            {var p=Demo();var c=Named(p,"Movies");string before=c.Source.Text;Reject(()=>p.SetControlText(c,"x\u0001",false));Reject(()=>p.SetControlText(c,new string('x',32769),false));Reject(()=>p.SetControlText(c,null,false));Check(c.Source.Text==before&&p.Undo.Count==0,"Invalid text had side effects.");Check(p.SetControlText(c,"",false)&&Named(p,"Movies").Get("label")=="","Empty text is unsupported.");});
            Test("Duplicate and nested label entries remain protected",()=>
            {var p=AuditFixture(root,"text-ambiguous","<control><type>label</type><label>A</label><label>B</label></control><control><type>label</type><label><part>C</part></label></control>");Check(p.Controls.All(c=>!p.InspectTextEdit(c).Allowed),"Ambiguous text is editable.");foreach(var c in p.Controls)Reject(()=>p.SetControlText(c,"Neu",true));Check(!p.Dirty,"Protected XML was changed.");});
            Test("Text editing preserves surrounding spaces and line breaks",()=>
            {var p=Demo();p.SetControlText(Named(p,"Movies"),"  First line\nSecond line  ",false);Check(Named(p,"Movies").Get("label")=="  First line\nSecond line  ","Text was trimmed or wrapped.");Check(!p.SetControlText(Named(p,"Movies"),"  First line\nSecond line  ",false)&&p.Undo.Count==1,"Unchanged text creates an extra step.");});
        }
    }
    public sealed partial class StudioForm
    {
        bool DriveTextDialog(Action open,Action<TextEditDialog> action)
        {
            bool seen=false;
            using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<TextEditDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();seen=true;action(dialog);if(!dialog.IsDisposed&&dialog.DialogResult==DialogResult.None){dialog.DialogResult=DialogResult.Cancel;dialog.Close();}};
                timer.Start();open();timer.Stop();
            }
            return seen;
        }
        void RunTextEditUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");var time=project.Controls.First(c=>c.Name=="Time");
            using(var dialog=new TextEditDialog(project.InspectTextEdit(time),state))
            {
                dialog.Show(this);Application.DoEvents();verify(dialog.CanAccept&&dialog.PreviewForTest=="18:42","Text dialogs show bound original values with current sample data and allow unchanged closing");
                dialog.TextForTest("New clock");verify(!dialog.CanAccept,"Changed bound text requires its own explicit approval");dialog.ApproveForTest();verify(dialog.CanAccept&&dialog.PreviewForTest=="New clock","Approval enables applying changes and refreshes the preview");dialog.Close();
            }
            var film=project.Controls.First(c=>c.Name=="Movies");string before=film.Source.Text;bool seen=DriveTextDialog(()=>canvas.ExerciseTextDoubleClick(film),dialog=>{dialog.TextForTest("Cinema & Café");dialog.AcceptForTest();});
            verify(seen&&selected.Name=="Movies"&&selected.Get("label")=="Cinema & Café"&&project.Undo.Count==1,"Canvas double-click opens text editing and applies text in one undo step");undo.PerformClick();verify(selected.Source.Text==before,"Undo after double-click restores XML and unknown data exactly");redo.PerformClick();verify(selected.Get("label")=="Cinema & Café","Redo after text editing restores the new value");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Name=="Series"));canvas.NativeView();canvas.Snap=false;string oldX=selected.Get("posX");canvas.ExerciseDrag(selected,1,0,false);verify(selected.Get("posX")==oldX&&!project.Dirty&&project.Undo.Count==0,"Small pointer movements while clicking do not change positions even without snapping");canvas.Snap=true;canvas.FitView();
            SelectControl(project.Controls.First(c=>c.Name=="Movies"));var menu=(StudioMenu)ProtectionMenu();int item=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Edit text",StringComparison.Ordinal));
            seen=DriveTextDialog(()=>{menu.Show(tree,new System.Drawing.Point(12,12));menu.ChooseForTest(item);},dialog=>{dialog.TextForTest("Discarded");dialog.DialogResult=DialogResult.Cancel;dialog.Close();});verify(seen&&!project.Dirty&&project.Undo.Count==0&&!menu.IsDisposed,"Real element menus open text editing and cancellation preserves XML and history");
            fields["width"].Text="512";canvas.ExerciseTextDoubleClick(project.Controls.First(c=>c.Name=="Series"));verify(selected.Name=="Movies"&&fields["width"].Text=="512"&&!project.Dirty,"Double-click and preceding mouse selection preserve pending property edits");fields["width"].Text="470";
            tree.SelectedNode=tree.Nodes[0];Activate();tree.Focus();Application.DoEvents();seen=DriveTextDialog(()=>{var message=new Message();ProcessCmdKey(ref message,Keys.F2);},dialog=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();});verify(!seen&&!project.Dirty,"F2 on file nodes does not accidentally edit the previous control");
            Activate();canvas.Focus();Application.DoEvents();seen=DriveTextDialog(()=>{var message=new Message();ProcessCmdKey(ref message,Keys.F2);},dialog=>{dialog.AcceptForTest();});verify(seen&&!project.Dirty&&project.Undo.Count==0,"F2 on the canvas opens text editing and unchanged application creates no undo step"+(seen&&!project.Dirty&&project.Undo.Count==0?"":" [dialog="+seen+", dirty="+project.Dirty+", undo="+project.Undo.Count+", selection="+SelectedControls().Length+", canvasFocus="+canvas.Focused+", treeFocus="+tree.Focused+", pending="+PendingProperties()+"]"));
            time=project.Controls.First(c=>c.Name=="Time");seen=DriveTextDialog(()=>EditElementText(time),dialog=>{dialog.TextForTest("Good evening");dialog.ApproveForTest();dialog.AcceptForTest();});verify(seen&&selected.Get("label")=="Good evening"&&project.Undo.Count==1,"Bound text can be explicitly replaced locally through the dialog");undo.PerformClick();verify(selected.Get("label")=="#time","Undo restores the original data binding");
            project.Editor.Locked.Add(selected.Key);EditElementText(selected);verify(!project.Dirty&&status.Text.Contains("locked"),"Locked text does not open an editable dialog");project.Editor.Locked.Clear();
            LoadProject(DemoRoot(),"demo.groups.xml");var title=project.Controls.First(c=>c.Name=="Title");project.Edit(project.Controls.First(c=>c.Name=="Media section"),new System.Collections.Generic.Dictionary<string,string>{{"layout","StackLayout"}},"Layout");RefreshProject(title.Key);title=project.Controls.First(c=>c.Name=="Title");seen=DriveTextDialog(()=>EditElementText(title),dialog=>{dialog.TextForTest("New collection");dialog.AcceptForTest();});verify(seen&&selected.AutomaticLayout&&selected.Get("label")=="New collection","Text remains explicitly editable within automatic group layouts");
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state)){dialog.Show(this);Application.DoEvents();dialog.TextForTest("Line 1\nLine 2");dialog.CommitShortcutForTest();verify(dialog.Result.Contains("Line 1")&&dialog.Result.Contains("Line 2"),"Ctrl+Enter applies multiline text without trimming");}
            project.SetControlText(selected,"  Line 1\nLine 2  ",false);RefreshProject(selected.Key);var info=project.InspectTextEdit(selected);using(var dialog=new TextEditDialog(info,state)){dialog.Show(this);Application.DoEvents();dialog.AcceptForTest();verify(dialog.Result==info.Raw&&!project.SetControlText(selected,dialog.Result,false),"Unchanged multiline text fields preserve spaces, line breaks and original values");}
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
