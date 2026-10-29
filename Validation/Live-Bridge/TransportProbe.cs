using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Xml;
using MP1SkinStudio.Live;
using MP1SkinStudio.LiveBridge;
class TransportProbe
{
 class Session:ILiveSession{public int Resets;public XmlDocument Handle(XmlDocument request){return LiveWire.Document("pong");}public void Reset(){Interlocked.Increment(ref Resets);}}
 static int Main(string[] args){try{Environment.SetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR",args[0]);var session=new Session();using(var server=new LiveServer(session,f=>f())){server.Diagnostic=Console.WriteLine;server.Start();Console.WriteLine("SERVER "+server.Endpoint.Pipe);using(var client=new NamedPipeClientStream(".",server.Endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous)){client.Connect(3000);Console.WriteLine("CONNECTED");var hello=LiveWire.Document("hello");LiveWire.Attr(hello.DocumentElement,"nonce",server.Endpoint.Nonce);LiveWire.Write(client,hello);Console.WriteLine(LiveWire.Read(client).OuterXml);LiveWire.Write(client,LiveWire.Document("ping"));if(LiveWire.Read(client).DocumentElement.GetAttribute("command")!="pong")throw new Exception("Bad response");}for(int i=0;i<20&&session.Resets==0;i++)Thread.Sleep(20);if(session.Resets==0)throw new Exception("No crash reset");Console.WriteLine("PASS authenticated pipe, command dispatch and crash restore");}return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
