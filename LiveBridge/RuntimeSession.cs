using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;

namespace MP1SkinStudio.LiveBridge
{
    public sealed class RuntimeSession : ILiveSession
    {
        sealed class Entry
        {public string Token,Parent,Path;public GUIControl Control;public GUIControlCollection Owner;public bool Automatic;public Dictionary<string,RuntimeProperty> Slots;public object[] AnimationOriginal;}
        static readonly System.Reflection.FieldInfo[] animationFields=new[]{"_originalRectangle","_originalDiffuseColor","_isAnimating"}.Select(n=>typeof(GUIControl).GetField(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)).Where(f=>f!=null).ToArray();
        static object[] Animation(GUIControl control){return animationFields.Select(f=>f.GetValue(control)).ToArray();}
        static void RestoreAnimation(GUIControl control,object[] values){if(values!=null)for(int i=0;i<animationFields.Length;i++)animationFields[i].SetValue(control,values[i]);}
        sealed class Saved {public RuntimeProperty Slot;public object Native;}
        sealed class Change {public string Name,Text;public Entry Entry;public RuntimeProperty Slot;public object Native;}
        readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>(StringComparer.Ordinal),created=new Dictionary<string,Entry>(StringComparer.Ordinal);
        readonly Dictionary<string,Saved> originals=new Dictionary<string,Saved>(StringComparer.Ordinal);
        readonly Dictionary<string,string> applied=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly HashSet<string> detached=new HashSet<string>(StringComparer.Ordinal),placed=new HashSet<string>(StringComparer.Ordinal);
        Dictionary<GUIControl,GUIControl> originalParents;Dictionary<GUIControlCollection,object[]> expectedTree;
        Dictionary<GUIControlCollection,object[]> originalTree;GUIWindow window;string epoch,skin,theme;
        readonly Func<GUIWindow> currentWindow;
        public RuntimeSession():this(()=>GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindowEx)){}
        public RuntimeSession(Func<GUIWindow> provider){currentWindow=provider;}
        string CurrentSkin {get{return Path.GetFullPath(GUIGraphicsContext.Skin).TrimEnd('\\','/');}}
        bool SameWindow {get{return window!=null&&Object.ReferenceEquals(window,currentWindow())&&String.Equals(skin,CurrentSkin,StringComparison.OrdinalIgnoreCase)&&String.Equals(theme,GUIGraphicsContext.ThemeName,StringComparison.OrdinalIgnoreCase)&&TreeMatches();}}
        bool TreeMatches(){return expectedTree==null||expectedTree.All(pair=>pair.Key.Count==pair.Value.Length&&pair.Key.Cast<object>().Zip(pair.Value,(a,b)=>Object.ReferenceEquals(a,b)).All(same=>same));}
        static XmlDocument Response(string command){var doc=LiveWire.Document(command);LiveWire.Attr(doc.DocumentElement,"ok","true");return doc;}
        public XmlDocument Handle(XmlDocument request)
        {
            string command=LiveWire.Required(request.DocumentElement,"command",20);
            if(command=="snapshot")return Snapshot();
            if(command=="activate")
            {int id=LiveWire.Int(request.DocumentElement,"window",0,Int32.MaxValue);if(GUIWindowManager.GetWindow(id)==null)throw new InvalidOperationException("This page is not registered by the running MediaPortal installation.");Reset();GUIWindowManager.ActivateWindow(id);return Response("activate");}
            if(command=="reset"){Reset();return Response("reset");}
            if(command=="ping")
            {var result=Response("ping");LiveWire.Attr(result.DocumentElement,"epoch",epoch??"");LiveWire.Attr(result.DocumentElement,"current",SameWindow?"true":"false");if(!SameWindow)Reset();return result;}
            if(command=="frame")return Apply(request.DocumentElement);
            if(command=="verify")return Verify(request.DocumentElement);
            throw new InvalidDataException("Unsupported live command.");
        }
        void EnsureWindow()
        {
            if(SameWindow&&entries.Count>0)return;Reset();window=currentWindow();if(window==null||!window.WindowLoaded)throw new InvalidOperationException("Open a skin page in MediaPortal first.");
            skin=CurrentSkin;theme=GUIGraphicsContext.ThemeName;epoch=Guid.NewGuid().ToString("N");
            var seen=new HashSet<GUIControl>();Capture(window.Children,"","",false,seen);originalTree=CaptureTree();expectedTree=CaptureTree();originalParents=CaptureParents();
        }
        void Capture(GUIControlCollection collection,string parent,string path,bool automatic,HashSet<GUIControl> seen)
        {
            int index=0;foreach(GUIControl control in collection.Cast<GUIControl>().ToArray())
            {CaptureOne(control,collection,parent,path+"/"+index,automatic,seen);index++;}
        }
        void CaptureOne(GUIControl control,GUIControlCollection owner,string parent,string path,bool automatic,HashSet<GUIControl> seen)
        {
            if(control==null||!seen.Add(control))return;if(entries.Count>=4096)throw new InvalidDataException("More than 4,096 runtime controls.");
            var entry=new Entry{Token=Guid.NewGuid().ToString("N"),Control=control,Owner=owner,Parent=parent,Path=path,Automatic=automatic,Slots=RuntimeProperty.For(control,skin)};entries.Add(entry.Token,entry);
            var group=control as GUIGroup;if(group!=null)Capture((GUIControlCollection)group.Children,entry.Token,path,automatic||group.Layout!=null,seen);
            var facade=control as GUIFacadeControl;if(facade!=null)
            {
                foreach(string name in new[]{"ListLayout","PlayListLayout","AlbumListLayout","FilmstripLayout","CoverFlowLayout","ThumbnailLayout"})
                {var property=facade.GetType().GetProperty(name);var child=property==null?null:property.GetValue(facade,null) as GUIControl;if(child!=null)CaptureOne(child,null,entry.Token,path+"/"+name,automatic,seen);}
            }
        }
        XmlDocument Snapshot()
        {
            EnsureWindow();var doc=Response("snapshot");var root=doc.DocumentElement;
            LiveWire.Attr(root,"epoch",epoch);LiveWire.Attr(root,"skin",skin);LiveWire.Attr(root,"theme",theme);LiveWire.Attr(root,"page",window.WindowXmlFileName);LiveWire.Attr(root,"window",window.GetID);
            LiveWire.Attr(root,"width",GUIGraphicsContext.SkinSize.Width);LiveWire.Attr(root,"height",GUIGraphicsContext.SkinSize.Height);
            LiveWire.Attr(root,"mpVersion",typeof(GUIControl).Assembly.GetName().Version);LiveWire.Attr(root,"bridgeVersion","1.0.0");
            foreach(var entry in entries.Values)
            {
                var node=LiveWire.Add(root,"control");LiveWire.Attr(node,"token",entry.Token);LiveWire.Attr(node,"parent",entry.Parent);LiveWire.Attr(node,"path",entry.Path);LiveWire.Attr(node,"type",entry.Control.Type);LiveWire.Attr(node,"id",entry.Control.GetID);LiveWire.Attr(node,"description",entry.Control.Description);LiveWire.Attr(node,"automatic",entry.Automatic?"true":"false");
                foreach(var slot in entry.Slots)
                {
                    try{var value=LiveWire.Add(node,"value");LiveWire.Attr(value,"key",slot.Key);LiveWire.Attr(value,"writable",entry.Automatic&&(slot.Key=="posX"||slot.Key=="posY")?"false":"true");value.InnerText=slot.Value.Value??"";}catch{throw new InvalidOperationException("Cannot inspect runtime property "+entry.Control.Description+" · "+slot.Key);}
                }
            }
            return doc;
        }
        XmlDocument Verify(XmlElement request)
        {
            if(!SameWindow||request.GetAttribute("epoch")!=epoch)throw new InvalidOperationException("MediaPortal changed page or skin. Reconnect the live session.");
            if(!String.Equals(Path.GetFullPath(LiveWire.Required(request,"skin")),skin,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The editor and MediaPortal must use the same skin folder.");
            if(!String.Equals(LiveWire.ThemeKey(request.GetAttribute("theme")),LiveWire.ThemeKey(theme),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The editor and MediaPortal must use the same theme.");
            var files=request.SelectNodes("file");if(files.Count>256)throw new InvalidDataException("Too many live source files.");
            long total=0;using(var sha=SHA256.Create())foreach(XmlElement file in files)
            {
                string path=LiveWire.SafeRelative(skin,LiveWire.Required(file,"path",500));if(!path.EndsWith(".xml",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Only XML sources may be verified.");
                var info=new FileInfo(path);total+=info.Length;if(total>64*1024*1024||info.Length>8*1024*1024)throw new InvalidDataException("Live source verification exceeds its size limit.");
                string digest;using(var input=File.OpenRead(path))digest=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();
                if(!String.Equals(digest,file.GetAttribute("sha256"),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Skin source differs from the editor baseline: "+file.GetAttribute("path"));
            }
            return Response("verify");
        }
        Entry Find(string token)
        {Entry entry;if(!entries.TryGetValue(token,out entry)&&!created.TryGetValue(token,out entry))throw new InvalidDataException("Unknown or stale runtime control.");return entry;}
        static bool Geometry(string key){return key=="posX"||key=="posY"||key=="width"||key=="height";}
        XmlDocument Apply(XmlElement request)
        {
            if(!SameWindow||request.GetAttribute("epoch")!=epoch){Reset();throw new InvalidOperationException("MediaPortal changed page or skin. Reconnect the live session.");}
            if(request.ChildNodes.Count>4096)throw new InvalidDataException("Too many live operations.");
            if(request.ChildNodes.OfType<XmlElement>().Any(n=>!new[]{"control","create","remove","place"}.Contains(n.Name)))throw new InvalidDataException("Unknown live operation.");
            var changes=new List<Change>();var names=new HashSet<string>(StringComparer.Ordinal);var desiredCreated=new HashSet<string>(StringComparer.Ordinal);
            var treeBefore=CaptureTree();var parentsBefore=CaptureParents();var animationBefore=entries.Values.Concat(created.Values).ToDictionary(e=>e.Control,e=>Animation(e.Control));var detachedBefore=detached.ToArray();var placedBefore=placed.ToArray();var existingCreated=created.Keys.ToArray();var originalBefore=originals.ToArray();var appliedBefore=applied.ToArray();var saved=new Dictionary<RuntimeProperty,object>();
            try
            {
                foreach(XmlElement add in request.SelectNodes("create"))
                {
                    string token=LiveWire.Required(add,"token",100);if(!token.StartsWith("new:",StringComparison.Ordinal)||!desiredCreated.Add(token))throw new InvalidDataException("Duplicate or invalid inserted control.");
                    Entry entry;if(!created.TryGetValue(token,out entry)){entry=Create(add,token);created.Add(token,entry);}
                    foreach(XmlElement field in add.SelectSingleNode("control").ChildNodes.OfType<XmlElement>().Where(n=>LiveWire.Fields.Contains(n.Name)))AddChange(changes,names,entry,field.Name,field.InnerText);
                }
                foreach(XmlElement node in request.SelectNodes("control"))
                {var entry=Find(LiveWire.Required(node,"token",100));foreach(XmlElement value in node.SelectNodes("set"))AddChange(changes,names,entry,LiveWire.Required(value,"key",64),value.InnerText);}
                var removed=new HashSet<string>(StringComparer.Ordinal);
                foreach(XmlElement remove in request.SelectNodes("remove")){var entry=Find(LiveWire.Required(remove,"token",100));if(!removed.Add(entry.Token)||entry.Owner==null)throw new InvalidDataException("This runtime control cannot be removed independently.");}
                // Parse and validate all placements before changing any collection.
                var places=request.SelectNodes("place").Cast<XmlElement>().ToArray();var desiredParents=new Dictionary<string,string>(StringComparer.Ordinal);
                foreach(var place in places){string token=LiveWire.Required(place,"token",100),parent=place.GetAttribute("parent");Find(token);if(parent.Length>0&&!(Find(parent).Control is GUIGroup))throw new InvalidDataException("A live parent must be a group.");if(desiredParents.ContainsKey(token))throw new InvalidDataException("Duplicate live placement.");desiredParents[token]=parent;LiveWire.Int(place,"index",0,4096);}
                foreach(string token in desiredParents.Keys){var seen=new HashSet<string>();string parent=token;while(parent.Length>0){if(!seen.Add(parent))throw new InvalidDataException("Cyclic live group placement.");string next;parent=desiredParents.TryGetValue(parent,out next)?next:ActualParent(Find(parent));if(seen.Count>64)throw new InvalidDataException("Live group nesting exceeds 64.");}}
                foreach(var entry in entries.Values.Concat(created.Values))if(entry.AnimationOriginal==null)entry.AnimationOriginal=Animation(entry.Control);
                var storedControls=changes.Select(c=>c.Entry.Control).Concat(entries.Values.Where(e=>originals.Keys.Any(k=>k.StartsWith(e.Token+"/",StringComparison.Ordinal))).Select(e=>e.Control)).Distinct().ToArray();
                foreach(var pair in originals)if(!names.Contains(pair.Key)){saved[pair.Value.Slot]=pair.Value.Slot.Read();pair.Value.Slot.Write(pair.Value.Native);}
                foreach(var change in changes)
                {
                    if(!saved.ContainsKey(change.Slot))saved[change.Slot]=change.Slot.Read();
                    if(!originals.ContainsKey(change.Name))originals.Add(change.Name,new Saved{Slot=change.Slot,Native=saved[change.Slot]});
                    string previous;if(!applied.TryGetValue(change.Name,out previous)||previous!=change.Text)change.Slot.Write(change.Native);
                }
                RestoreMissingRemovals(removed);
                foreach(string token in removed){var entry=Find(token);RemoveFromAll(entry.Control);detached.Add(token);}
                foreach(var place in places)
                {
                    var entry=Find(place.GetAttribute("token"));string parent=place.GetAttribute("parent");var destination=parent.Length==0?(GUIControlCollection)window.Children:(GUIControlCollection)((GUIGroup)Find(parent).Control).Children;
                    RemoveFromAll(entry.Control);int index=Math.Min(destination.Count,LiveWire.Int(place,"index",0,4096));destination.Insert(index,entry.Control);entry.Control.ParentControl=parent.Length==0?null:Find(parent).Control;placed.Add(entry.Token);
                }
                var obsolete=created.Keys.Where(k=>!desiredCreated.Contains(k)).ToArray();foreach(string key in obsolete)RemoveFromAll(created[key].Control);
                foreach(string name in originals.Keys.Where(k=>!names.Contains(k)).ToArray())originals.Remove(name);
                applied.Clear();foreach(var change in changes)applied[change.Name]=change.Text;
                foreach(var control in storedControls)control.StorePosition();
                GUIInfoManager.ResetCache();foreach(string key in obsolete){try{created[key].Control.Dispose();}catch{}created.Remove(key);}expectedTree=CaptureTree();
                var result=Response("frame");LiveWire.Attr(result.DocumentElement,"epoch",epoch);LiveWire.Attr(result.DocumentElement,"properties",changes.Count);LiveWire.Attr(result.DocumentElement,"created",created.Count);LiveWire.Attr(result.DocumentElement,"removed",removed.Count);return result;
            }
            catch
            {
                foreach(var pair in saved.Reverse())try{pair.Key.Write(pair.Value);}catch{}
                RestoreTree(treeBefore);
                RestoreParents(parentsBefore);detached.Clear();foreach(string token in detachedBefore)detached.Add(token);placed.Clear();foreach(string token in placedBefore)placed.Add(token);
                foreach(var pair in animationBefore)RestoreAnimation(pair.Key,pair.Value);
                foreach(string key in created.Keys.Where(k=>!existingCreated.Contains(k)).ToArray()){try{created[key].Control.Dispose();}catch{}created.Remove(key);}
                originals.Clear();foreach(var pair in originalBefore)originals.Add(pair.Key,pair.Value);applied.Clear();foreach(var pair in appliedBefore)applied.Add(pair.Key,pair.Value);throw;
            }
        }
        void AddChange(List<Change> changes,HashSet<string> names,Entry entry,string key,string text)
        {
            RuntimeProperty slot;if(!LiveWire.Fields.Contains(key)||!entry.Slots.TryGetValue(key,out slot))throw new InvalidDataException("Live property is not supported by this MediaPortal control: "+key);
            if(entry.Automatic&&(key=="posX"||key=="posY"))throw new InvalidDataException("This position is controlled by an automatic group layout.");
            string name=entry.Token+"/"+key;if(!names.Add(name))throw new InvalidDataException("Duplicate live property.");changes.Add(new Change{Name=name,Entry=entry,Slot=slot,Text=text,Native=slot.Parse(text)});
        }
        Entry Create(XmlElement request,string token)
        {
            if(created.Count>=128)throw new InvalidDataException("Live insertion is limited to 128 controls per session.");
            var definition=request.SelectSingleNode("control") as XmlElement;if(definition==null||definition.Attributes.Count>0)throw new InvalidDataException("Missing live element template.");
            var seen=new HashSet<string>();foreach(XmlNode node in definition.ChildNodes){var field=node as XmlElement;if(field==null)continue;if(field.Attributes.Count>0||field.ChildNodes.OfType<XmlElement>().Any()||!seen.Add(field.Name)||(!LiveWire.Fields.Contains(field.Name)&&!new[]{"type","id","description"}.Contains(field.Name))||field.InnerText.Length>16384)throw new InvalidDataException("Unsupported live insertion field.");}
            string kind=definition["type"]==null?"":definition["type"].InnerText;if(!new[]{"label","button","image","group"}.Contains(kind))throw new InvalidDataException("Live creation supports text, buttons, images and groups.");
            int id;if(definition["id"]==null||!Int32.TryParse(definition["id"].InnerText,out id)||id<1||entries.Values.Concat(created.Values).Any(e=>e.Control.GetID==id))throw new InvalidDataException("Live insertion requires a unique positive control ID.");
            foreach(XmlElement field in definition.ChildNodes.OfType<XmlElement>())if(field.Name.StartsWith("texture",StringComparison.Ordinal)&&field.InnerText.Length>0&&field.InnerText!="-")LiveWire.SafeRelative(skin,"Media/"+field.InnerText);
            var control=GUIControlFactory.Create(window.GetID,definition,new Dictionary<string,string>(),window.WindowXmlFileName);if(control==null)throw new InvalidOperationException("MediaPortal could not create this control.");
            try{control.AllocResources();((GUIControlCollection)window.Children).Add(control);return new Entry{Token=token,Control=control,Owner=(GUIControlCollection)window.Children,Parent="",Path="new",Slots=RuntimeProperty.For(control,skin)};}
            catch{control.Dispose();throw;}
        }
        Dictionary<GUIControlCollection,object[]> CaptureTree()
        {
            var result=new Dictionary<GUIControlCollection,object[]>();if(window==null)return result;result[(GUIControlCollection)window.Children]=window.Children.Cast<object>().ToArray();
            foreach(var group in entries.Values.Concat(created.Values).Select(e=>e.Control).OfType<GUIGroup>())result[(GUIControlCollection)group.Children]=group.Children.Cast<object>().ToArray();return result;
        }
        static void RestoreTree(Dictionary<GUIControlCollection,object[]> tree)
        {if(tree==null)return;foreach(var pair in tree){try{pair.Key.Clear();foreach(object control in pair.Value)pair.Key.Add((GUIControl)control);}catch{}}}
        void RemoveFromAll(GUIControl control)
        {foreach(var collection in CaptureTree().Keys)if(collection.Contains(control))collection.Remove(control);}
        void RestoreMissingRemovals(HashSet<string> removed)
        {
            foreach(var entry in entries.Values.Where(e=>e.Owner!=null&&detached.Contains(e.Token)&&!removed.Contains(e.Token)))
            {
                bool present=CaptureTree().Keys.Any(c=>c.Contains(entry.Control));if(!present){object[] original;if(!originalTree.TryGetValue(entry.Owner,out original))continue;int index=Array.IndexOf(original,entry.Control);entry.Owner.Insert(Math.Min(Math.Max(0,index),entry.Owner.Count),entry.Control);}detached.Remove(entry.Token);
            }
        }
        string ActualParent(Entry entry){var parent=entries.Values.Concat(created.Values).FirstOrDefault(e=>Object.ReferenceEquals(e.Control,entry.Control.ParentControl));return parent==null?"":parent.Token;}
        Dictionary<GUIControl,GUIControl> CaptureParents(){return entries.Values.Concat(created.Values).Select(e=>e.Control).Distinct().ToDictionary(c=>c,c=>c.ParentControl);}
        static void RestoreParents(Dictionary<GUIControl,GUIControl> parents){if(parents!=null)foreach(var pair in parents)try{pair.Key.ParentControl=pair.Value;}catch{}}
        public void Reset()
        {
            foreach(var item in originals.Values.Reverse())try{item.Slot.Write(item.Native);}catch{}
            foreach(var entry in entries.Values)try{RestoreAnimation(entry.Control,entry.AnimationOriginal);}catch{}
            bool scenePresent=window!=null&&Object.ReferenceEquals(window,currentWindow())&&TreeMatches();
            foreach(var entry in created.Values)try{RemoveFromAll(entry.Control);}catch{}
            if(scenePresent&&originalTree!=null)
            {
                foreach(var entry in entries.Values.Where(e=>placed.Contains(e.Token)||detached.Contains(e.Token)))try{RemoveFromAll(entry.Control);}catch{}
                foreach(var collection in originalTree)foreach(var control in collection.Value.OfType<GUIControl>())
                {var entry=entries.Values.FirstOrDefault(e=>Object.ReferenceEquals(e.Control,control));if(entry!=null&&(placed.Contains(entry.Token)||detached.Contains(entry.Token)))try{collection.Key.Insert(Math.Min(Array.IndexOf(collection.Value,control),collection.Key.Count),control);}catch{}}
                RestoreParents(originalParents);
            }
            foreach(var entry in created.Values)try{entry.Control.Dispose();}catch{}
            originals.Clear();applied.Clear();created.Clear();entries.Clear();detached.Clear();placed.Clear();originalTree=expectedTree=null;originalParents=null;window=null;epoch=null;
        }
    }
}
