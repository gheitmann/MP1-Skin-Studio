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
            Test("Layoutdialog-Änderung ist atomar und erhält Kindpositionen",()=>{
                var p=StackFixture(root,"dialog-model","StackLayout(7)","",StackChild(1,"")+StackChild(2,""));string xml=p.Controls[0].Source.Text;
                Check(p.SetStackLayout(p.Controls[0],"StackLayout(15, Horizontal, true)",false)&&p.Controls[1].Get("posX")=="900"&&p.Controls[1].Get("posY")=="800"&&p.Undo.Count==1,"Kindpositionen verändert.");p.UndoChange();Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Undo nicht exakt.");});
            Test("Layoutdialog-Modell schützt Sperren, Bindungen und unbekannte Regeln",()=>{
                var p=StackFixture(root,"dialog-guard","#layout","",StackChild(1,""));string xml=p.Controls[0].Source.Text;Reject(()=>p.SetStackLayout(p.Controls[0],"StackLayout(5)",false));Check(p.Controls[0].Source.Text==xml&&!p.Dirty,"Bindung überschrieben.");p.Editor.Locked.Add(p.Controls[0].Key);Reject(()=>p.SetStackLayout(p.Controls[0],"StackLayout(5)",true));p.Editor.Locked.Clear();p.SetStackLayout(p.Controls[0],"StackLayout(5)",true);Check(p.Undo.Count==1,"Freigabe nicht atomar.");});
            Test("Vertikaler Stack ersetzt Quellpositionen ohne XML-Änderung",()=>{
                var p=StackFixture(root,"vertical","StackLayout(7, Vertical, true)","",StackChild(1,"")+StackChild(2,""));string xml=p.Controls[0].Source.Text;var a=new StackPreview(p,new PreviewState(p));
                Check(StackRect(p,a,1)==new RectangleF(100,200,80,30)&&StackRect(p,a,2).Y==237&&StackRect(p,a,90).Height==74,"Vertikale Anordnung oder abschließender Abstand falsch.");Check(p.Controls[0].Source.Text==xml&&!p.Dirty&&p.Undo.Count==0,"Vorschau schreibt XML.");});
            Test("Horizontaler Stack und nichtnegative Abstände",()=>{
                var p=StackFixture(root,"horizontal","StackLayout(-7 Horizontal)","",StackChild(1,"")+StackChild(2,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).X==180&&StackRect(p,a,2).Y==200&&StackRect(p,a,90).Width==160,"Horizontaler Stack falsch.");});
            Test("Stack ohne Parameter verwendet vertikale Anordnung",()=>{
                var p=StackFixture(root,"default","StackLayout","",StackChild(1,"")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,null),2).Y==230,"Standardorientierung falsch.");});
            Test("Unsichtbare Controls rücken nur mit CollapseHiddenButtons nach",()=>{
                foreach(string collapse in new[]{"true","false"}){var p=StackFixture(root,"hidden-"+collapse,"StackLayout(7, Vertical, "+collapse+")","",StackChild(1,"<visible>no</visible>")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,new PreviewState(p)),2).Y==(collapse=="true"?200:237),"Hidden-Slot falsch.");}});
            Test("Unbekannte Bedingungen und Editor-Ausblendung behalten ihren Platz",()=>{
                var p=StackFixture(root,"unknown","StackLayout(7, Vertical, true)","",StackChild(1,"<visible>unknown.test()</visible>")+StackChild(2,""));p.Editor.Hidden.Add(p.Controls[1].Key);p.Editor.ShowUnresolved=false;Check(StackRect(p,new StackPreview(p,new PreviewState(p)),2).Y==237,"Editorzustand verändert Runtime-Layout.");});
            Test("Collapsed entfernt Messgröße und Layoutslot",()=>{
                var p=StackFixture(root,"collapsed","StackLayout(7, Vertical)","",StackChild(1,"<Visibility>Collapsed</Visibility>")+StackChild(2,""));var a=new StackPreview(p,null);Check(StackRect(p,a,2).Y==200&&StackRect(p,a,90).Height==37,"Collapsed weiterhin gemessen.");});
            Test("Gruppen-Margin und Kind-Ausrichtung werden angewandt",()=>{
                var p=StackFixture(root,"margin","StackLayout(5)","<Margin>10,20,30,40</Margin>",StackChild(1,"<width>160</width>")+StackChild(2,"<HorizontalAlignment>Right</HorizontalAlignment>"));var a=new StackPreview(p,null);Check(StackRect(p,a,1).X==110&&StackRect(p,a,2).X==190&&StackRect(p,a,2).Y==255&&StackRect(p,a,90).Width==200,"Margin oder Right falsch.");});
            Test("Horizontale Gruppenausrichtung folgt MP1 inklusive Endabstand",()=>{
                var p=StackFixture(root,"right","StackLayout(10, Horizontal)","<width>300</width><height>100</height><align>right</align>",StackChild(1,"")+StackChild(2,""));Check(StackRect(p,new StackPreview(p,null),1).X==220,"Gruppenausrichtung falsch.");});
            Test("Rechtsbündiges Stack-Label behält seine Zeichenanker-Konvention",()=>{
                string child=StackChild(1,"<align>right</align>").Replace("<type>button</type>","<type>label</type>");var p=StackFixture(root,"label","StackLayout()","",child);Check(StackRect(p,new StackPreview(p,null),1).X==100,"Labelbreite doppelt abgezogen.");});
            Test("Unbekannte und fehlerhafte Layouts warnen ohne Positionsänderung",()=>{
                foreach(string name in new[]{"RingLayout(5)","StackLayout(bad)"}){var p=StackFixture(root,"invalid-"+name.Replace('(','_').Replace(')','_'),name,"",StackChild(1,""));var a=new StackPreview(p,null);Check(a.Warnings.Count==1&&StackRect(p,a,1).X==900,"Unbekanntes Layout erfunden.");}});
            Test("Layoutänderung, Undo und Redo erneuern abgeleitete Positionen",()=>{
                var p=StackFixture(root,"undo","StackLayout(7)","",StackChild(1,"")+StackChild(2,""));p.Edit(p.Controls[0],Set("layout","StackLayout(20, Horizontal)"),"Layout");Check(StackRect(p,new StackPreview(p,null),2).X==200,"Änderung nicht sichtbar.");p.UndoChange();Check(StackRect(p,new StackPreview(p,null),2).Y==237&&!p.Dirty,"Undo falsch.");p.RedoChange();Check(StackRect(p,new StackPreview(p,null),2).X==200,"Redo falsch.");});
            Test("Wiederverwendete Includes behalten getrennte Layoutrechtecke",()=>{
                var p=StackFixture(root,"include","StackLayout(7)","",StackChild(1,""));string folder=p.Root;Write(folder,"part.xml","<window><controls>"+StackChild(7,"")+"</controls></window>");Write(folder,"main.xml","<window><controls><control><type>group</type><posX>100</posX><layout>StackLayout()</layout><import>part.xml</import></control><control><type>group</type><posX>500</posX><layout>StackLayout()</layout><import>part.xml</import></control></controls></window>");p=new SkinProject(folder);p.OpenPage("main.xml");var uses=p.Controls.Where(c=>c.Get("id")=="7").ToArray();var a=new StackPreview(p,null);Check(uses.Length==2&&uses[0].Key==uses[1].Key&&a.Bounds(uses[0]).X==100&&a.Bounds(uses[1]).X==500,"Quellschlüssel vermischt Vorkommen.");});
        }
    }
    public sealed partial class StudioForm
    {
        void RunStackUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.stack.xml");canvas.Update();var series=project.Controls.First(c=>c.Get("id")=="803");
            verify(canvas.BoundsOf(series).Y==373&&series.Node.Child("posY")==null&&!project.Dirty,"Stack-Vorschau ordnet sichtbare Einträge mit Abstand an, ohne Quellpositionen zu ändern");
            canvas.ExerciseConnection(series);verify(selected.Get("id")=="803","Mausauswahl trifft das angeordnete statt des ursprünglichen Rechtecks");
            SelectControl(project.Controls.First(c=>c.Get("id")=="800"));string before=selected.Source.Text;
            bool seen=false;using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var d=Application.OpenForms.OfType<StackLayoutDialog>().FirstOrDefault();if(d==null)return;timer.Stop();seen=true;d.ChooseForTest(false,35,false);d.SubmitForTest();if(d.DialogResult==DialogResult.None){d.DialogResult=DialogResult.Cancel;d.Close();}};
                var menu=(StudioMenu)ProtectionMenu();int item=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Gruppenlayout bearbeiten",StringComparison.Ordinal));timer.Start();menu.Show(tree,new Point(12,12));menu.ChooseForTest(item);timer.Stop();
            }
            verify(seen&&selected.Get("layout")=="StackLayout(35, Vertical, false)"&&project.Undo.Count==1,"Element-Menü öffnet Layoutdialog und übernimmt Richtung, Abstand und Nachrücken atomar");
            series=project.Controls.First(c=>c.Get("id")=="803");verify(canvas.BoundsOf(series).Y==515&&series.Node.Child("posY")==null,"Layoutübernahme reserviert den ausgeblendeten Eintrag und erneuert die Vorschau");
            undo.PerformClick();verify(selected.Source.Text==before&&!project.Dirty&&canvas.BoundsOf(project.Controls.First(c=>c.Get("id")=="803")).Y==373,"Undo stellt Quelltext und Stack-Vorschau exakt wieder her");
            redo.PerformClick();verify(canvas.BoundsOf(project.Controls.First(c=>c.Get("id")=="803")).Y==515,"Redo erneuert Stack-Geometrie");undo.PerformClick();
            using(var dialog=new StackLayoutDialog(selected)){dialog.Show(this);Application.DoEvents();verify(dialog.AcceptForTest&&dialog.Result=="StackLayout(18, Vertical, true)","Layoutdialog übernimmt vorhandene Einstellungen korrekt");using(var image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(Point.Empty,dialog.Size));image.Save(Path.Combine(directory,"Layoutdialog.png"),System.Drawing.Imaging.ImageFormat.Png);}dialog.Close();}
            project.Edit(selected,new System.Collections.Generic.Dictionary<string,string>{{"layout","RingLayout(5)"}},"Probe");RefreshProject(selected.Key);
            using(var dialog=new StackLayoutDialog(selected)){dialog.Show(this);Application.DoEvents();verify(!dialog.AcceptForTest,"Andere Layouttypen erfordern ausdrückliche Freigabe im Dialog");dialog.ReplaceForTest();verify(dialog.AcceptForTest,"Ausdrückliche Freigabe aktiviert Layoutübernahme");dialog.Close();}undo.PerformClick();
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
