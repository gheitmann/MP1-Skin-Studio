using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using MP1SkinStudio;

class PerformanceProbe
{
    static T Field<T>(object owner,string name)
    {return (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
    static object Call(object owner,string name,params object[] args)
    {return owner.GetType().GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}
    static void Time(string name,Action action)
    {
        var times=new double[7];
        for(int i=0;i<times.Length;i++){var watch=Stopwatch.StartNew();action();times[i]=watch.Elapsed.TotalMilliseconds;}
        Array.Sort(times);
        Console.WriteLine(name+" median_ms="+times[3].ToString("F2")+" max_ms="+times[6].ToString("F2"));
    }
    static void CheckHits(SkinCanvas canvas,SkinProject project)
    {
        foreach(var point in new[]{new Point(100,100),new Point(500,350),new Point(700,500)})
        {
            var logical=(PointF)Call(canvas,"Logical",point);
            var expected=project.Controls.Where(c=>(bool)Call(canvas,"Drawn",c)&&canvas.BoundsOf(c).Contains(logical)).Reverse().GroupBy(c=>c.Key).Select(g=>g.First().Key).ToArray();
            var actual=((SkinControl[])Call(canvas,"OverlappingAt",point)).Select(c=>c.Key).ToArray();
            if(!expected.SequenceEqual(actual)||Field<StackPreview>(canvas,"paintingLayout")!=null)
                throw new Exception("Hit order/geometry differs or measurement escaped input scope.");
        }
    }
    [STAThread]static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        using(var form=new StudioForm(args[0]))
        {
            form.CreateControl();
            foreach(string page in new[]{"myHome.xml","MediaTipOverlay.xml"})
            {
                form.LoadProject(args[0],page);
                var project=Field<SkinProject>(form,"project");var state=Field<PreviewState>(form,"state");var canvas=Field<SkinCanvas>(form,"canvas");
                Console.WriteLine(page+" controls="+project.Controls.Count);
                Time("layout",()=>{new StackPreview(project,state);});
                Time("overlap",()=>Call(canvas,"OverlappingAt",new Point(500,350)));
                Time("visibility x2",()=>{foreach(var control in project.Controls)state.Visible(control);foreach(var control in project.Controls)state.Visible(control);});
                Time("diagnostics",()=>Call(form,"UpdateDiagnostics"));
                Time("tree",()=>Call(form,"RebuildElementTree"));
                int index=0;Time("select",()=>Call(form,"SelectControl",project.Controls[(index++)%project.Controls.Count]));
                foreach(string name in new[]{"UpdateResetTools","UpdateSelectionTools","UpdatePreviewTree","SyncElementTreeSelection"})Time(name,()=>Call(form,name));
                Time("caption",()=>Call(form,"UpdateProjectCaption"));
                using(var bitmap=new Bitmap(1000,700))
                {
                    canvas.Size=bitmap.Size;Time("paint",()=>canvas.DrawToBitmap(bitmap,canvas.ClientRectangle));
                    CheckHits(canvas,project);
                    state.FocusId=999;CheckHits(canvas,project);
                    state.Values["#time"]="12:34";CheckHits(canvas,project);
                    var edit=project.Controls.First(c=>c.Get("type")=="image"&&!c.IsBound("width")&&c.Number("width",0)>0);
                    project.Edit(edit,new Dictionary<string,string>{{"width",((int)edit.Number("width",0)+17).ToString()}},"Geometry freshness probe");CheckHits(canvas,project);
                    project.UndoChange();CheckHits(canvas,project);
                    Console.WriteLine("PASS hit equivalence and scope cleanup: initial, focus, sample, geometry edit, Undo");
                }
            }
        }
    }
}