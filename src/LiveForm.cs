using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        ChoiceBox liveEndpoint=new ChoiceBox();Label liveNotice;Button liveConnect,liveDisconnect,liveMatch,liveShow;Panel liveHost;
        StudioLiveClient liveClient;LiveBinding liveBinding;bool liveVerified,liveBuilding,liveDraftInvalid,liveOpenRequested,liveOpening;int liveGeneration,liveRevision,liveAcknowledged;
        Button liveOpen;
        string liveContext,liveLastFrame,liveBridgeVersion;List<ControlEdit> liveGesture;
        readonly Timer liveDebounce=new Timer{Interval=60};
        Panel BuildLiveBar()
        {
            var host=new Panel{Dock=DockStyle.Top,Height=64,BackColor=panel,Padding=new Padding(8,0,8,0),Visible=true};liveHost=host;
            var commands=new FlowLayoutPanel{Dock=DockStyle.Top,Height=36,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(0,36),WrapContents=true};
            commands.Controls.Add(new Label{Text="LIVE",Width=38,Height=30,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(103,206,166)});
            liveEndpoint.Width=181;liveEndpoint.Height=28;liveEndpoint.Margin=new Padding(0,3,8,0);liveEndpoint.AccessibleName="Running MediaPortal live bridges";liveEndpoint.Opening+=(s,e)=>RefreshLiveEndpoints();commands.Controls.Add(liveEndpoint);
            liveConnect=ButtonFor("Connect",ConnectLive);liveConnect.Width=77;commands.Controls.Add(liveConnect);
            liveDisconnect=ButtonFor("Disconnect",DisconnectLive);liveDisconnect.Width=91;liveDisconnect.Enabled=false;commands.Controls.Add(liveDisconnect);
            liveOpen=ButtonFor("Open MP page",OpenPageFromLive);liveOpen.Width=119;liveOpen.Enabled=false;commands.Controls.Add(liveOpen);
            liveShow=ButtonFor("Show in MP",ShowPageLive);liveShow.Width=99;liveShow.Enabled=false;commands.Controls.Add(liveShow);
            liveMatch=ButtonFor("Link element",MatchLiveElement);liveMatch.Width=102;liveMatch.Enabled=false;commands.Controls.Add(liveMatch);
            liveNotice=new Label{Dock=DockStyle.Bottom,Height=24,AutoEllipsis=true,ForeColor=muted,Text="Start MediaPortal, then Connect → Open MP page → select an element and edit. Save skin copy keeps your changes."};
            host.Controls.Add(liveNotice);host.Controls.Add(commands);commands.SizeChanged+=(s,e)=>host.Height=commands.Height+26;
            tips.SetToolTip(liveConnect,"Connect to the same skin, theme and page in MediaPortal. Valid inputs and canvas drags preview live without Apply, export or reload. Skin files remain unchanged.");
            tips.SetToolTip(liveDisconnect,"Restore all preview overrides in MediaPortal and close the live connection. Editor changes remain in Studio.");
            tips.SetToolTip(liveShow,"Navigate MediaPortal to the current XML window. This is optional; ordinary live edits never reload or switch its page.");
            tips.SetToolTip(liveOpen,"Open the skin, theme and XML page currently displayed in MediaPortal. Pending inputs and unsaved projects remain protected. This does not navigate MediaPortal.");
            tips.SetToolTip(liveMatch,"Resolve an ambiguous ID by explicitly linking the selected XML element to a matching runtime control.");
            liveDebounce.Tick+=(s,e)=>{liveDebounce.Stop();SendLiveFrame();};
            canvas.LiveGesturePreview+=changes=>{liveGesture=changes;ScheduleLivePreview();};
            FormClosed+=(s,e)=>{liveDebounce.Dispose();if(liveClient!=null)liveClient.Dispose();};RefreshLiveEndpoints();return host;
        }
        void RefreshLiveEndpoints()
        {
            if(liveClient!=null)return;var selected=liveEndpoint.SelectedItem as LiveEndpoint;liveEndpoint.Items.Clear();var endpoints=LiveEndpoint.Discover();liveEndpoint.Items.AddRange(endpoints.Cast<object>());
            int index=selected==null?-1:Array.FindIndex(endpoints,e=>e.Pipe==selected.Pipe);liveEndpoint.SelectedIndex=index>=0?index:endpoints.Length>0?0:-1;
        }
        void LiveStatus(string text,bool error=false,bool ready=false)
        {if(liveNotice==null)return;if(ready&&!String.IsNullOrEmpty(liveBridgeVersion))text+=" · bridge "+liveBridgeVersion;liveNotice.Text=text;liveNotice.ForeColor=error?Color.FromArgb(245,186,105):ready?Color.FromArgb(103,206,166):muted;tips.SetToolTip(liveNotice,text);}
        void LiveOnUi(System.Action action)
        {if(IsDisposed||Disposing||!IsHandleCreated)return;try{BeginInvoke(new System.Action(()=>{if(!IsDisposed&&!Disposing)action();}));}catch(InvalidOperationException){}}
        string LiveContext(){return project==null?"":project.Root+"\n"+project.Theme+"\n"+project.Page;}
        void ConnectLive()
        {
            if(liveClient!=null)return;RefreshLiveEndpoints();var endpoint=liveEndpoint.SelectedItem as LiveEndpoint;
            if(endpoint==null){LiveStatus("No running bridge found. Install/enable MP1 Skin Studio Live in MediaPortal and start it once.",true);return;}
            liveGeneration++;liveBinding=null;liveVerified=false;liveBuilding=false;liveContext=LiveContext();liveLastFrame=null;liveBridgeVersion=null;liveRevision=liveAcknowledged=0;
            var client=new StudioLiveClient(endpoint);liveClient=client;
            client.Response+=response=>LiveOnUi(()=>{if(Object.ReferenceEquals(liveClient,client))ReceiveLive(response);});
            client.Disconnected+=reason=>LiveOnUi(()=>{if(!Object.ReferenceEquals(liveClient,client))return;liveClient=null;liveBinding=null;liveVerified=false;liveBuilding=false;liveOpenRequested=false;liveGeneration++;liveConnect.Enabled=liveEndpoint.Enabled=true;liveDisconnect.Enabled=liveShow.Enabled=liveMatch.Enabled=liveOpen.Enabled=false;liveDebounce.Stop();LiveStatus(reason+" · Connect to resume live editing.",true);});
            liveConnect.Enabled=liveEndpoint.Enabled=false;liveDisconnect.Enabled=liveShow.Enabled=true;LiveStatus("Connecting to MediaPortal…");client.Start();
        }
        void DisconnectLive()
        {
            if(liveClient==null)return;liveOpenRequested=false;liveVerified=false;liveBinding=null;liveBuilding=false;liveGesture=null;liveGeneration++;liveDebounce.Stop();LiveStatus("Disconnecting · restoring MediaPortal preview…");liveClient.Dispose();
        }
        void ReceiveLive(XmlDocument response)
        {
            var root=response.DocumentElement;string command=root.GetAttribute("command");
            if(root.GetAttribute("ok")=="false"){if(command=="snapshot")liveOpenRequested=false;LiveStatus("Not sent: "+root.GetAttribute("message"),true);return;}
            if(command=="snapshot")
            {
                liveOpen.Enabled=true;liveBridgeVersion=root.GetAttribute("bridgeVersion");if(liveBridgeVersion.Length>32)liveBridgeVersion=liveBridgeVersion.Substring(0,32);
                if(liveOpenRequested)
                {
                    liveOpenRequested=false;
                    try{OpenLiveSnapshot(response);}catch(Exception ex){LiveStatus("Cannot open MP page: "+ex.GetBaseException().Message,true);return;}
                }
                PrepareLiveBinding(response);return;
            }
            if(command=="verify"){liveVerified=true;liveMatch.Enabled=true;liveLastFrame=null;ScheduleLivePreview();return;}
            if(command=="frame")
            {
                int revision;if(Int32.TryParse(root.GetAttribute("requestRevision"),out revision))liveAcknowledged=Math.Max(liveAcknowledged,revision);
                if(!liveDraftInvalid&&liveAcknowledged==liveRevision)LiveStatus("Live · "+(liveBinding==null?0:liveBinding.Matches.Count)+" XML elements linked · "+project.Page+" · "+root.GetAttribute("roundTripMs")+" ms acknowledgement",false,true);
            }
        }
        void PrepareLiveBinding(XmlDocument snapshot)
        {
            if(project==null||liveClient==null)return;int generation=++liveGeneration;liveVerified=false;liveBuilding=true;liveMatch.Enabled=false;liveGesture=null;
            var editor=project;string root=project.Root,themeName=project.Theme,pageName=project.Page;liveContext=LiveContext();LiveStatus("Checking live skin/page and linking runtime controls…");
            Task.Factory.StartNew(()=>{var baseline=new SkinProject(root){Theme=themeName};baseline.OpenPage(pageName);return new LiveBinding(baseline,snapshot);}).ContinueWith(task=>
            {
                var failure=task.IsFaulted?task.Exception.GetBaseException():null;
                LiveOnUi(()=>
                {
                if(generation!=liveGeneration||!Object.ReferenceEquals(project,editor)||liveClient==null)return;liveBuilding=false;
                if(failure!=null){liveBinding=null;LiveStatus(failure.Message+" Click Open MP page to edit what MP is showing, or Show in MP to display this editor page.",true);return;}
                try{liveBinding=task.Result;liveClient.Send(liveBinding.Verify(project));}catch(Exception ex){liveBinding=null;LiveStatus(ex.Message,true);}
                });
            });
        }
        void ScheduleLivePreview()
        {
            if(liveClient==null||liveOpening||liveOpenRequested)return;
            if(liveContext!=LiveContext())
            {
                liveContext=LiveContext();liveGeneration++;liveVerified=false;liveBuilding=false;liveBinding=null;liveLastFrame=null;liveGesture=null;liveClient.ClearFrames();
                liveClient.Send(LiveWire.Document("reset"));liveClient.Send(LiveWire.Document("snapshot"));LiveStatus("Live page context changed · checking MediaPortal…");return;
            }
            if(liveVerified&&!liveBuilding&&!liveDebounce.Enabled)liveDebounce.Start();
        }
        Dictionary<string,Dictionary<string,string>> LiveDrafts()
        {
            var drafts=new Dictionary<string,Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            if(unlock.Checked)drafts["@bindings-approved"]=new Dictionary<string,string>();
            foreach(var target in SelectedControls())
            {
                var values=new Dictionary<string,string>();
                foreach(var field in fields)
                {
                    string previous;if(!originals.TryGetValue(field.Key,out previous)||(field.Value.Text==previous&&!(mixedFields.Contains(field.Key)&&touchedFields.Contains(field.Key))))continue;
                    if(project.IsPropertyLocked(target,field.Key))throw new InvalidOperationException("This live property is locked: "+field.Key);
                    if(target.IsBound(field.Key)&&!unlock.Checked)throw new InvalidOperationException("Binding protected: enable Replace bindings locally.");
                    string value=field.Value.Text;
                    if(new[]{"posX","posY","width","height","textXOff","textYOff"}.Contains(field.Key)){int number;if(!Int32.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out number)||(field.Key=="width"||field.Key=="height")&&number<1)throw new InvalidOperationException("Complete the numeric value for "+field.Key+" before previewing it.");}
                    if(field.Key=="font"&&!project.FontNames.Contains(value))throw new InvalidOperationException("Choose a font from this skin before previewing it.");
                    values[field.Key]=value;
                }
                if(values.Count>0)drafts[target.Key]=values;
            }
            if(liveGesture!=null)foreach(var edit in liveGesture){Dictionary<string,string> values;if(!drafts.TryGetValue(edit.Control.Key,out values))drafts[edit.Control.Key]=values=new Dictionary<string,string>();foreach(var pair in edit.Values)values[pair.Key]=pair.Value;}
            return drafts;
        }
        void SendLiveFrame()
        {
            if(liveClient==null||!liveVerified||liveBinding==null)return;
            try
            {
                var frame=liveBinding.Frame(project,LiveDrafts(),0);string signature=frame.OuterXml;liveDraftInvalid=false;
                if(signature==liveLastFrame){if(liveAcknowledged==liveRevision)LiveStatus("Live · "+liveBinding.Matches.Count+" XML elements linked · "+project.Page,false,true);return;}
                liveLastFrame=signature;LiveWire.Attr(frame.DocumentElement,"revision",++liveRevision);liveClient.Send(frame);LiveStatus("Live · updating MediaPortal…");
            }
            catch(Exception ex){liveDraftInvalid=true;LiveStatus("Not sent: "+ex.GetBaseException().Message,true);}
        }
        void ShowPageLive()
        {
            if(liveClient==null||project==null)return;var doc=LiveWire.Document("activate");LiveWire.Attr(doc.DocumentElement,"window",project.WindowId);liveClient.ClearFrames();liveVerified=false;liveClient.Send(doc);liveClient.Send(LiveWire.Document("snapshot"));
        }
        void OpenPageFromLive()
        {
            if(liveClient==null||liveOpenRequested)return;
            if(PendingProperties()){ShowNotice("Apply pending property edits or restore their previous values before opening the MP page.");return;}
            liveOpenRequested=true;liveVerified=false;liveGeneration++;liveBuilding=false;liveDebounce.Stop();liveClient.ClearFrames();
            liveClient.Send(LiveWire.Document("reset"));liveClient.Send(LiveWire.Document("snapshot"));LiveStatus("Opening the page currently displayed in MediaPortal…");
        }
        void OpenLiveSnapshot(XmlDocument snapshot)
        {
            var root=snapshot.DocumentElement;string skin=Path.GetFullPath(LiveWire.Required(root,"skin")),page=Path.GetFileName(LiveWire.Required(root,"page")),themeName=LiveWire.ThemeKey(root.GetAttribute("theme"));
            var next=new SkinProject(skin){Theme=themeName};
            if(!File.Exists(next.Resolve(page)))throw new FileNotFoundException("This MP page has no readable skin XML: "+page);
            if(project!=null&&String.Equals(project.Root.TrimEnd('\\','/'),skin.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase)&&String.Equals(project.Page,page,StringComparison.OrdinalIgnoreCase)&&String.Equals(LiveWire.ThemeKey(project.Theme),themeName,StringComparison.OrdinalIgnoreCase))return;
            // Recheck after the asynchronous snapshot: inputs may have changed while MP replied.
            if(!CanSwitch())throw new InvalidOperationException("The current editor session was retained.");
            liveOpening=true;try{InstallProject(next,page,null);liveContext=LiveContext();}finally{liveOpening=false;}
        }
        void MatchLiveElement()
        {
            if(liveBinding==null||selected==null||SelectedControls().Length!=1)return;var menu=StudioMenus.For(this,"live-match");var matches=liveBinding.Items.Where(item=>item.Kind.Equals(selected.Get("type"),StringComparison.OrdinalIgnoreCase)).ToArray();
            menu.Items.Add("Link XML: "+selected.Name).Enabled=false;
            foreach(var item in matches.OrderBy(item=>item.Id==SkinProject.ControlId(selected)?0:1).ThenBy(item=>item.Description).Take(80))
            {var target=item;menu.Items.Add(item.ToString().Replace("&","&&"),null,(s,e)=>Attempt(()=>{liveBinding.Link(selected,target);liveLastFrame=null;ScheduleLivePreview();}));}
            if(matches.Length==0)menu.Items.Add("No matching control type is currently loaded in MP").Enabled=false;menu.Show(liveMatch,new Point(0,liveMatch.Height));
        }
    }
}
