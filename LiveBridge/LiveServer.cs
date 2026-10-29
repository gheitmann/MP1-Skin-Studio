using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Xml;
using MP1SkinStudio.Live;

namespace MP1SkinStudio.LiveBridge
{
    public sealed class LiveServer : IDisposable
    {
        readonly ILiveSession session;readonly Func<Func<XmlDocument>,XmlDocument> dispatch;public Action<string> Diagnostic;
        Thread worker;Timer watchdog;volatile bool stopped;NamedPipeServerStream pipe;readonly object gate=new object();long activity;string endpointFile;LiveEndpoint endpoint;
        public LiveServer(ILiveSession runtime,Func<Func<XmlDocument>,XmlDocument> guiDispatch){session=runtime;dispatch=guiDispatch;}
        public LiveEndpoint Endpoint {get{return endpoint;}}
        NamedPipeServerStream CreatePipe(){return new NamedPipeServerStream(endpoint.Pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,65536,65536,LiveWire.PipePermissions());}
        public void Start()
        {
            using(var process=Process.GetCurrentProcess())endpoint=new LiveEndpoint{ProcessId=process.Id,StartTicks=process.StartTime.ToUniversalTime().Ticks,Pipe="MP1SkinStudio.Live."+process.Id+"."+Guid.NewGuid().ToString("N"),Nonce=Guid.NewGuid().ToString("N"),Name="MediaPortal live"};
            // Publish only after the OS accepted the pipe and its current-user ACL.
            pipe=CreatePipe();
            Directory.CreateDirectory(LiveWire.DirectoryPath);endpointFile=Path.Combine(LiveWire.DirectoryPath,"endpoint-"+endpoint.ProcessId+"-"+endpoint.Nonce+".xml");
            File.WriteAllText(endpointFile,endpoint.ToXml().OuterXml,new System.Text.UTF8Encoding(false));
            worker=new Thread(Listen){IsBackground=true,Name="MP1 Skin Studio live pipe"};worker.Start();
            watchdog=new Timer(s=>{if(Interlocked.Read(ref activity)>0&&DateTime.UtcNow.Ticks-Interlocked.Read(ref activity)>TimeSpan.FromSeconds(8).Ticks)ClosePipe();},null,1000,1000);
        }
        static XmlDocument Error(Exception error)
        {var doc=LiveWire.Document("error");LiveWire.Attr(doc.DocumentElement,"ok","false");LiveWire.Attr(doc.DocumentElement,"message",error.GetBaseException().Message);return doc;}
        void Listen()
        {
            while(!stopped)
            {
                NamedPipeServerStream connection=null;
                try
                {
                    connection=pipe??CreatePipe();
                    lock(gate){if(stopped){connection.Dispose();break;}pipe=connection;}connection.WaitForConnection();Interlocked.Exchange(ref activity,DateTime.UtcNow.Ticks);
                    var hello=LiveWire.Read(connection);if(hello.DocumentElement.GetAttribute("command")!="hello"||hello.DocumentElement.GetAttribute("nonce")!=endpoint.Nonce)throw new InvalidDataException("Invalid live handshake.");
                    var welcome=LiveWire.Document("hello");LiveWire.Attr(welcome.DocumentElement,"ok","true");LiveWire.Attr(welcome.DocumentElement,"pid",endpoint.ProcessId);LiveWire.Write(connection,welcome);
                    while(!stopped&&connection.IsConnected)
                    {
                        var request=LiveWire.Read(connection);Interlocked.Exchange(ref activity,DateTime.UtcNow.Ticks);
                        string command=request.DocumentElement.GetAttribute("command");
                        try
                        {
                            if(command=="disconnect"){dispatch(()=>{session.Reset();return LiveWire.Document("disconnected");});LiveWire.Write(connection,LiveWire.Document("disconnected"));break;}
                            LiveWire.Write(connection,dispatch(()=>session.Handle(request)));
                        }
                        catch(Exception ex){LiveWire.Write(connection,Error(ex));}
                    }
                }
                catch(Exception ex){if(!stopped&&!(ex is EndOfStreamException)&&!(ex is ObjectDisposedException)){if(Diagnostic!=null)Diagnostic(ex.GetType().Name+": "+ex.Message);System.Diagnostics.Trace.WriteLine("MP1 Skin Studio live: "+ex.Message);}}
                finally
                {
                    Interlocked.Exchange(ref activity,0);if(connection!=null)connection.Dispose();lock(gate){if(Object.ReferenceEquals(pipe,connection))pipe=null;}
                    try{dispatch(()=>{session.Reset();return LiveWire.Document("reset");});}catch{}
                }
            }
        }
        void ClosePipe(){lock(gate){if(pipe!=null)try{pipe.Dispose();}catch{}}}
        public void Dispose()
        {
            stopped=true;if(watchdog!=null)watchdog.Dispose();ClosePipe();
            if(endpointFile!=null)try{File.Delete(endpointFile);}catch(IOException){}catch(UnauthorizedAccessException){}
            // Stop can be called on the GUI thread. Do not join a worker which
            // may be waiting for that same GUI thread to restore its session.
        }
    }
}
