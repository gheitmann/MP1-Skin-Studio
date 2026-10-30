using System;
using System.Reflection;
using System.Threading;
using System.Xml;
using MediaPortal.Common.Utils;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;

[assembly: AssemblyTitle("MP1 Skin Studio Live Bridge")]
[assembly: AssemblyDescription("Local live skin editing bridge for MP1 Skin Studio")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: CompatibleVersion("1.39.0.0")]
namespace MP1SkinStudio.LiveBridge
{
    public sealed class StudioLivePlugin : IPlugin,ISetupForm
    {
        LiveServer server;RuntimeSession session;
        public string PluginName(){return "MP1 Skin Studio Live";}
        public string Description(){return "Live skin editing from MP1 Skin Studio on this Windows user account. Preview changes are restored when disconnected.";}
        public string Author(){return "MP1 Skin Studio contributors";}
        public bool CanEnable(){return true;}
        public bool DefaultEnabled(){return true;}
        public bool HasSetup(){return false;}
        public void ShowPlugin(){}
        public int GetWindowId(){return 0;}
        public bool GetHome(out string name,out string image,out string focus,out string picture){name=PluginName();image=focus=picture="";return false;}
        public void Start()
        {
            try{session=new RuntimeSession();server=new LiveServer(session,Dispatch);server.Diagnostic=message=>Log.Warn("MP1 Skin Studio Live: {0}",message);server.Start();GUIWindowManager.OnDeActivateWindow+=LeavingWindow;Log.Info("MP1 Skin Studio Live: bridge ready, PID {0}; no skin files are modified.",System.Diagnostics.Process.GetCurrentProcess().Id);}
            catch(Exception ex){Log.Error("MP1 Skin Studio Live: could not start bridge: {0}",ex.Message);if(server!=null)server.Dispose();server=null;}
        }
        sealed class Ticket {public int State;public bool Abandoned;public XmlDocument Result;public Exception Error;}
        XmlDocument Dispatch(Func<XmlDocument> action)
        {
            var form=GUIGraphicsContext.form;if(form==null||form.IsDisposed||!form.IsHandleCreated)throw new InvalidOperationException("MediaPortal GUI is not ready.");
            if(!form.InvokeRequired){lock(GUIGraphicsContext.RenderLock)return action();}
            var ticket=new Ticket();
            form.BeginInvoke(new System.Action(()=>
            {
                lock(ticket){if(ticket.State!=0)return;ticket.State=1;}
                lock(GUIGraphicsContext.RenderLock)
                {
                    try{ticket.Result=action();}catch(Exception ex){ticket.Error=ex;}
                    lock(ticket){if(ticket.Abandoned&&session!=null)session.Reset();ticket.State=2;Monitor.PulseAll(ticket);}
                }
            }));
            lock(ticket)
            {
                var deadline=DateTime.UtcNow.AddSeconds(3);
                while(ticket.State!=2&&DateTime.UtcNow<deadline)Monitor.Wait(ticket,Math.Max(1,(int)(deadline-DateTime.UtcNow).TotalMilliseconds));
                if(ticket.State==0){ticket.State=3;throw new TimeoutException("MediaPortal GUI is busy; this live operation was cancelled before execution.");}
                deadline=DateTime.UtcNow.AddSeconds(5);
                while(ticket.State!=2&&DateTime.UtcNow<deadline)Monitor.Wait(ticket,Math.Max(1,(int)(deadline-DateTime.UtcNow).TotalMilliseconds));
                if(ticket.State!=2){ticket.Abandoned=true;throw new TimeoutException("MediaPortal is still processing a live operation. The abandoned preview will be restored.");}
                if(ticket.Error!=null)throw ticket.Error;return ticket.Result;
            }
        }
        public void Stop()
        {
            GUIWindowManager.OnDeActivateWindow-=LeavingWindow;
            if(server!=null)server.Dispose();server=null;
            if(session!=null)try{Dispatch(()=>{session.Reset();return LiveWire.Document("reset");});}catch(Exception ex){Log.Warn("MP1 Skin Studio Live: shutdown restore: {0}",ex.Message);}session=null;
        }
        void LeavingWindow(int id)
        {if(session!=null)try{lock(GUIGraphicsContext.RenderLock)session.Reset();}catch(Exception ex){Log.Warn("MP1 Skin Studio Live: leaving-window restore: {0}",ex.Message);}}
    }
}
