using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class PlainNavigationFormProbe
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            using(var form=new StudioForm(args[0]))
            {
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);
                form.LoadProject(args[0],"demo.free.xml");form.PrepareNavigationPreview();
                var canvas=(SkinCanvas)typeof(StudioForm).GetField("canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                var exercise=typeof(SkinCanvas).GetMethod("ExerciseFollow",BindingFlags.NonPublic|BindingFlags.Instance);
                string xml=ProjectFile.XmlSignature(canvas.Project);
                var keys=new[]{Keys.Right,Keys.Down,Keys.Left,Keys.Up};var expected=new[]{853,854,852,851};
                for(int i=0;i<keys.Length;i++)
                {
                    exercise.Invoke(canvas,new object[]{keys[i]});
                    if(canvas.State.FocusId!=expected[i]||SkinProject.ControlId(canvas.Selected)!=expected[i])throw new Exception("Form handler failed at "+keys[i]+"; focus "+canvas.State.FocusId);
                }
                if(canvas.Project.Dirty||canvas.Project.Undo.Count>0||ProjectFile.XmlSignature(canvas.Project)!=xml)throw new Exception("Arrow events changed XML");
                string file=Path.Combine(args[1],"Plain-Demo.mp1skinproj");ProjectFile.Save(canvas.Project,file);
                var restored=ProjectFile.Load(file);if(new PreviewState(restored).FocusId!=851)throw new Exception("Saved form focus changed");
                form.RenderTo(Path.Combine(args[1],"Plain-Demo.png"));
                string result="PASS bundled demo.free.xml: four synthetic canvas KeyDown events traverse 851→853→854→852→851 across ordinary group branches through the production form handler. Focus and designer selection agree; XML and undo history unchanged. Saved project restores focus 851. Production form render inspected separately. This is an automated WinForms event test, not attended keyboard input or a MediaPortal client run.";
                File.WriteAllText(Path.Combine(args[1],"Form-Results.txt"),result+Environment.NewLine);Console.WriteLine(result);
            }
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
