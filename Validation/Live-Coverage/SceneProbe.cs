using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Xml;
using MP1SkinStudio.Live;
class SceneProbe
{
 static NamedPipeClientStream pipe;static string epoch,directory;static int checks;static readonly List<string> results=new List<string>();
 static XmlDocument Rpc(XmlDocument request){LiveWire.Write(pipe,request);var response=LiveWire.Read(pipe);if(response.DocumentElement.GetAttribute("ok")=="false")throw new Exception(response.OuterXml);return response;}
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;results.Add("PASS "+name);Console.WriteLine("PASS "+name);}
 static XmlDocument Snapshot(){return Rpc(LiveWire.Document("snapshot"));}
 static XmlElement Item(XmlDocument doc,int id){return (XmlElement)doc.SelectSingleNode("/live/control[@id='"+id+"']");}
 static string Value(XmlDocument doc,int id,string field){return Item(doc,id).SelectSingleNode("value[@key='"+field+"']").InnerText;}
 static bool Near(XmlDocument doc,int id,string field,int expected){int actual;return Int32.TryParse(Value(doc,id,field),out actual)&&Math.Abs(actual-expected)<=1;}
 static XmlDocument Frame(XmlDocument baseline,params string[][] changes){var doc=LiveWire.Document("frame");LiveWire.Attr(doc.DocumentElement,"epoch",epoch);foreach(var values in changes){var node=LiveWire.Add(doc.DocumentElement,"control");LiveWire.Attr(node,"token",Item(baseline,Int32.Parse(values[0])).GetAttribute("token"));for(int i=1;i<values.Length;i+=2){var set=LiveWire.Add(node,"set");LiveWire.Attr(set,"key",values[i]);set.InnerText=values[i+1];}}return doc;}
 static void Hold(int seconds){for(int i=0;i<seconds;i++){Rpc(LiveWire.Document("ping"));Thread.Sleep(1000);}}
 static XmlElement NativeState(){for(int i=0;i<20;i++){try{var doc=new XmlDocument{XmlResolver=null};doc.Load(Path.Combine(directory,"coverage-state.xml"));return doc.DocumentElement;}catch(IOException){Thread.Sleep(100);}}throw new Exception("Fixture observer did not produce native state.");}
 static int Main(string[] args)
 {
  try
  {
   directory=args[0];Directory.CreateDirectory(args[1]);Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR",directory);var endpoint=LiveEndpoint.Discover().Single();pipe=new NamedPipeClientStream(".",endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous);pipe.Connect(3000);var hello=LiveWire.Document("hello");LiveWire.Attr(hello.DocumentElement,"nonce",endpoint.Nonce);Rpc(hello);
   var activate=LiveWire.Document("activate");LiveWire.Attr(activate.DocumentElement,"window",90110);Rpc(activate);Hold(1);var baseline=Snapshot();epoch=baseline.DocumentElement.GetAttribute("epoch");Check(baseline.DocumentElement.GetAttribute("window")=="90110","real MP loads the isolated specialized-control scene");Check(baseline.DocumentElement.GetAttribute("bridgeVersion")=="1.1.0.0","actual MP process loads bridge 1.1");Check(NativeState().GetAttribute("listItems")=="6"&&NativeState().GetAttribute("filmstripItems")=="4","fixture starts with six list rows and four filmstrip items");
   var frame=Frame(baseline,new[]{"21101","label","Live scrolling text in MediaPortal. The font and color are changed from Studio's bridge.","font","font18","textcolor","ffffcc00"},new[]{"21102","label","Live multiline text in MediaPortal. Restoring the original is checked after this preview.","font","font18","textcolor","ff44ddff"},new[]{"21103","textureHeight","90","spaceBetweenItems","12","itemWidth","50","itemHeight","45","font","font18","textcolor","ffffaa55"},new[]{"21104","textureWidth","180","textureHeight","200","thumbWidth","140","thumbHeight","160","itemWidth","210","itemHeight","240"});Rpc(frame);Hold(1);var changed=Snapshot();
   Check(Value(changed,21101,"label").StartsWith("Live scrolling text"),"real scrolling text updates");Check(Value(changed,21102,"label").StartsWith("Live multiline text"),"real multiline text updates");Check(Value(changed,21101,"font")=="font18"&&Value(changed,21101,"textcolor")=="ffffcc00","real scrolling font and color update");Check(Near(changed,21103,"textureHeight",90)&&Near(changed,21103,"itemHeight",45),"real list row and icon dimensions stay independent");Check(Near(changed,21104,"textureWidth",180)&&Near(changed,21104,"thumbWidth",140),"real filmstrip dimensions update: texture="+Value(changed,21104,"textureWidth")+", thumb="+Value(changed,21104,"thumbWidth"));Check(NativeState().GetAttribute("listItems")=="6"&&NativeState().GetAttribute("filmstripItems")=="4","native list data survives real layout rebuilding");Check(changed.DocumentElement.GetAttribute("epoch")==epoch,"same runtime control identities remain active");
   Console.WriteLine("HOLDING changed specialized scene for 35 seconds");Hold(35);
   var empty=LiveWire.Document("frame");LiveWire.Attr(empty.DocumentElement,"epoch",epoch);Rpc(empty);Hold(1);var restored=Snapshot();foreach(var spec in new[]{new[]{"21101","label","font","textcolor"},new[]{"21102","label","font","textcolor"},new[]{"21103","textureHeight","itemHeight","spaceBetweenItems"},new[]{"21104","textureWidth","thumbWidth","itemWidth"}}){int id=Int32.Parse(spec[0]);Check(spec.Skip(1).All(key=>Value(restored,id,key)==Value(baseline,id,key)),"native original properties restore for control "+id);}
   Check(NativeState().GetAttribute("listItems")=="6"&&NativeState().GetAttribute("filmstripItems")=="4","list data survives restoration");Rpc(frame);Rpc(LiveWire.Document("disconnect"));pipe.Dispose();Thread.Sleep(300);pipe=new NamedPipeClientStream(".",endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous);pipe.Connect(3000);Rpc(hello);restored=Snapshot();Check(Value(restored,21101,"label")==Value(baseline,21101,"label")&&Value(restored,21103,"textureHeight")==Value(baseline,21103,"textureHeight"),"disconnect restores real text and collection geometry");Rpc(LiveWire.Document("disconnect"));Console.WriteLine(checks+" specialized real-MP checks passed.");return 0;
  }catch(Exception ex){results.Add(ex.ToString());Console.Error.WriteLine(ex);return 1;}finally{if(pipe!=null)pipe.Dispose();File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);}
 }
}
