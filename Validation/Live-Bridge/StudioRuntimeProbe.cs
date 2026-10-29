using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio;
using MP1SkinStudio.Live;
class StudioRuntimeProbe
{
 static StudioForm form;static StudioLiveClient client;static volatile XmlDocument received;static int frames;static readonly List<string> results=new List<string>();
 static object Field(string name){return typeof(StudioForm).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);}
 static void Call(string name,params object[] args){typeof(StudioForm).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic,null,args.Select(x=>x.GetType()).ToArray(),null).Invoke(form,args);}
 static void Wait(Func<bool> condition){var timer=Stopwatch.StartNew();while(!condition()&&timer.ElapsedMilliseconds<8000){Application.DoEvents();Thread.Sleep(10);}if(!condition())throw new Exception("Timeout: "+((Label)Field("liveNotice")).Text);Application.DoEvents();}
 static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL "+name);results.Add("PASS "+name);Console.WriteLine("PASS "+name);}
 static XmlDocument Snapshot(){received=null;client.Send(LiveWire.Document("snapshot"));Wait(()=>received!=null);var result=received;Application.DoEvents();Wait(()=>(bool)Field("liveVerified"));return result;}
 static string Value(XmlDocument doc,string key){return doc.SelectSingleNode("/live/control[@id='21001']/value[@key='"+key+"']").InnerText;}
 static void Edit(Dictionary<string,TextBox> fields,string key,string value){int before=frames;fields[key].Text=value;Wait(()=>frames>before);}
 [STAThread]static int Main(string[] args)
 {
  try
  {
   Directory.CreateDirectory(args[2]);Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR",args[1]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   form=new StudioForm(args[0]);form.StartPosition=FormStartPosition.Manual;form.Location=new System.Drawing.Point(-20000,-20000);form.Show();form.PrepareExternalPreview(args[0],"myHome.xml","");Application.DoEvents();((Panel)Field("liveHost")).Visible=true;Call("ConnectLive");Wait(()=>(bool)Field("liveVerified"));client=(StudioLiveClient)Field("liveClient");client.Response+=doc=>{string command=doc.DocumentElement.GetAttribute("command");if(command=="snapshot")received=doc;if(command=="frame"&&doc.DocumentElement.GetAttribute("ok")=="true")Interlocked.Increment(ref frames);};
   var original=Snapshot();string epoch=original.DocumentElement.GetAttribute("epoch"),label=Value(original,"label"),x=Value(original,"posX");Check(original.SelectNodes("/live/control").Count>100,"Studio connects to the real MP Home page and verifies XML hashes");
   var project=(SkinProject)Field("project");var target=project.Controls.First(c=>c.Get("id")=="21001");typeof(StudioForm).GetMethod("SelectControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SkinControl)},null).Invoke(form,new object[]{target});var fields=(Dictionary<string,TextBox>)Field("fields");string disk=File.ReadAllText(target.Source.Path);
   Edit(fields,"label","Typed in Studio - live in MP");Check(Value(Snapshot(),"label")=="Typed in Studio - live in MP"&&!project.Dirty,"typing in Studio updates real MP without Apply or XML writes");
   Edit(fields,"posX","240");Edit(fields,"textcolor","ffffcc00");var edited=Snapshot();Check(Value(edited,"posX")=="240"&&Value(edited,"textcolor")=="ffffcc00","position and color inputs update real MP controls");Check(edited.DocumentElement.GetAttribute("epoch")==epoch,"same MP window and control identities: no page reload");
   Console.WriteLine("HOLDING real Studio visual for 40 seconds");var hold=Stopwatch.StartNew();while(hold.ElapsedMilliseconds<40000){Application.DoEvents();Thread.Sleep(20);}
   fields["posX"].Text="-";var delay=Stopwatch.StartNew();while(delay.ElapsedMilliseconds<150){Application.DoEvents();Thread.Sleep(10);}Check(Value(Snapshot(),"posX")=="240","invalid numeric draft preserves last valid real MP preview");
   int count=frames;Call("RevertPropertyInputs");Wait(()=>frames>count);Check(Value(Snapshot(),"label")==label&&Value(Snapshot(),"posX")==x,"Revert Inputs restores real MP text and position");
   Edit(fields,"label","Applied in Studio");Call("Apply");Check(project.Dirty&&Value(Snapshot(),"label")=="Applied in Studio","Apply commits editor model while real MP keeps its live preview");count=frames;Call("UndoEditor",false);Wait(()=>frames>count);Check(Value(Snapshot(),"label")==label&&!project.Dirty,"Undo restores real MP and exact editor source");
   target=project.Controls.First(c=>c.Get("id")=="21001");var callback=(Action<List<ControlEdit>>)typeof(SkinCanvas).GetField("LiveGesturePreview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Field("canvas"));count=frames;callback(new List<ControlEdit>{new ControlEdit(target,new Dictionary<string,string>{{"posX","340"}})});Wait(()=>frames>count);Check(Value(Snapshot(),"posX")=="340"&&!project.Dirty,"in-progress canvas gesture updates real MP before committing XML");callback(null);
   Wait(()=>(bool)Field("liveVerified"));fields=(Dictionary<string,TextBox>)Field("fields");Edit(fields,"label","Disconnect this preview");Call("DisconnectLive");Wait(()=>Field("liveClient")==null);Check(File.ReadAllText(target.Source.Path)==disk,"real MP workflow leaves disk XML unchanged");
   Call("RevertPropertyInputs");Call("ConnectLive");Wait(()=>(bool)Field("liveVerified"));client=(StudioLiveClient)Field("liveClient");client.Response+=doc=>{if(doc.DocumentElement.GetAttribute("command")=="snapshot")received=doc;};Check(Value(Snapshot(),"label")==label&&Value(Snapshot(),"posX")==x,"disconnect and reconnect restore real MP native baseline");Call("DisconnectLive");Wait(()=>Field("liveClient")==null);Console.WriteLine(results.Count+" real Studio-to-MP checks passed.");return 0;
  }catch(Exception ex){results.Add(ex.ToString());Console.Error.WriteLine(ex);return 1;}finally{if(form!=null)form.Dispose();File.WriteAllLines(Path.Combine(args[2],"Results.txt"),results);}
 }
}
