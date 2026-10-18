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
        public bool ShowUnresolved=true;
        public readonly PreviewSettings Preview=new PreviewSettings();
        public readonly LanguagePreview Language=new LanguagePreview();
        public readonly CollectionSettings Collection=new CollectionSettings();
        public readonly HashSet<string> Locked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Hidden=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
    public sealed partial class SkinProject
    {
        public readonly EditorState Editor=new EditorState();
        public bool IsLocked(SkinControl control){return Controls.Where(c=>c.Key==control.Key).Any(c=>Flagged(c,Editor.Locked));}
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
            if(split<1||!Int32.TryParse(key.Substring(split+1),out ordinal)||ordinal<0)throw new InvalidDataException("Ungültiger Elementverweis im Editorprojekt.");
            string relative=key.Substring(0,split);
            if(!relative.EndsWith(".xml",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Ungültige Editorquelle.");
            return Source(SafePath(relative));
        }
        internal IEnumerable<string> ActiveEditorKeys()
        {
            foreach(var key in Editor.Locked.Concat(Editor.Hidden).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k=>k,StringComparer.OrdinalIgnoreCase))
            {int ordinal;var source=EditorSource(key,out ordinal);if(ordinal<source.Elements.Count(n=>n.Name=="control"))yield return key;}
        }
    }
    public static partial class ProjectFile
    {
        static string Number(double value){return value.ToString("R",CultureInfo.InvariantCulture);}
        public static string EditorSignature(SkinProject project)
        {
            var value=new StringBuilder(Number(project.Editor.Zoom)+","+Number(project.Editor.PanX)+","+Number(project.Editor.PanY));
            foreach(var key in project.ActiveEditorKeys())value.Append('\n').Append(key).Append(':').Append(project.Editor.Locked.Contains(key)?"L":"").Append(project.Editor.Hidden.Contains(key)?"H":"");
            value.Append('\n').Append(project.Editor.Preview.SignatureText()).Append('\n').Append(project.Editor.Language.Signature()).Append(project.Editor.ShowUnresolved?'U':'-').Append(project.Editor.Collection.Signature());
            return Digest(Encoding.UTF8.GetBytes(value.ToString()));
        }
        public static string ContentSignature(SkinProject project)
        {return Digest(Encoding.UTF8.GetBytes(XmlSignature(project)+EditorSignature(project)));}
        static void SaveEditorState(SkinProject project,XmlDocument doc)
        {
            var state=doc.CreateElement("editorState");doc.DocumentElement.AppendChild(state);
            state.SetAttribute("zoom",Number(project.Editor.Zoom));state.SetAttribute("panX",Number(project.Editor.PanX));state.SetAttribute("panY",Number(project.Editor.PanY));
            state.SetAttribute("showUnresolved",project.Editor.ShowUnresolved?"true":"false");SavePreviewSettings(project.Editor.Preview,state);
            SaveCollectionSettings(project.Editor.Collection,state);
            foreach(var key in project.ActiveEditorKeys())
            {
                int ordinal;var source=project.EditorSource(key,out ordinal);var item=doc.CreateElement("control");
                item.SetAttribute("key",key);item.SetAttribute("locked",project.Editor.Locked.Contains(key)?"true":"false");item.SetAttribute("hidden",project.Editor.Hidden.Contains(key)?"true":"false");
                item.SetAttribute("sourceSha256",Digest(OriginalBytes(source)));state.AppendChild(item);
            }
        }
        static double StateNumber(XmlElement state,string name,double minimum,double maximum)
        {
            double value;if(!Double.TryParse(state.GetAttribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out value)||Double.IsNaN(value)||Double.IsInfinity(value)||value<minimum||value>maximum)throw new InvalidDataException("Ungültiger Ansichtsstand: "+name);
            return value;
        }
        static void LoadEditorState(SkinProject project,XmlDocument doc)
        {
            var state=doc.SelectSingleNode("/skinStudioProject/editorState") as XmlElement;if(state==null)return;
            project.Editor.Zoom=StateNumber(state,"zoom",.25,16);project.Editor.PanX=StateNumber(state,"panX",-1000000,1000000);project.Editor.PanY=StateNumber(state,"panY",-1000000,1000000);
            if(state.HasAttribute("showUnresolved")){bool unresolved;if(!Boolean.TryParse(state.GetAttribute("showUnresolved"),out unresolved))throw new InvalidDataException("Ungültige Vorschauoption.");project.Editor.ShowUnresolved=unresolved;}
            LoadPreviewSettings(project.Editor.Preview,state);
            LoadCollectionSettings(project.Editor.Collection,state);
            var nodes=state.SelectNodes("control");if(nodes.Count>10000)throw new InvalidDataException("Zu viele Editor-Markierungen.");
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(XmlElement item in nodes)
            {
                string key=item.GetAttribute("key");int ordinal;var source=project.EditorSource(key,out ordinal);
                if(!seen.Add(key)||ordinal>=source.Elements.Count(n=>n.Name=="control"))throw new InvalidDataException("Doppelter oder fehlender Editor-Elementverweis.");
                if(Digest(OriginalBytes(source))!=item.GetAttribute("sourceSha256"))throw new InvalidDataException("Die Quelle einer Editor-Markierung hat sich geändert: "+source.RelativePath);
                bool locked,hidden;if(!Boolean.TryParse(item.GetAttribute("locked"),out locked)||!Boolean.TryParse(item.GetAttribute("hidden"),out hidden))throw new InvalidDataException("Ungültige Editor-Markierung.");
                if(locked)project.Editor.Locked.Add(key);if(hidden)project.Editor.Hidden.Add(key);
            }
        }
    }
}
