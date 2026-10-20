using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;

class WorkflowToolsProbe
{
    static readonly List<string> results=new List<string>();
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object value,string name){return (T)value.GetType().GetField(name,Private).GetValue(value);}
    static object Call(object value,string name,params object[] args){return value.GetType().GetMethod(name,Private).Invoke(value,args);}
    static void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);if(!ok)throw new InvalidOperationException(name);}
    static void Render(Form form,string path){using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(path);}}
    [STAThread]static int Main(string[] args)
    {
        Directory.CreateDirectory(args[1]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            using(var form=new StudioForm(args[0]))
            {
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();form.Hide();form.Show();Application.DoEvents();
                Call(form,"RunWorkflowToolsUiSmoke",args[1],new Action<bool,string>(Check));
                var project=Field<SkinProject>(form,"project");var canvas=Field<SkinCanvas>(form,"canvas");
                var tool=new DataObject(typeof(ElementDrag).FullName,new ElementDrag{Kind="button",ProjectRoot=project.Root});string xml=ProjectFile.XmlSignature(project);int count=project.Controls.Count;
                var outside=(DragDropEffects)Call(canvas,"ExerciseDrop",tool,new PointF(-30,300));Check(outside==DragDropEffects.None&&project.Controls.Count==count&&ProjectFile.XmlSignature(project)==xml,"Dropping outside the skin is rejected through the canvas event path");
                canvas.NavigationMode=true;var blocked=(DragDropEffects)Call(canvas,"ExerciseDrop",tool,new PointF(300,300));Check(blocked==DragDropEffects.None&&project.Controls.Count==count,"Navigation mode rejects insertion through the canvas event path");canvas.NavigationMode=false;
                Call(form,"ToggleDropPalette");Application.DoEvents();Render(form,Path.Combine(args[1],"toolbox.png"));
                form.Size=form.MinimumSize;Application.DoEvents();var designerButton=FindButton(form,"Toolbox");Check(designerButton!=null&&designerButton.Right<=designerButton.Parent.ClientSize.Width&&canvas.Width>=150&&canvas.Height>=100,"Toolbox toggle fits and retains a usable canvas at minimum window size");
                Render(form,Path.Combine(args[1],"toolbox-minimum.png"));form.Hide();
            }
            using(var installed=new StudioForm(args[2]))
            {
                installed.StartPosition=FormStartPosition.Manual;installed.Location=new Point(-20000,-20000);installed.Show();installed.Hide();installed.Show();Application.DoEvents();
                var project=Field<SkinProject>(installed,"project");string xml=ProjectFile.XmlSignature(project);Call(installed,"ToggleDropPalette");var grid=Field<TextureGrid>(installed,"dropTextures");
                Check(grid.Assets.Count>0&&grid.DragImages&&ProjectFile.XmlSignature(project)==xml&&!project.Dirty,"Installed skin toolbox discovers Media assets without editing XML");installed.Hide();
            }
            return 0;
        }
        catch(Exception ex){results.Add(ex.ToString());return 1;}
        finally{File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);}
    }
    static Button FindButton(Control parent,string text){foreach(Control c in parent.Controls){var button=c as Button;if(button!=null&&button.Text==text)return button;var found=FindButton(c,text);if(found!=null)return found;}return null;}
}
