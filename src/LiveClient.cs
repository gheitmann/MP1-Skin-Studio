using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Xml;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public sealed class StudioLiveClient : IDisposable
    {
        readonly LiveEndpoint endpoint;readonly object gate=new object();readonly Queue<string> commands=new Queue<string>();readonly AutoResetEvent signal=new AutoResetEvent(false);
        volatile bool stopping;bool signalDisposed;string frame;NamedPipeClientStream pipe;Thread worker;System.Threading.Timer watchdog;long waiting;
        public event System.Action<XmlDocument> Response;public event System.Action<string> Disconnected;
        public StudioLiveClient(LiveEndpoint selected){endpoint=selected;}
        public void Start(){worker=new Thread(Run){IsBackground=true,Name="Studio live link"};worker.Start();}
        public void ClearFrames(){lock(gate)frame=null;}
        public void Send(XmlDocument command){lock(gate){if(stopping)return;if(command.DocumentElement.GetAttribute("command")=="frame")frame=command.OuterXml;else{if(commands.Count>=16)throw new InvalidOperationException("Live connection is busy.");commands.Enqueue(command.OuterXml);}if(!signalDisposed)signal.Set();}}
        XmlDocument Exchange(XmlDocument request)
        {
            Interlocked.Exchange(ref waiting,DateTime.UtcNow.Ticks);var clock=Stopwatch.StartNew();
            try{LiveWire.Write(pipe,request);var response=LiveWire.Read(pipe);LiveWire.Attr(response.DocumentElement,"requestRevision",request.DocumentElement.GetAttribute("revision"));LiveWire.Attr(response.DocumentElement,"roundTripMs",clock.ElapsedMilliseconds);return response;}
            finally{Interlocked.Exchange(ref waiting,0);}
        }
        void Notify(XmlDocument response){var handler=Response;if(handler!=null)handler(response);}
        void Run()
        {
            string reason="Disconnected; MediaPortal preview restored.";
            try
            {
                pipe=new NamedPipeClientStream(".",endpoint.Pipe,PipeDirection.InOut,PipeOptions.Asynchronous);pipe.Connect(1500);
                watchdog=new System.Threading.Timer(s=>{long time=Interlocked.Read(ref waiting);if(time>0&&DateTime.UtcNow.Ticks-time>TimeSpan.FromSeconds(5).Ticks)try{pipe.Dispose();}catch{}},null,250,250);
                var hello=LiveWire.Document("hello");LiveWire.Attr(hello.DocumentElement,"nonce",endpoint.Nonce);var welcome=Exchange(hello);if(welcome.DocumentElement.GetAttribute("command")!="hello"||welcome.DocumentElement.GetAttribute("pid")!=endpoint.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture))throw new InvalidDataException("Unexpected live bridge handshake.");
                Notify(Exchange(LiveWire.Document("snapshot")));long lastPing=Stopwatch.GetTimestamp();
                while(!stopping)
                {
                    string next=null;lock(gate){if(commands.Count>0)next=commands.Dequeue();else if(frame!=null){next=frame;frame=null;}}
                    if(next!=null)
                    {
                        var request=LiveWire.Parse(new System.Text.UTF8Encoding(false,true).GetBytes(next));var response=Exchange(request);Notify(response);lastPing=Stopwatch.GetTimestamp();
                    }
                    else if((Stopwatch.GetTimestamp()-lastPing)/(double)Stopwatch.Frequency>.75)
                    {
                        var response=Exchange(LiveWire.Document("ping"));if(response.DocumentElement.GetAttribute("current")=="false")Notify(Exchange(LiveWire.Document("snapshot")));lastPing=Stopwatch.GetTimestamp();
                    }
                    else signal.WaitOne(40);
                }
                if(pipe.IsConnected)Exchange(LiveWire.Document("disconnect"));
            }
            catch(Exception ex){reason="Live disconnected: "+ex.GetBaseException().Message;}
            finally
            {
                if(watchdog!=null)watchdog.Dispose();if(pipe!=null)pipe.Dispose();lock(gate){stopping=true;signalDisposed=true;signal.Dispose();}
                var handler=Disconnected;if(handler!=null)handler(reason);
            }
        }
        public void Dispose(){lock(gate){stopping=true;if(!signalDisposed)signal.Set();if(worker==null&&!signalDisposed){signal.Dispose();signalDisposed=true;}}}
    }
}
