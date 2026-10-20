using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class CheckmarkFormProbe
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
                form.LoadProject(args[0],"demo.checkmark.xml");form.PrepareNavigationPreview();
                var canvas=(SkinCanvas)typeof(StudioForm).GetField("canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                var exercise=typeof(SkinCanvas).GetMethod("ExerciseFollow",BindingFlags.NonPublic|BindingFlags.Instance);
                string xml=ProjectFile.XmlSignature(canvas.Project);
                var keys=new[]{Keys.Down,Keys.Down,Keys.Down,Keys.Up,Keys.Up,Keys.Up};var expected=new[]{882,883,884,883,882,881};
                for(int i=0;i<keys.Length;i++)
                {
                    exercise.Invoke(canvas,new object[]{keys[i]});
                    if(canvas.State.FocusId!=expected[i]||SkinProject.ControlId(canvas.Selected)!=expected[i])throw new Exception("Form handler failed at "+keys[i]+"; focus "+canvas.State.FocusId);
                }
                if(canvas.Project.Dirty||canvas.Project.Undo.Count>0||ProjectFile.XmlSignature(canvas.Project)!=xml)throw new Exception("Arrow events changed XML");
                form.RenderTo(Path.Combine(args[1],"Checkmark-States.png"));
                exercise.Invoke(canvas,new object[]{Keys.Down});
                var fields=(System.Collections.Generic.Dictionary<string,TextBox>)typeof(StudioForm).GetField("fields",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                foreach(string key in CheckmarkPresentation.Fields)if(!fields.ContainsKey(key))throw new Exception("Missing checkmark field "+key);
                if(fields["markWidth"].Text!="70"||fields["selected"].Text!="true")throw new Exception("Checkmark fields lost model values");
                fields["selected"].Text="false";
                typeof(StudioForm).GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(form,null);
                if(CheckmarkPresentation.Selected(canvas.Selected,canvas.State)!=false||!canvas.Project.Dirty||canvas.Project.Undo.Count!=1)throw new Exception("Field Apply did not edit selection");
                string file=Path.Combine(args[1],"Checkmark-Demo.mp1skinproj");ProjectFile.Save(canvas.Project,file);
                var restored=ProjectFile.Load(file);if(new PreviewState(restored).FocusId!=882||CheckmarkPresentation.Selected(restored.Controls.Find(c=>SkinProject.ControlId(c)==882),new PreviewState(restored))!=false)throw new Exception("Saved form focus changed");
                form.RenderTo(Path.Combine(args[1],"Checkmark-Demo.png"));
                string result="PASS bundled demo.checkmark.xml: six synthetic canvas KeyDown events traverse 881→882→883→884→883→882→881 across static and unverified checkmarks through the production form handler. Focus and designer selection agree; XML and undo history unchanged. After the clean route, six checkmark-only fields show correct values; selected=true→false Apply creates one XML undo step. Saved project restores edited selection and focus 882. Production form render inspected separately. This is an automated WinForms event test, not attended keyboard input or a MediaPortal client run.";
                File.WriteAllText(Path.Combine(args[1],"Form-Results.txt"),result+Environment.NewLine);Console.WriteLine(result);
            }
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
