using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class ControlActionsFormProbe
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static string Control(string type,int id,string extra="")
 {return "<control><type>"+type+"</type><id>"+id+"</id><posX>100</posX><posY>"+(id*5)+"</posY><width>200</width><height>50</height><onleft>10</onleft><onright>30</onright><onup>10</onup><ondown>30</ondown>"+extra+"</control>";}
 [STAThread]static int Main(string[] args){try{return Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static int Run(string[] args)
 {
  string output=Path.GetFullPath(args[0]),root=Path.Combine(output,"Authored-Skin");Directory.CreateDirectory(root);
  File.WriteAllText(Path.Combine(root,"references.xml"),"<window />");
  string file=Path.Combine(root,"main.xml");File.WriteAllText(file,"<window><id>9020</id><defaultcontrol>20</defaultcontrol><controls>"+Control("button",10)+Control("button",30).Replace("<onup>10</onup>","<onup>20</onup>")+Control("slider",20)+Control("spincontrol",40)+Control("spincontrol",50,"<orientation>vertical</orientation>")+Control("spincontrol",60,"<orientation>#unknown</orientation>")+"</controls></window>");
  byte[] original=File.ReadAllBytes(file);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);int events=0;
  using(var form=new StudioForm(root))
  {
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.PrepareNavigationPreview();
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;var canvas=(SkinCanvas)typeof(StudioForm).GetField("canvas",flags).GetValue(form);var pick=typeof(StudioForm).GetMethod("SelectControl",flags,null,new[]{typeof(SkinControl)},null);var follow=typeof(SkinCanvas).GetMethod("ExerciseFollow",flags);var status=(Label)typeof(StudioForm).GetField("status",flags).GetValue(form);
   Action<int> select=id=>pick.Invoke(form,new object[]{canvas.Project.Controls.Single(c=>SkinProject.ControlId(c)==id)});
   Action<Keys,string> guard=(key,message)=>{int before=canvas.State.FocusId;string signature=ProjectFile.ContentSignature(canvas.Project);follow.Invoke(canvas,new object[]{key});events++;Check(canvas.State.FocusId==before&&ProjectFile.ContentSignature(canvas.Project)==signature&&status.Text.Contains(message),"Blocked form arrow changed state or lost explanation: "+key);};
   Action<Keys,int> move=(key,id)=>{follow.Invoke(canvas,new object[]{key});events++;Check(canvas.State.FocusId==id&&SkinProject.ControlId(canvas.Selected)==id,"Cross-axis form route failed: "+key);};
   select(20);guard(Keys.Left,"Slider");guard(Keys.Right,"Slider");move(Keys.Down,30);move(Keys.Up,20);
   select(40);guard(Keys.Left,"Spin-control");guard(Keys.Right,"Spin-control");move(Keys.Down,30);select(40);move(Keys.Up,10);
   select(50);guard(Keys.Up,"Spin-control");guard(Keys.Down,"Spin-control");move(Keys.Right,30);select(50);move(Keys.Left,10);
   select(60);foreach(var key in new[]{Keys.Left,Keys.Right,Keys.Up,Keys.Down})guard(key,"orientation is unverified");
   Check(PreviewProblems.Run(canvas.Project,canvas.State).Issues.Count(i=>i.Code=="PREVIEW_INTERACTION")==4,"Expected four informational interaction findings.");
   Check(!canvas.Project.Dirty&&canvas.Project.Undo.Count==0&&File.ReadAllBytes(file).SequenceEqual(original),"Arrow events changed authored XML/history.");
   string saved=Path.Combine(output,"Control-Actions.mp1skinproj");ProjectFile.Save(canvas.Project,saved);form.ResumeProjectFile(saved);Check(canvas.State.FocusId==60&&!canvas.Project.Dirty,"Guarded focus did not reopen.");
  }
  string result="PASS "+events+" synthetic Windows canvas arrow events: slider/horizontal-spin/vertical-spin/unknown-orientation guards retain focus, project signature and explanatory status; valid cross-axis routes update focus and selection. Four informational interaction findings; no XML/undo mutations; focus 60 survives save/reopen. This is an automated form test, not attended input or a MediaPortal client run.";
  File.WriteAllText(Path.Combine(output,"Form-Results.txt"),result+Environment.NewLine);Console.WriteLine(result);return 0;
 }
}
