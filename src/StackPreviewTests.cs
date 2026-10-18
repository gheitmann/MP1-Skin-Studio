using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject StackFixture(string root,string name,string layout,string extra,string children)
        {
            string folder=Path.Combine(root,"stack-"+name);Directory.CreateDirectory(folder);
            Write(folder,"main.xml","<window><controls><control><type>group</type><id>90</id><posX>100</posX><posY>200</posY><layout>"+layout+"</layout>"+extra+children+"</control></controls></window>");
            var p=new SkinProject(folder);p.OpenPage("main.xml");return p;
        }
        static string StackChild(int id,string extra){return "<control><type>button</type><id>"+id+"</id><posX>900</posX><posY>800</posY><width>80</width><height>30</height>"+extra+"</control>";}
        static RectangleF StackRect(SkinProject p,StackPreview layout,int id){return layout.Bounds(p.Controls.Single(c=>c.Get("id")==id.ToString()));}
        static void RunStackTests(string root)
        {
            Test("Layout-dialog changes are atomic and preserve child positions",()=>{
                var p=StackFixture(root,"dialog-model","StackLayout(7)","",StackChild(1,"")+StackChild(2,""));string xml=p.Controls[0].Source.Text;
                Check(p.SetStackLayout(p.Controls[0],"StackLayout(15, Horizontal, true)",false)&&p.Controls[1].Get("posX")=="900"&&p.Controls[1].Get("posY")=="800"&&p.Undo.Count==1,"Child positions were changed.");p.UndoChange();Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Undo is not exact.");});
            Test("The layout-dialog model protects locks, bindings and unknown rules",()=>{
                var p=StackFixture(root,"dialog-guard","#layout","",StackChild(1,""));string xml=p.Controls[0].Source.Text;Reject(()=>p.SetStackLayout(p.Controls[0],"StackLayout(5)",false));Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Binding was overwritten.");p.Editor.Locked.Add(p.Controls[0].Key);Reject(()=>p.SetStackLayout(p.Controls[0],"StackLayout(5)",true));p.Editor.Locked.Clear();p.SetStackLayout(p.Controls[0],"StackLayout(5)",true);Check(p.Undo.Count==1,"Approval is not atomic.");});
            Test("Vertical stacks replace source positions without modifying XML",()=>{
                var p=StackFixture(root,"vertical","StackLayout(7, Vertical, true)","",StackChild(1,"")+StackChild(2,""));string xml=p.Controls[0].Source.Text;var a=new StackPreview(p,new PreviewState(p));
                Check(StackRect(p,a,1)==new RectangleF(100,200,80,30)&&StackRect(p,a,2).Y==237&&StackRect(p,a,90).Height==74,"Incorrect vertical arrangement or trailing spacing.");Check(p.Controls[0].Source.Text==xml&&!p.Dirty&&p.Undo.Count==0,"Preview writes XML.");});
            Test("Horizontal stacks use nonnegative spacing",()=>{
                var p=StackFixture(root,"horizontal","StackLayout(-7 Horizontal)","",StackChild(1,"")+StackChild(2,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).X==180&&StackRect(p,a,2).Y==200&&StackRect(p,a,90).Width==160,"Incorrect horizontal stack.");});
            Test("Stacks without parameters use vertical layout",()=>{
                var p=StackFixture(root,"default","StackLayout","",StackChild(1,"")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,null),2).Y==230,"Incorrect default orientation.");});
            Test("Invisible controls collapse only with CollapseHiddenButtons",()=>{
                foreach(string collapse in new[]{"true","false"}){var p=StackFixture(root,"hidden-"+collapse,"StackLayout(7, Vertical, "+collapse+")","",StackChild(1,"<visible>no</visible>")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,new PreviewState(p)),2).Y==(collapse=="true"?200:237),"Incorrect hidden slot.");}});
            Test("Unknown conditions and editor-hidden controls retain their layout slots",()=>{
                var p=StackFixture(root,"unknown","StackLayout(7, Vertical, true)","",StackChild(1,"<visible>unknown.test()</visible>")+StackChild(2,""));p.Editor.Hidden.Add(p.Controls[1].Key);p.Editor.ShowUnresolved=false;Check(StackRect(p,new StackPreview(p,new PreviewState(p)),2).Y==237,"Editor state changes runtime layout.");});
            Test("Collapsed controls contribute neither measured size nor layout slots",()=>{
                var p=StackFixture(root,"collapsed","StackLayout(7, Vertical)","",StackChild(1,"<Visibility>Collapsed</Visibility>")+StackChild(2,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).Y==200&&StackRect(p,a,90).Height==37,"Collapsed controls are still measured.");});
            Test("Group margins and child alignment are applied",()=>{
                var p=StackFixture(root,"margin","StackLayout(5)","<Margin>10,20,30,40</Margin>",StackChild(1,"<width>160</width>")+StackChild(2,"<HorizontalAlignment>Right</HorizontalAlignment>"));var a=new StackPreview(p,null);Check(StackRect(p,a,1).X==110&&StackRect(p,a,2).X==190&&StackRect(p,a,2).Y==255&&StackRect(p,a,90).Width==200,"Incorrect margin or right alignment.");});
            Test("Horizontal group alignment matches MP1 including trailing spacing",()=>{
                var p=StackFixture(root,"right","StackLayout(10, Horizontal)","<width>300</width><height>100</height><align>right</align>",StackChild(1,"")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,null),1).X==220,"Incorrect group alignment.");});
            Test("Right-aligned stack labels retain their drawing-anchor convention",()=>{
                string child=StackChild(1,"<align>right</align>").Replace("<type>button</type>","<type>label</type>");var p=StackFixture(root,"label","StackLayout()","",child);Check(StackRect(p,new StackPreview(p,null),1).X==100,"Label width was subtracted twice.");});
            Test("Unknown and malformed layouts warn without changing positions",()=>{
                foreach(string name in new[]{"RingLayout(5)","StackLayout(bad)"}){var p=StackFixture(root,"invalid-"+name.Replace('(','_').Replace(')','_'),name,"",StackChild(1,""));var a=new StackPreview(p,null);Check(a.Warnings.Count==1&&StackRect(p,a,1).X==900,"Unknown layout was invented.");}});
            Test("Layout changes, undo and redo refresh derived positions",()=>{
                var p=StackFixture(root,"undo","StackLayout(7)","",StackChild(1,"")+StackChild(2,""));p.Edit(p.Controls[0],Set("layout","StackLayout(20, Horizontal)"),"Layout");Check(StackRect(p,new StackPreview(p,null),2).X==200,"Change is not visible.");p.UndoChange();Check(StackRect(p,new StackPreview(p,null),2).Y==237&&!p.Dirty,"Incorrect undo.");p.RedoChange();Check(StackRect(p,new StackPreview(p,null),2).X==200,"Incorrect redo.");});
            Test("Repeated includes retain separate layout rectangles",()=>{
                var p=StackFixture(root,"include","StackLayout(7)","",StackChild(1,""));string folder=p.Root;Write(folder,"part.xml","<window><controls>"+StackChild(7,"")+"</controls></window>");Write(folder,"main.xml","<window><controls><control><type>group</type><posX>100</posX><layout>StackLayout()</layout><import>part.xml</import></control><control><type>group</type><posX>500</posX><layout>StackLayout()</layout><import>part.xml</import></control></controls></window>");p=new SkinProject(folder);p.OpenPage("main.xml");var uses=p.Controls.Where(c=>c.Get("id")=="7").ToArray();var a=new StackPreview(p,null);Check(uses.Length==2&&uses[0].Key==uses[1].Key&&a.Bounds(uses[0]).X==100&&a.Bounds(uses[1]).X==500,"Source key mixes occurrences.");});
        }
    }
    public sealed partial class StudioForm
    {
        void RunStackUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.stack.xml");canvas.Update();var series=project.Controls.First(c=>c.Get("id")=="803");
            verify(canvas.BoundsOf(series).Y==373&&series.Node.Child("posY")==null&&!project.Dirty,"Stack previews arrange visible items with spacing without changing source positions");
            canvas.ExerciseConnection(series);verify(selected.Get("id")=="803","Mouse selection hits arranged rectangles rather than original positions");
            SelectControl(project.Controls.First(c=>c.Get("id")=="800"));string before=selected.Source.Text;
            bool seen=false;using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var d=Application.OpenForms.OfType<StackLayoutDialog>().FirstOrDefault();if(d==null)return;timer.Stop();seen=true;d.ChooseForTest(false,35,false);d.SubmitForTest();if(d.DialogResult==DialogResult.None){d.DialogResult=DialogResult.Cancel;d.Close();}};
                var menu=(StudioMenu)ProtectionMenu();int item=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Edit group layout",StringComparison.Ordinal));timer.Start();menu.Show(tree,new Point(12,12));menu.ChooseForTest(item);timer.Stop();
            }
            verify(seen&&selected.Get("layout")=="StackLayout(35, Vertical, false)"&&project.Undo.Count==1,"Element menus open layout dialogs and atomically apply direction, spacing and collapsing");
            series=project.Controls.First(c=>c.Get("id")=="803");verify(canvas.BoundsOf(series).Y==515&&series.Node.Child("posY")==null,"Applying layouts reserves hidden items and refreshes the preview");
            undo.PerformClick();verify(selected.Source.Text==before&&!project.Dirty&&canvas.BoundsOf(project.Controls.First(c=>c.Get("id")=="803")).Y==373,"Undo restores source text and stack previews exactly");
            redo.PerformClick();verify(canvas.BoundsOf(project.Controls.First(c=>c.Get("id")=="803")).Y==515,"Redo refreshes stack geometry");undo.PerformClick();
            using(var dialog=new StackLayoutDialog(selected)){dialog.Show(this);Application.DoEvents();verify(dialog.AcceptForTest&&dialog.Result=="StackLayout(18, Vertical, true)","Layout dialogs load existing settings correctly");using(var image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(Point.Empty,dialog.Size));image.Save(Path.Combine(directory,"Layout-Dialog.png"),System.Drawing.Imaging.ImageFormat.Png);}dialog.Close();}
            project.Edit(selected,new System.Collections.Generic.Dictionary<string,string>{{"layout","RingLayout(5)"}},"Probe");RefreshProject(selected.Key);
            using(var dialog=new StackLayoutDialog(selected)){dialog.Show(this);Application.DoEvents();verify(!dialog.AcceptForTest,"Other layout types require explicit approval in the dialog");dialog.ReplaceForTest();verify(dialog.AcceptForTest,"Explicit approval enables applying layouts");dialog.Close();}undo.PerformClick();
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
