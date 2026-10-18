using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string GridChildren(){return StackChild(1,"")+StackChild(2,"")+StackChild(3,"")+StackChild(4,"")+StackChild(5,"");}
        static void RunGridTests(string root)
        {
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
            RenderTo(Path.Combine(directory,"Grid-Preview.png"));LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
