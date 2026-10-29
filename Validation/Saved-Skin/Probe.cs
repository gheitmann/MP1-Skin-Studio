using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MP1SkinStudio;
class SavedSkinProbe
{
 static readonly List<string> results=new List<string>();static Exception failure;
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static void Call(StudioForm form,string name,params object[] args){typeof(StudioForm).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic,null,args.Select(a=>a.GetType()).ToArray(),null).Invoke(form,args);}
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);results.Add("PASS "+name);}
 static void SaveThroughDialog(StudioForm form,bool inspect)
 {
  var clock=Stopwatch.StartNew();bool clicked=false,reviewed=!inspect;var timer=new System.Windows.Forms.Timer{Interval=30};
  timer.Tick+=(s,e)=>
  {
   try
   {
    var review=Application.OpenForms.OfType<ChangeReviewDialog>().FirstOrDefault();if(review!=null){Check(((Array)Field(review,"files")).Length==1,"output XML review displays the exact pending target change");reviewed=true;review.Close();return;}
    var dialog=Application.OpenForms.OfType<SaveSkinDialog>().FirstOrDefault();if(dialog==null)return;
    if(clock.ElapsedMilliseconds>15000)throw new Exception("Save dialog timeout: "+((Label)Field(dialog,"status")).Text);
    if(!reviewed){((Button)Field(dialog,"review")).PerformClick();return;}
    if(!clicked){var save=(Button)Field(dialog,"save");Check(save.Enabled,"review enables updating a valid output copy");clicked=true;save.PerformClick();return;}
    var status=(Label)Field(dialog,"status");if(status.Text.StartsWith("Changes saved.")){Check(!((Button)Field(dialog,"save")).Enabled,"successful save refreshes to an up-to-date copy");dialog.Close();timer.Stop();}
    else if(status.Text.StartsWith("Save did not complete:"))throw new Exception(status.Text);
   }
   catch(Exception ex){failure=ex;timer.Stop();foreach(var f in Application.OpenForms.Cast<Form>().Where(f=>f is ChangeReviewDialog||f is SaveSkinDialog).ToArray())f.Close();}
  };
  timer.Start();try{Call(form,"SaveSkinCopy");if(failure!=null)throw failure;}finally{timer.Dispose();}
 }
 [STAThread]static int Main(string[] args)
 {
  Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try
  {
   string source=Path.Combine(args[0],"Source");Directory.CreateDirectory(source);File.WriteAllText(Path.Combine(source,"main.xml"),"<window><id>0</id><controls><control><type>label</type><id>1</id><posX>100</posX><posY>100</posY><width>400</width><height>50</height><label>Original</label></control></controls></window>");
   using(var form=new StudioForm(source))
   {
    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();form.PrepareExternalPreview(source,"main.xml","");Application.DoEvents();var project=(SkinProject)Field(form,"project");string target=SkinSavePlan.Capture(project).CreateCopy(args[0],"Output",CancellationToken.None);project.Editor.SavedSkinRoot=target;
    typeof(StudioForm).GetMethod("SelectControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SkinControl)},null).Invoke(form,new object[]{project.Controls[0]});var fields=(Dictionary<string,TextBox>)Field(form,"fields");fields["label"].Text="Saved from Studio";Check(!project.Dirty,"pending inputs remain unapplied before Save skin");SaveThroughDialog(form,true);
    Check(File.ReadAllText(Path.Combine(target,"main.xml")).Contains("Saved from Studio")&&project.Dirty&&project.Undo.Count==1,"Save skin applies valid drafts and saves the reviewed output copy");Check(File.ReadAllText(Path.Combine(source,"main.xml")).Contains(">Original<"),"dialog workflow preserves source XML");
    Call(form,"UndoEditor",false);SaveThroughDialog(form,false);Check(!project.Dirty&&File.ReadAllText(Path.Combine(target,"main.xml")).Contains(">Original<")&&project.Redo.Count==1,"dialog saves Undo without losing Redo or source baseline");
    form.Size=form.MinimumSize;Call(form,"ToggleDropPalette");form.PerformLayout();Application.DoEvents();var canvas=(SkinCanvas)Field(form,"canvas");Check(canvas.Width>=150&&canvas.Height>=100,"Save skin toolbar retains a usable minimum-size canvas with Toolbox");Call(form,"ToggleDropPalette");
    using(var dialog=new SkinCopyDialog(project,true)){Check(!((CheckBox)Field(dialog,"open")).Checked&&!((CheckBox)Field(dialog,"open")).Enabled&&((CheckBox)Field(dialog,"include")).Checked&&!((CheckBox)Field(dialog,"include")).Enabled,"first-save dialog always includes edits and keeps the current live source");}
    File.AppendAllText(Path.Combine(target,"main.xml"),"\n<!--external-->");using(var dialog=new SaveSkinDialog(project)){dialog.Show();Application.DoEvents();Check(!((Button)Field(dialog,"save")).Enabled&&((Label)Field(dialog,"status")).Text.Contains("conflicts"),"target conflict disables writing while leaving New skin copy available");Check(((Button)Field(dialog,"create")).Enabled,"conflicted output can be replaced by creating a separate new copy");using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.Size));bitmap.Save(Path.Combine(args[0],"save-copy-conflict.png"));}dialog.Close();}
   }
   return 0;
  }catch(Exception ex){results.Add(ex.ToString());return 1;}finally{File.WriteAllLines(Path.Combine(args[0],"Results.txt"),results);foreach(string line in results)Console.WriteLine(line);}
 }
}
