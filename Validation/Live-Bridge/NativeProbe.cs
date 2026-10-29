using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;
using MP1SkinStudio.LiveBridge;
class NativeProbe
{
 static int checks;
 static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL "+name);Console.WriteLine("PASS "+name);checks++;}
 static void Set(object obj,string field,object value){typeof(GUIWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);}
 static XmlDocument Frame(string epoch,string token,params string[] values){var doc=LiveWire.Document("frame");LiveWire.Attr(doc.DocumentElement,"epoch",epoch);var c=LiveWire.Add(doc.DocumentElement,"control");LiveWire.Attr(c,"token",token);for(int i=0;i<values.Length;i+=2){var node=LiveWire.Add(c,"set");LiveWire.Attr(node,"key",values[i]);node.InnerText=values[i+1];}return doc;}
 static void Reject(System.Action action){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,"invalid native frame rejected");}
 [STAThread]static int Main(string[] args)
 {
  try
  {
   GUIGraphicsContext.form=new System.Windows.Forms.Form{ClientSize=new Size(1920,1080)};GUIGraphicsContext.Skin=args[0];GUIGraphicsContext.SkinSize=new Size(1920,1080);GUIGraphicsContext.OverScanWidth=1920;GUIGraphicsContext.OverScanHeight=1080;
   var window=new GUIWindow();window.GetID=0;Set(window,"_windowLoaded",true);Set(window,"_windowXmlFileName",Path.Combine(args[0],"myHome.xml"));
   var label=(GUILabelControl)Activator.CreateInstance(typeof(GUILabelControl),new object[]{0});label.Type="label";label.GetID=21001;label.Description="Native text";label.Label="Original native text";label.SetPosition(101,103);label.Width=800;label.Height=70;label.TextColor=0xffffffff;label.StorePosition();window.Children.Add(label);
   var second=(GUILabelControl)Activator.CreateInstance(typeof(GUILabelControl),new object[]{0});second.Type="label";second.GetID=0;second.Description="Zero B";second.Label="Keep me";window.Children.Add(second);
   var session=new RuntimeSession(()=>window);var snapshot=session.Handle(LiveWire.Document("snapshot"));string epoch=snapshot.DocumentElement.GetAttribute("epoch"),token=((XmlElement)snapshot.SelectSingleNode("/live/control[@id='21001']")).GetAttribute("token");Check(snapshot.SelectNodes("/live/control").Count==2,"installed MP SDK controls captured");
   int x=label.XPosition,y=label.YPosition;var rectangle=typeof(GUIControl).GetField("_originalRectangle",BindingFlags.Instance|BindingFlags.NonPublic);object originalRect=rectangle.GetValue(label);
   session.Handle(Frame(epoch,token,"label","Edited native label","posX","240","posY","180","textcolor","ffaa5500"));Check(label.Label=="Edited native label","installed MP label setter applied");Check(label.XPosition==GUIGraphicsContext.ScaleHorizontal(240)&&label.YPosition==GUIGraphicsContext.ScaleVertical(180),"skin coordinates converted to native pixels");Check(label.TextColor==0xffaa5500,"native color applied");Check(second.Label=="Keep me","zero ID sibling unchanged");
   Reject(()=>session.Handle(Frame(epoch,token,"label","Invalid must roll back","width","-1")));Check(label.Label=="Edited native label","atomic failure retains native state");
   Reject(()=>session.Handle(Frame("stale",token,"label","Stale")));Check(label.Label=="Original native text"&&label.XPosition==x&&label.YPosition==y,"stale epoch restores exact native geometry and text");Check(rectangle.GetValue(label).Equals(originalRect),"animation baseline restored exactly");
   snapshot=session.Handle(LiveWire.Document("snapshot"));epoch=snapshot.DocumentElement.GetAttribute("epoch");token=((XmlElement)snapshot.SelectSingleNode("/live/control[@id='21001']")).GetAttribute("token");
   session.Handle(Frame(epoch,token,"label","Second edit"));session.Reset();Check(label.Label=="Original native text","explicit disconnect reset restores native text");
   snapshot=session.Handle(LiveWire.Document("snapshot"));epoch=snapshot.DocumentElement.GetAttribute("epoch");token=((XmlElement)snapshot.SelectSingleNode("/live/control[@id='21001']")).GetAttribute("token");
   var remove=LiveWire.Document("frame");LiveWire.Attr(remove.DocumentElement,"epoch",epoch);LiveWire.Attr(LiveWire.Add(remove.DocumentElement,"remove"),"token",token);session.Handle(remove);Check(!window.Children.Contains(label),"live remove detaches without disposing");session.Reset();Check(window.Children.Count==2&&Object.ReferenceEquals(window.Children[0],label),"reset restores exact native tree order");
   Console.WriteLine(checks+" native SDK checks passed.");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
