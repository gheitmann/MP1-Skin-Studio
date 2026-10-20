using System;using System.IO;using System.Linq;using System.Drawing;using System.Reflection;using System.Windows.Forms;using System.Collections.Generic;using MP1SkinStudio;
class TextSelectionProbe
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static List<string> results=new List<string>();
    static T Field<T>(object o,string name){return (T)o.GetType().GetField(name,flags).GetValue(o);}
    static object Call(object o,string name,params object[] args){try{return o.GetType().GetMethods(flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);}
    static SkinControl Named(SkinProject p,string name){return p.Controls.Single(c=>c.Get("description")==name);}
    static Point PointAt(SkinCanvas canvas,float x,float y){return (Point)Call(canvas,"PointForTest",x,y);}
    static void Mouse(SkinCanvas canvas,string name,Point at,int clicks=1){Call(canvas,name,new MouseEventArgs(MouseButtons.Left,clicks,at.X,at.Y,0));}
    [STAThread]static void Main(string[] args)
    {
        Application.EnableVisualStyles();string demo=args[0],skin=args[1],output=args[2];Directory.CreateDirectory(output);
        using(var form=new StudioForm(demo))
        {
            form.LoadProject(skin,"MediaTip.xml");form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
            var project=Field<SkinProject>(form,"project");var canvas=Field<SkinCanvas>(form,"canvas");var album=Named(project,"Music Album Artist Label");var artist=Named(project,"Music Artist Label");string xml=ProjectFile.XmlSignature(project);
            Call(form,"SelectControl",album);
            using(var image=new Bitmap(canvas.Width,canvas.Height))using(var graphics=Graphics.FromImage(image))
            {
                var a=(RectangleF)Call(canvas,"SelectionBounds",graphics,album);var b=(RectangleF)Call(canvas,"SelectionBounds",graphics,artist);
                Check(a.Height<canvas.BoundsOf(album).Height&&a.Bottom<b.Top&&a.Right<canvas.BoundsOf(Named(project,"Music Album Artist Value")).Left,"Real MediaTip caption frame excludes next row and value column");
                results.Add("MEASURE raw="+canvas.BoundsOf(album)+" caption="+a+" next="+b);
                var point=PointAt(canvas,b.X+b.Width/2,b.Y+b.Height/2);Mouse(canvas,"OnMouseDown",point);Mouse(canvas,"OnMouseUp",point);
                Check(Field<SkinControl>(form,"selected").Key==artist.Key,"Clicking Artists selects Artists rather than the overlapping Album Artists fallback box");
                var hits=(SkinControl[])Call(canvas,"OverlappingAt",point);Check(hits.Any(c=>c.Key==artist.Key)&&!hits.Any(c=>c.Key==album.Key),"Overlap menu uses the same caption target as the blue frame");
                bool textDialog=false;using(var timer=new Timer{Interval=30})
                {
                    timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<TextEditDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();textDialog=Field<SkinControl>(form,"selected").Key==artist.Key;dialog.DialogResult=DialogResult.Cancel;dialog.Close();};
                    timer.Start();Mouse(canvas,"OnMouseDown",point,2);Mouse(canvas,"OnMouseDoubleClick",point,2);Mouse(canvas,"OnMouseUp",point,2);timer.Stop();
                }
                Check(textDialog&&ProjectFile.XmlSignature(project)==xml,"Double-click opens the text editor for the visible row and cancellation preserves XML");
                Call(form,"SelectControl",album);var start=PointAt(canvas,a.X+a.Width/2,a.Y+a.Height/2);var end=PointAt(canvas,a.X+a.Width/2+20,a.Y+a.Height/2);Mouse(canvas,"OnMouseDown",start);Mouse(canvas,"OnMouseMove",end,0);Mouse(canvas,"OnMouseUp",end);
                Check(Named(project,"Music Album Artist Label").Number("posX",0)==650&&project.Undo.Count==1,"Dragging the text caption moves its XML position in one transaction");
                Field<Button>(form,"undo").PerformClick();Check(ProjectFile.XmlSignature(project)==xml&&!project.Dirty,"Undo restores exact original XML after caption dragging");
                Call(form,"SelectControl",Named(project,"Music Album Artist Label"));var fields=Field<Dictionary<string,TextBox>>(form,"fields");fields["width"].Text="100";Call(form,"Apply");Check(Named(project,"Music Album Artist Label").Get("width")=="100","Explicit Width remains editable in Properties");Field<Button>(form,"undo").PerformClick();
                Check(ProjectFile.XmlSignature(project)==xml&&!project.Dirty&&Named(project,"Music Album Artist Label").Get("height")=="","Outline inspection never inserts font-derived XML dimensions");
                form.RenderTo(Path.Combine(output,"MediaTip-Text-Selection.png"));
            }
        }
        foreach(string suite in new[]{"RunDesignerUiSmoke","RunSearchUiSmoke","RunTextEditUiSmoke"})using(var form=new StudioForm(demo))
        {
            form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
            Call(form,suite,new Action<bool,string>(Check));
        }
        results.Add(results.Count(r=>r.StartsWith("PASS "))+" passed, 0 failed. Synthetic forms/mouse events; no desktop input.");File.WriteAllLines(Path.Combine(output,"Results.txt"),results);Console.WriteLine(results.Last());
    }
}
