using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class PackageReadinessProbe
{
 [STAThread]static int Main(string[] args){try{return Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static int Run(string[] args)
 {
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Directory.CreateDirectory(args[1]);var lines=new List<string>();
  using(var form=new StudioForm(args[0]))
  {
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);
   var binding=BindingFlags.NonPublic|BindingFlags.Instance;var dirty=typeof(StudioForm).GetMethod("SessionNeedsSaving",binding);var save=typeof(StudioForm).GetMethod("SaveSession",binding);
   foreach(string file in Directory.GetFiles(args[0],"demo*.xml").Where(p=>Path.GetFileName(p)!="demo.menu.xml").OrderBy(p=>p))
   {
    string page=Path.GetFileName(file);form.LoadProject(args[0],page);
    var canvas=(SkinCanvas)typeof(StudioForm).GetField("canvas",binding).GetValue(form);
    if((bool)dirty.Invoke(form,null)||form.Text.EndsWith("*")||canvas.Project.Dirty)throw new Exception("Opening falsely marks dirty: "+page);
    int focus=canvas.State.FocusId;var signature=ProjectFile.ContentSignature(canvas.Project);
    typeof(SkinCanvas).GetMethod("ExerciseZoom",binding).Invoke(canvas,new object[]{new Point(100,100),120});
    if(!(bool)dirty.Invoke(form,null))throw new Exception("Zoom was not dirty: "+page);
    typeof(SkinCanvas).GetMethod("FitView",BindingFlags.Public|BindingFlags.Instance).Invoke(canvas,null);
    if((bool)dirty.Invoke(form,null)||ProjectFile.ContentSignature(canvas.Project)!=signature)throw new Exception("Fit failed to restore clean state: "+page);
    string project=Path.Combine(args[1],Path.GetFileNameWithoutExtension(page)+".mp1skinproj");save.Invoke(form,new object[]{project});form.ResumeProjectFile(project);
    if((bool)dirty.Invoke(form,null)||form.Text.EndsWith("*")||canvas.Project.Dirty)throw new Exception("Reopening falsely marks dirty: "+page);
    var loaded=ProjectFile.Load(project);if(new PreviewState(loaded).FocusId!=focus)throw new Exception("Default focus did not reopen: "+page);
    lines.Add("PASS "+page+": clean open; zoom marks unsaved; Fit restores exact clean signature; save/reopen remains clean with focus "+focus+".");
   }
  }
  File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);return 0;
 }
}
