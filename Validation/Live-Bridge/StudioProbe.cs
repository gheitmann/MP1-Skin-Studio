using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.GUI.Library;
using MP1SkinStudio;
using MP1SkinStudio.Live;
using MP1SkinStudio.LiveBridge;
class StudioProbe
{
 static readonly List<string> results=new List<string>();static StudioForm form;
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static void Call(string method,params object[] args){typeof(StudioForm).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic,null,args.Select(a=>a.GetType()).ToArray(),null).Invoke(form,args);}
 static void Check(bool value,string name){results.Add((value?"PASS ":"FAIL ")+name);if(!value)throw new Exception(name);}
 static void Wait(Func<bool> condition){var watch=Stopwatch.StartNew();while(!condition()&&watch.ElapsedMilliseconds<8000){Application.DoEvents();Thread.Sleep(10);}if(!condition())throw new Exception("Timed out: "+((Label)Field(form,"liveNotice")).Text);Application.DoEvents();}
 [STAThread]static int Main(string[] args)
 {
  LiveServer server=null;
  try
  {
   Directory.CreateDirectory(args[1]);Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR",Path.Combine(args[1],"ipc"));Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   GUIGraphicsContext.form=new Form{ClientSize=new Size(1920,1080)};GUIGraphicsContext.form.CreateControl();GUIGraphicsContext.Skin=args[0];GUIGraphicsContext.SkinSize=new Size(1920,1080);GUIGraphicsContext.OverScanWidth=1920;GUIGraphicsContext.OverScanHeight=1080;
   var window=new GUIWindow();window.GetID=0;typeof(GUIWindow).GetField("_windowLoaded",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,true);typeof(GUIWindow).GetField("_windowXmlFileName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,Path.Combine(args[0],"myHome.xml"));
   var native=(GUILabelControl)Activator.CreateInstance(typeof(GUILabelControl),new object[]{0});native.Type="label";native.GetID=21001;native.Description="Studio live test text";native.Label="Live bridge: original text";native.SetPosition(100,110);native.Width=900;native.Height=70;native.StorePosition();window.Children.Add(native);
   form=new StudioForm(args[0]);form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();form.PrepareExternalPreview(args[0],"myHome.xml","");Application.DoEvents();((Panel)Field(form,"liveHost")).Visible=true;
   var session=new RuntimeSession(()=>window);server=new LiveServer(session,action=>(System.Xml.XmlDocument)form.Invoke(new Func<System.Xml.XmlDocument>(()=>{lock(GUIGraphicsContext.RenderLock)return action();})));server.Start();Call("ConnectLive");Wait(()=>(bool)Field(form,"liveVerified"));Check(true,"Studio discovers, authenticates and verifies original XML hashes");
   var project=(SkinProject)Field(form,"project");var control=project.Controls.First(c=>c.Get("id")=="21001");typeof(StudioForm).GetMethod("SelectControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SkinControl)},null).Invoke(form,new object[]{control});var fields=(Dictionary<string,TextBox>)Field(form,"fields");string before=control.Source.Text;
   fields["label"].Text="Typing updates native MP control";Wait(()=>native.Label=="Typing updates native MP control");Check(control.Source.Text==before&&!project.Dirty,"typing previews native text without Apply or XML modification");
   fields["posX"].Text="230";Wait(()=>native.XPosition==GUIGraphicsContext.ScaleHorizontal(230));Check(true,"pending geometry input updates native pixels");
   fields["posX"].Text="-";var timer=Stopwatch.StartNew();while(timer.ElapsedMilliseconds<200){Application.DoEvents();Thread.Sleep(10);}Check(native.XPosition==GUIGraphicsContext.ScaleHorizontal(230),"unfinished numeric input retains last valid preview");
   Call("RevertPropertyInputs");Wait(()=>native.Label=="Live bridge: original text"&&native.XPosition==GUIGraphicsContext.ScaleHorizontal(100));Check(true,"Revert Inputs restores native text and geometry");
   fields["label"].Text="Applied native text";Wait(()=>native.Label=="Applied native text");Call("Apply");Check(project.Dirty&&native.Label=="Applied native text","Apply retains live preview and commits editor model");Call("UndoEditor",false);Wait(()=>native.Label=="Live bridge: original text");Check(!project.Dirty,"Undo restores native label and exact source");
   control=project.Controls.First(c=>c.Get("id")=="21001");var gesture=new List<ControlEdit>{new ControlEdit(control,new Dictionary<string,string>{{"posX","340"}})};var callback=(System.Action<List<ControlEdit>>)typeof(SkinCanvas).GetField("LiveGesturePreview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Field(form,"canvas"));callback(gesture);Wait(()=>native.XPosition==GUIGraphicsContext.ScaleHorizontal(340));Check(!project.Dirty,"canvas gesture draft streams before model commit");callback(null);Wait(()=>native.XPosition==GUIGraphicsContext.ScaleHorizontal(100));Check(true,"cancelled canvas gesture restores baseline");
   fields["label"].Text="Disconnect restores this";Wait(()=>native.Label=="Disconnect restores this");Call("DisconnectLive");Wait(()=>Field(form,"liveClient")==null);Check(native.Label=="Live bridge: original text","Studio disconnect restores MP SDK control");
   Check(File.ReadAllText(control.Source.Path)==before,"on-disk isolated XML remains byte-content identical");return 0;
  }catch(Exception ex){results.Add(ex.ToString());return 1;}
  finally{if(form!=null)form.Dispose();if(server!=null)server.Dispose();if(GUIGraphicsContext.form!=null)GUIGraphicsContext.form.Dispose();File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);foreach(string line in results)Console.WriteLine(line);}
 }
}
