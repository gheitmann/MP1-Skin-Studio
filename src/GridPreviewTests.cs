using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string GridChildren(){return StackChild(1,"")+StackChild(2,"")+StackChild(3,"")+StackChild(4,"")+StackChild(5,"");}
        static void RunGridTests(string root)
        {
            Test("Grid dialog edits preserve child XML and undo exactly",()=>{
                var p=StackFixture(root,"grid-dialog","GridLayout(2)","",GridChildren());string xml=p.Controls[0].Source.Text;
                Check(p.SetGridLayout(p.Controls[0],"GridLayout(0, 2, 15, 20, Vertical)",false)&&p.Undo.Count==1&&p.Controls[1].Get("posX")=="900","Grid edit is not atomic or changes children.");
                Check(StackRect(p,new StackPreview(p,null),3).X==195,"Grid edit did not refresh arrangement.");p.UndoChange();Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Grid undo is not exact.");});
            Test("Grid dialog model protects bindings, locks and other layouts",()=>{
                foreach(string rule in new[]{"#layout","StackLayout(7)","RingLayout(5)"}){var p=StackFixture(root,"grid-dialog-guard-"+Guid.NewGuid().ToString("N"),rule,"",GridChildren());string xml=p.Controls[0].Source.Text;
                Reject(()=>p.SetGridLayout(p.Controls[0],"GridLayout(2)",false));Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Protected layout was changed.");
                p.Editor.Locked.Add(p.Controls[0].Key);Reject(()=>p.SetGridLayout(p.Controls[0],"GridLayout(2)",true));p.Editor.Locked.Clear();Check(p.SetGridLayout(p.Controls[0],"GridLayout(2)",true)&&p.Undo.Count==1,"Explicit replacement failed.");}});
            Test("Grid dialog model rejects invalid, nested and duplicate layouts",()=>{
                var p=StackFixture(root,"grid-dialog-invalid","GridLayout(2)","",GridChildren());Reject(()=>p.SetGridLayout(p.Controls[0],"GridLayout(0,0)",true));Reject(()=>p.SetGridLayout(p.Controls[1],"GridLayout(2)",true));
                foreach(string extra in new[]{"<layout>GridLayout(3)</layout>","<layout><value>GridLayout(3)</value></layout>"}){var q=StackFixture(root,"grid-dialog-ambiguous-"+Guid.NewGuid().ToString("N"),"GridLayout(2)",extra,GridChildren());Reject(()=>q.SetGridLayout(q.Controls[0],"GridLayout(4)",true));Check(!q.Dirty,"Ambiguous layout changed.");}
                Check(!p.Dirty,"Rejected grid edits changed XML.");});
            Test("Grid columns wrap row-first without trailing spacing or XML changes",()=>{
                var p=StackFixture(root,"grid-columns","GridLayout(2,0,5,7)","",GridChildren());string xml=p.Controls[0].Source.Text;var a=new StackPreview(p,null);
                Check(StackRect(p,a,90)==new RectangleF(100,200,165,104)&&StackRect(p,a,3)==new RectangleF(100,237,80,30)&&StackRect(p,a,5).Y==274,"Incorrect grid size or wrapping.");Check(a.Warnings.Count==0&&p.Controls[0].Source.Text==xml&&!p.Dirty&&p.Undo.Count==0,"Preview modified XML or warned.");});
            Test("Titan grids fill column-first with numeric orientation",()=>{
                var p=StackFixture(root,"grid-titan","GridLayout(2,0,5,7,1)","",GridChildren());var a=new StackPreview(p,null);
                Check(StackRect(p,a,2).Y==237&&StackRect(p,a,4)==new RectangleF(185,200,80,30),"Incorrect column-first order.");});
            Test("Positive row count overrides grid column count as in MP1",()=>{
                var p=StackFixture(root,"grid-rows","GridLayout(99,2,5,7,Vertical)","",GridChildren());var a=new StackPreview(p,null);
                Check(StackRect(p,a,90).Width==250&&StackRect(p,a,3).X==185&&StackRect(p,a,5).X==270,"Rows did not take precedence.");});
            Test("Grid defaults and three-argument XML match the MP1 factory",()=>{
                foreach(string name in new[]{"GridLayout","GridLayout()","GridLayout(1,0,999)"}){var p=StackFixture(root,"grid-default-"+Guid.NewGuid().ToString("N"),name,"",GridChildren());var a=new StackPreview(p,null);Check(StackRect(p,a,2).Y==230&&StackRect(p,a,90).Height==150,"Incorrect factory default or ignored third token.");}});
            Test("Grid margins, alignment and stretching use maximum cell size",()=>{
                var p=StackFixture(root,"grid-align","GridLayout(2,0,5,7)","<Margin>10,20,30,40</Margin>",StackChild(1,"<width>160</width><height>60</height>")+StackChild(2,"<HorizontalAlignment>Right</HorizontalAlignment><VerticalAlignment>Bottom</VerticalAlignment>")+StackChild(3,"<HorizontalAlignment>Stretch</HorizontalAlignment><VerticalAlignment>Stretch</VerticalAlignment>"));var a=new StackPreview(p,null);
                Check(StackRect(p,a,2)==new RectangleF(355,250,80,30)&&StackRect(p,a,3)==new RectangleF(110,287,160,60)&&StackRect(p,a,90).Width==365,"Incorrect grid margin or alignment.");});
            Test("Collapsed grid children preserve MP1 row count and skip horizontal advance",()=>{
                var p=StackFixture(root,"grid-collapse","GridLayout(2,0,5,7)","",StackChild(1,"<Visibility>Collapsed</Visibility>")+StackChild(2,"")+StackChild(3,""));var a=new StackPreview(p,null);
                Check(StackRect(p,a,2).X==100&&StackRect(p,a,3).Y==237&&StackRect(p,a,90).Height==67,"Collapsed grid semantics differ from MP1.");});
            Test("Runtime-invisible and editor-hidden grid controls retain slots",()=>{
                var p=StackFixture(root,"grid-hidden","GridLayout(2)","",StackChild(1,"<visible>no</visible>")+StackChild(2,""));p.Editor.Hidden.Add(p.Controls[1].Key);var a=new StackPreview(p,new PreviewState(p));Check(StackRect(p,a,2).X==180,"Visibility unexpectedly reflows grid.");});
            Test("Nested stack and grid layouts measure before parent arrangement",()=>{
                string nested="<control><type>group</type><id>10</id><layout>StackLayout(7)</layout>"+StackChild(1,"")+StackChild(2,"")+"</control>";
                var p=StackFixture(root,"grid-nested","GridLayout(2,0,5,7)","",nested+StackChild(3,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).Y==237&&StackRect(p,a,3).X==185&&StackRect(p,a,90).Height==74,"Nested geometry is stale.");});
            Test("Invalid grids warn and keep source positions",()=>{
                foreach(string name in new[]{"GridLayout(0,0)","GridLayout(-1)","GridLayout(2,0,NaN,7)","GridLayout(2,0,5,7,bad)"}){var p=StackFixture(root,"grid-invalid-"+Guid.NewGuid().ToString("N"),name,"",StackChild(1,""));var a=new StackPreview(p,null);Check(a.Warnings.Count==1&&StackRect(p,a,1).X==900,"Invalid grid was arranged.");}});
            Test("Large grid dimensions do not enumerate empty cells",()=>{
                var p=StackFixture(root,"grid-large","GridLayout(2147483647,0,0,0)","",StackChild(1,"")+StackChild(2,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).X==180,"Large grid dimensions overflowed.");});
            Test("Grid geometry follows edits and exact undo",()=>{
                var p=StackFixture(root,"grid-undo","GridLayout(2)","",GridChildren());string xml=p.Controls[0].Source.Text;p.Edit(p.Controls[0],Set("layout","GridLayout(1)"),"Grid columns");Check(StackRect(p,new StackPreview(p,null),3).Y==260,"Edit did not refresh grid.");p.UndoChange();Check(p.Controls[0].Source.Text==xml&&StackRect(p,new StackPreview(p,null),3).Y==230&&!p.Dirty,"Undo changed source or geometry.");});
        }
    }
    public sealed partial class StudioForm
    {
        void RunGridUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.grid.xml");canvas.Update();var item=project.Controls.First(c=>c.Get("id")=="903");string xml=item.Source.Text;
            verify(canvas.BoundsOf(item)==new RectangleF(120,415,500,110),"Grid previews use arranged cell positions");
            canvas.ExerciseConnection(item);verify(selected.Get("id")=="903","Grid hit testing selects arranged controls");
            verify(item.Source.Text==xml&&!project.Dirty,"Grid rendering and selection preserve source XML");
            RenderTo(Path.Combine(directory,"Grid-Preview.png"));Show();Activate();Application.DoEvents();
            SelectControl(project.Controls.First(c=>c.Get("type")=="group"));string before=selected.Source.Text;bool seen=false;
            using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var d=Application.OpenForms.OfType<GridLayoutDialog>().FirstOrDefault();if(d==null)return;timer.Stop();seen=true;d.ChooseForTest(2,0,15,20,false);d.SubmitForTest();if(d.DialogResult==DialogResult.None){d.DialogResult=DialogResult.Cancel;d.Close();}};
                var menu=(StudioMenu)ProtectionMenu();int index=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Edit grid layout",StringComparison.Ordinal));timer.Start();menu.Show(tree,new Point(12,12));menu.ChooseForTest(index);timer.Stop();
            }
            verify(seen&&selected.Get("layout")=="GridLayout(2, 0, 15, 20, Horizontal)"&&project.Undo.Count==1,"Grid menus open dialogs and apply atomic edits");
            undo.PerformClick();verify(selected.Source.Text==before&&!project.Dirty,"Grid dialog undo restores complete source XML");redo.PerformClick();verify(selected.Get("layout")=="GridLayout(2, 0, 15, 20, Horizontal)","Grid dialog redo restores the new rule");undo.PerformClick();
            using(var d=new GridLayoutDialog(selected)){d.Show(this);Application.DoEvents();d.ChooseForTest(0,0,0,0,false);verify(!d.AcceptForTest,"Grid dialog prevents zero rows and zero columns");d.ChooseForTest(0,2,Int32.MaxValue,Int32.MaxValue,true);verify(d.AcceptForTest&&d.Result=="GridLayout(0, 2, 2147483647, 2147483647, Vertical)","Grid dialog supports row precedence and full integer spacing");d.ChooseForTest(2,0,15,20,false);using(var image=new Bitmap(d.Width,d.Height)){d.DrawToBitmap(image,new Rectangle(Point.Empty,d.Size));image.Save(Path.Combine(directory,"Grid-Layout-Dialog.png"));}d.Close();}
            project.Edit(selected,new System.Collections.Generic.Dictionary<string,string>{{"layout","#layout"}},"Binding probe");RefreshProject(selected.Key);
            using(var d=new GridLayoutDialog(selected)){d.Show(this);Application.DoEvents();verify(!d.AcceptForTest,"Grid dialog protects existing bindings");d.ReplaceForTest();verify(d.AcceptForTest,"Grid dialog permits explicit binding replacement");d.Close();}undo.PerformClick();
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
