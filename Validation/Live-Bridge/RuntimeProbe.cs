using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Xml;
using MP1SkinStudio.Live;
class RuntimeProbe
{
 static int checks; static NamedPipeClientStream pipe;
 static XmlDocument Rpc(XmlDocument request){LiveWire.Write(pipe,request);var response=LiveWire.Read(pipe);if(response.DocumentElement.GetAttribute("ok")=="false")throw new Exception(response.OuterXml);return response;}
 static void Check(bool value,string name){if(!value)throw new Exception("FAIL "+name);Console.WriteLine("PASS "+name);checks++;}
 static XmlDocument Snapshot(){return Rpc(LiveWire.Document("snapshot"));}
 static XmlElement Item(XmlDocument doc,int id){return (XmlElement)doc.SelectSingleNode("/live/control[@id='"+id+"']");}
 static string Value(XmlElement item,string key){return item.SelectSingleNode("value[@key='"+key+"']").InnerText;}
 static XmlDocument Frame(string epoch,string token,params string[] fields){var doc=LiveWire.Document("frame");LiveWire.Attr(doc.DocumentElement,"epoch",epoch);var node=LiveWire.Add(doc.DocumentElement,"control");LiveWire.Attr(node,"token",token);for(int i=0;i<fields.Length;i+=2){var set=LiveWire.Add(node,"set");LiveWire.Attr(set,"key",fields[i]);set.InnerText=fields[i+1];}return doc;}
 static int Main(string[] args)
 {
  try
  {
   Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR",args[0]);var endpoint=LiveEndpoint.Discover().Single();pipe=new NamedPipeClientStream(".",endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous);pipe.Connect(3000);
   var hello=LiveWire.Document("hello");LiveWire.Attr(hello.DocumentElement,"nonce",endpoint.Nonce);Check(Rpc(hello).DocumentElement.GetAttribute("pid")==endpoint.ProcessId.ToString(),"handshake matches live MP PID");
   var activate=LiveWire.Document("activate");LiveWire.Attr(activate.DocumentElement,"window",0);Rpc(activate);Thread.Sleep(500);
   var snapshot=Snapshot();snapshot.Save(Path.Combine(args[0],"probe-snapshot.xml"));Console.WriteLine("PAGE "+snapshot.DocumentElement.GetAttribute("page")+" CONTROLS "+snapshot.SelectNodes("/live/control").Count);
   var item=Item(snapshot,21001);Check(item!=null,"real test label captured");string epoch=snapshot.DocumentElement.GetAttribute("epoch"),token=item.GetAttribute("token"),original=Value(item,"label"),x=Value(item,"posX");
   Rpc(Frame(epoch,token,"label","Live from Skin Studio","posX","240","textcolor","ffffcc00"));
   var changed=Item(Snapshot(),21001);Check(Value(changed,"label")=="Live from Skin Studio","native text changed without reload");Check(Value(changed,"posX")=="240","native position changed without reload");Check(Value(changed,"textcolor")=="ffffcc00","native color changed without reload");Check(Snapshot().DocumentElement.GetAttribute("epoch")==epoch,"same window and runtime control identities");
   if(args.Length>1&&args[1]=="hold"){Console.WriteLine("HOLDING live visual for 25 seconds");for(int i=0;i<25;i++){Rpc(LiveWire.Document("ping"));Thread.Sleep(1000);}}
   var invalid=Frame(epoch,token,"label","Must not commit","width","-5");LiveWire.Write(pipe,invalid);Check(LiveWire.Read(pipe).DocumentElement.GetAttribute("ok")=="false","invalid frame rejected");Check(Value(Item(Snapshot(),21001),"label")=="Live from Skin Studio","invalid frame preserves last valid text");
   var empty=LiveWire.Document("frame");LiveWire.Attr(empty.DocumentElement,"epoch",epoch);Rpc(empty);Check(Value(Item(Snapshot(),21001),"label")==original&&Value(Item(Snapshot(),21001),"posX")==x,"empty frame restores exact baseline");
   Rpc(Frame(epoch,token,"label","Disconnect test"));Rpc(LiveWire.Document("disconnect"));pipe.Dispose();Thread.Sleep(200);
   pipe=new NamedPipeClientStream(".",endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous);pipe.Connect(3000);Rpc(hello);Check(Value(Item(Snapshot(),21001),"label")==original,"disconnect restores native label");Rpc(LiveWire.Document("disconnect"));Console.WriteLine(checks+" live runtime checks passed.");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{if(pipe!=null)pipe.Dispose();}
 }
}
