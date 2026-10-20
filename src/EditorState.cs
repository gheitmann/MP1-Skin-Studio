using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class EditorState
    {
        public double Zoom=1,PanX,PanY;
        public int SnapSpacing=10;
        public bool SnapEnabled=true,GridVisible=true;
        public bool ShowUnresolved=true;
        public string PreviewMode="layout",MediaTipScene="music";
        public int PreviewFocusId=-1;
        public string PreviewFocusPage="";
        public string PreviewFocusTheme="";
        internal sealed class FocusEntry {public string Page,Theme;public int Id;}
        internal readonly Dictionary<string,FocusEntry> FocusEntries=new Dictionary<string,FocusEntry>(StringComparer.OrdinalIgnoreCase);
        internal bool TryFocus(string page,string theme,out int id)
        {FocusEntry entry;if(FocusEntries.TryGetValue((theme??"")+"\n"+page,out entry)){id=entry.Id;return true;}id=-1;return false;}
        internal void RememberFocus(string page,string theme,int id)
        {
            string key=(theme??"")+"\n"+page;
            ValidateFocus(page,theme,id);
            FocusEntry previous;if(FocusEntries.TryGetValue(key,out previous)&&previous.Id==id)return;
            FocusEntries[key]=new FocusEntry{Page=page,Theme=theme??"",Id=id};PreviewFocusPage=page;PreviewFocusTheme=theme??"";PreviewFocusId=id;
        }
        internal void ValidateFocus(string page,string theme,int id)
        {if(id<=0||String.IsNullOrWhiteSpace(page)||(!FocusEntries.ContainsKey((theme??"")+"\n"+page)&&FocusEntries.Count>=128))throw new InvalidDataException("Invalid preview focus or more than 128 saved page/theme focus states.");}
        public readonly PreviewSettings Preview=new PreviewSettings();
        public readonly LanguagePreview Language=new LanguagePreview();
        public readonly CollectionSettings Collection=new CollectionSettings();
        public readonly HashSet<string> Locked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> PositionLocked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> SizeLocked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Hidden=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
    public sealed partial class SkinProject
    {
        public readonly EditorState Editor=new EditorState();
        public bool IsLocked(SkinControl control){return Controls.Where(c=>c.Key==control.Key).Any(c=>Flagged(c,Editor.Locked));}
        public bool IsPositionLocked(SkinControl control){return IsLocked(control)||Controls.Where(c=>c.Key==control.Key).Any(c=>Flagged(c,Editor.PositionLocked));}
        public bool IsSizeLocked(SkinControl control){return IsLocked(control)||Controls.Where(c=>c.Key==control.Key).Any(c=>Flagged(c,Editor.SizeLocked));}
        public bool IsPropertyLocked(SkinControl control,string key)
        {return IsLocked(control)||(key=="posX"||key=="posY")&&IsPositionLocked(control)||(key=="width"||key=="height")&&IsSizeLocked(control);}
        public bool IsEditorHidden(SkinControl control){return Flagged(control,Editor.Hidden);}
        bool Flagged(SkinControl control,HashSet<string> flags)
        {
            if(flags.Contains(control.Key))return true;
            for(var owner=control.RuntimeParent;owner!=null;owner=owner.RuntimeParent)if(flags.Contains(owner.Key))return true;
            for(var parent=control.Node.Parent;parent!=null;parent=parent.Parent)
                if(parent.Name=="control"&&flags.Contains(control.Source.RelativePath+"|"+control.Source.Elements.Where(n=>n.Name=="control").TakeWhile(n=>n!=parent).Count()))return true;
            return false;
        }
        internal SkinSource EditorSource(string key,out int ordinal)
        {
            int split=key.LastIndexOf('|');
            if(split<1||!Int32.TryParse(key.Substring(split+1),out ordinal)||ordinal<0)throw new InvalidDataException("Invalid element reference in editor project.");
            string relative=key.Substring(0,split);
            if(!relative.EndsWith(".xml",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid editor source.");
            return Source(SafePath(relative));
        }
        internal IEnumerable<string> ActiveEditorKeys()
        {
            foreach(var key in Editor.Locked.Concat(Editor.PositionLocked).Concat(Editor.SizeLocked).Concat(Editor.Hidden).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k=>k,StringComparer.OrdinalIgnoreCase))
            {int ordinal;var source=EditorSource(key,out ordinal);if(ordinal<source.Elements.Count(n=>n.Name=="control"))yield return key;}
        }
    }
    public static partial class ProjectFile
    {
        static string Number(double value){return value.ToString("R",CultureInfo.InvariantCulture);}
        public static string EditorSignature(SkinProject project)
        {
            ReadablePreview.Validate(project.Editor);
            ValidateGrid(project.Editor);
            var value=new StringBuilder(Number(project.Editor.Zoom)+","+Number(project.Editor.PanX)+","+Number(project.Editor.PanY));
            string page=project.Page??"",theme=project.Theme??"";
            value.Append('\n').Append(page.Length).Append(':').Append(page).Append(theme.Length).Append(':').Append(theme);
            foreach(var key in project.ActiveEditorKeys())value.Append('\n').Append(key).Append(':').Append(project.Editor.Locked.Contains(key)?"L":"").Append(project.Editor.PositionLocked.Contains(key)?"P":"").Append(project.Editor.SizeLocked.Contains(key)?"S":"").Append(project.Editor.Hidden.Contains(key)?"H":"");
            value.Append('\n').Append(project.Editor.SnapSpacing).Append(':').Append(project.Editor.SnapEnabled).Append(':').Append(project.Editor.GridVisible);
            value.Append('\n').Append(project.Editor.Preview.SignatureText()).Append('\n').Append(project.Editor.Language.Signature()).Append(project.Editor.ShowUnresolved?'U':'-').Append(project.Editor.Collection.Signature());
            value.Append('\n').Append(project.Editor.PreviewMode).Append(':').Append(project.Editor.MediaTipScene);
            value.Append('\n').Append(project.Editor.PreviewFocusPage).Append(':').Append(project.Editor.PreviewFocusId);
            value.Append(':').Append(project.Editor.PreviewFocusTheme);
            foreach(var entry in project.Editor.FocusEntries.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase))value.Append('\n').Append(entry.Key.Length).Append(':').Append(entry.Key.ToLowerInvariant()).Append(':').Append(entry.Value.Id);
            return Digest(Encoding.UTF8.GetBytes(value.ToString()));
        }
        public static string ContentSignature(SkinProject project)
        {return Digest(Encoding.UTF8.GetBytes(XmlSignature(project)+EditorSignature(project)));}
        static void SaveEditorState(SkinProject project,XmlDocument doc)
        {
            ReadablePreview.Validate(project.Editor);
            ValidateGrid(project.Editor);
            var state=doc.CreateElement("editorState");doc.DocumentElement.AppendChild(state);
            state.SetAttribute("zoom",Number(project.Editor.Zoom));state.SetAttribute("panX",Number(project.Editor.PanX));state.SetAttribute("panY",Number(project.Editor.PanY));
            state.SetAttribute("snapSpacing",project.Editor.SnapSpacing.ToString(CultureInfo.InvariantCulture));state.SetAttribute("snapEnabled",project.Editor.SnapEnabled?"true":"false");state.SetAttribute("gridVisible",project.Editor.GridVisible?"true":"false");
            state.SetAttribute("previewMode",project.Editor.PreviewMode);state.SetAttribute("mediaTipScene",project.Editor.MediaTipScene);
            state.SetAttribute("showUnresolved",project.Editor.ShowUnresolved?"true":"false");SavePreviewSettings(project.Editor.Preview,state);
            SaveCollectionSettings(project.Editor.Collection,state);
            if(project.Editor.PreviewFocusId>0){state.SetAttribute("focusPage",project.Editor.PreviewFocusPage);state.SetAttribute("focusTheme",project.Editor.PreviewFocusTheme);state.SetAttribute("focusId",project.Editor.PreviewFocusId.ToString(CultureInfo.InvariantCulture));}
            foreach(var entry in project.Editor.FocusEntries.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase).Select(p=>p.Value))
            {var item=doc.CreateElement("focus");item.SetAttribute("page",entry.Page);item.SetAttribute("theme",entry.Theme);item.SetAttribute("id",entry.Id.ToString(CultureInfo.InvariantCulture));state.AppendChild(item);}
            foreach(var key in project.ActiveEditorKeys())
            {
                int ordinal;var source=project.EditorSource(key,out ordinal);var item=doc.CreateElement("control");
                item.SetAttribute("key",key);item.SetAttribute("locked",project.Editor.Locked.Contains(key)?"true":"false");item.SetAttribute("hidden",project.Editor.Hidden.Contains(key)?"true":"false");
                item.SetAttribute("positionLocked",project.Editor.PositionLocked.Contains(key)?"true":"false");item.SetAttribute("sizeLocked",project.Editor.SizeLocked.Contains(key)?"true":"false");
                item.SetAttribute("sourceSha256",Digest(OriginalBytes(source)));state.AppendChild(item);
            }
        }
        static double StateNumber(XmlElement state,string name,double minimum,double maximum)
        {
            double value;if(!Double.TryParse(state.GetAttribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out value)||Double.IsNaN(value)||Double.IsInfinity(value)||value<minimum||value>maximum)throw new InvalidDataException("Invalid view state: "+name);
            return value;
        }
        static void LoadEditorState(SkinProject project,XmlDocument doc)
        {
            var state=doc.SelectSingleNode("/skinStudioProject/editorState") as XmlElement;if(state==null)return;
            if(state.HasAttribute("snapSpacing")){int spacing;if(!Int32.TryParse(state.GetAttribute("snapSpacing"),NumberStyles.None,CultureInfo.InvariantCulture,out spacing))throw new InvalidDataException("Invalid snap spacing.");project.Editor.SnapSpacing=spacing;}
            project.Editor.SnapEnabled=OptionalStateBoolean(state,"snapEnabled",true);project.Editor.GridVisible=OptionalStateBoolean(state,"gridVisible",true);ValidateGrid(project.Editor);
            project.Editor.PreviewMode=state.HasAttribute("previewMode")?state.GetAttribute("previewMode"):"all";
            project.Editor.MediaTipScene=state.HasAttribute("mediaTipScene")?state.GetAttribute("mediaTipScene"):"music";ReadablePreview.Validate(project.Editor);
            project.Editor.Zoom=StateNumber(state,"zoom",.25,16);project.Editor.PanX=StateNumber(state,"panX",-1000000,1000000);project.Editor.PanY=StateNumber(state,"panY",-1000000,1000000);
            if(state.HasAttribute("showUnresolved")){bool unresolved;if(!Boolean.TryParse(state.GetAttribute("showUnresolved"),out unresolved))throw new InvalidDataException("Invalid preview option.");project.Editor.ShowUnresolved=unresolved;}
            if(!state.HasAttribute("previewMode")&&!project.Editor.ShowUnresolved)project.Editor.PreviewMode="layout";
            LoadPreviewSettings(project.Editor.Preview,state);
            LoadCollectionSettings(project.Editor.Collection,state);
            if(state.HasAttribute("focusId")||state.HasAttribute("focusPage"))
            {
                int focus;if(!Int32.TryParse(state.GetAttribute("focusId"),NumberStyles.None,CultureInfo.InvariantCulture,out focus)||focus<=0||String.IsNullOrWhiteSpace(state.GetAttribute("focusPage")))throw new InvalidDataException("Invalid preview focus.");
                string focusTheme=state.HasAttribute("focusTheme")?state.GetAttribute("focusTheme"):project.Theme;
                project.SafePath(state.GetAttribute("focusPage"));if(focusTheme.Length>0)project.SafePath("Themes/"+focusTheme);project.Editor.RememberFocus(state.GetAttribute("focusPage"),focusTheme,focus);
            }
            var focusNodes=state.SelectNodes("focus");if(focusNodes.Count>128)throw new InvalidDataException("Too many saved preview focus states.");
            if(focusNodes.Count>0)
            {
                var seenFocus=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(XmlElement item in focusNodes)
                {
                    int id;string page=item.GetAttribute("page"),theme=item.GetAttribute("theme");
                    if(!Int32.TryParse(item.GetAttribute("id"),NumberStyles.None,CultureInfo.InvariantCulture,out id)||id<=0||String.IsNullOrWhiteSpace(page)||!seenFocus.Add(theme+"\n"+page))throw new InvalidDataException("Invalid or duplicate saved preview focus.");
                    project.SafePath(page);if(theme.Length>0)project.SafePath("Themes/"+theme);
                    project.Editor.FocusEntries[theme+"\n"+page]=new EditorState.FocusEntry{Page=page,Theme=theme,Id=id};
                }
                int last;if(project.Editor.FocusEntries.Count>128||(project.Editor.PreviewFocusId>0&&(!seenFocus.Contains(project.Editor.PreviewFocusTheme+"\n"+project.Editor.PreviewFocusPage)||!project.Editor.TryFocus(project.Editor.PreviewFocusPage,project.Editor.PreviewFocusTheme,out last)||last!=project.Editor.PreviewFocusId)))throw new InvalidDataException("Inconsistent saved preview focus.");
            }
            var nodes=state.SelectNodes("control");if(nodes.Count>10000)throw new InvalidDataException("Too many editor markers.");
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(XmlElement item in nodes)
            {
                string key=item.GetAttribute("key");int ordinal;var source=project.EditorSource(key,out ordinal);
                if(!seen.Add(key)||ordinal>=source.Elements.Count(n=>n.Name=="control"))throw new InvalidDataException("Duplicate or missing editor element reference.");
                if(Digest(OriginalBytes(source))!=item.GetAttribute("sourceSha256"))throw new InvalidDataException("The source for an editor marker has changed: "+source.RelativePath);
                bool locked,hidden;if(!Boolean.TryParse(item.GetAttribute("locked"),out locked)||!Boolean.TryParse(item.GetAttribute("hidden"),out hidden))throw new InvalidDataException("Invalid editor marker.");
                if(locked)project.Editor.Locked.Add(key);if(hidden)project.Editor.Hidden.Add(key);
                if(OptionalStateBoolean(item,"positionLocked",false))project.Editor.PositionLocked.Add(key);if(OptionalStateBoolean(item,"sizeLocked",false))project.Editor.SizeLocked.Add(key);
            }
        }
        static bool OptionalStateBoolean(XmlElement element,string name,bool fallback)
        {if(!element.HasAttribute(name))return fallback;bool value;if(!Boolean.TryParse(element.GetAttribute(name),out value))throw new InvalidDataException("Invalid editor option: "+name);return value;}
        static void ValidateGrid(EditorState state)
        {if(state.SnapSpacing<1||state.SnapSpacing>200)throw new InvalidDataException("Snap spacing must be between 1 and 200 skin pixels.");}
    }
}
