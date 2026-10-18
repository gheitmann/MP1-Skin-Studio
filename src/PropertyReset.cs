using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed class PropertyResetInfo
    {
        public bool HasLocal,Allowed;
        public string Reason,LocalValue;
        public PropertyValue Inherited;
        public string AfterDescription
        {get{return Inherited==null?"Nicht gesetzt; Vorschau verwendet Ersatzwert":Inherited.Value+" · "+Inherited.Origin;}}
    }
    public sealed partial class SkinSource
    {
        internal ElementSpan ResetNode(int ordinal,string key)
        {
            if(key==null||!Regex.IsMatch(key,"^[a-zA-Z][a-zA-Z0-9]*$")||key=="id"||key=="type")throw new InvalidOperationException("Diese Eigenschaft darf nicht zurückgesetzt werden: "+key);
            var controls=Elements.Where(n=>n.Name=="control").ToArray();
            if(ordinal<0||ordinal>=controls.Length)throw new InvalidOperationException("Control nicht mehr vorhanden.");
            var matches=controls[ordinal].Children.Where(n=>n.Name==key).ToArray();
            if(matches.Length>1)throw new InvalidOperationException("Die Eigenschaft "+key+" kommt lokal mehrfach vor. Die XML-Zuordnung zuerst klären.");
            if(matches.Length==0)return null;
            var node=matches[0];
            if(node.Children.Count>0)throw new InvalidOperationException("Verschachtelte XML-Eigenschaft bleibt geschützt: "+key);
            if(node.Element.Attributes.Count>0)throw new InvalidOperationException("Die lokale Eigenschaft "+key+" hat XML-Attribute. Diese bleiben geschützt.");
            return node;
        }
        public void ResetControlProperty(int ordinal,string key)
        {
            var node=ResetNode(ordinal,key);if(node==null)return;
            // Preserve genuine comments and PIs, including their original bytes.
            // CDATA is matched first so comment-looking value text is not retained.
            var keep=new StringBuilder();
            if(!node.Empty)foreach(Match chunk in Regex.Matches(Text.Substring(node.OpenEnd,node.CloseStart-node.OpenEnd),@"<!\[CDATA\[[\s\S]*?\]\]>|<!--[\s\S]*?-->|<\?[\s\S]*?\?>"))
                if(!chunk.Value.StartsWith("<![CDATA[",StringComparison.Ordinal))keep.Append(chunk.Value);
            int start=node.Start,end=node.End;
            if(keep.Length==0)
            {
                int lineStart=Text.LastIndexOf('\n',Math.Max(0,start-1))+1,lineEnd=Text.IndexOf('\n',end);
                if(lineEnd>=0&&Text.Substring(lineStart,start-lineStart).All(ch=>ch==' '||ch=='\t')&&Text.Substring(end,lineEnd-end).All(ch=>ch==' '||ch=='\t'||ch=='\r')){start=lineStart;end=lineEnd+1;}
            }
            string before=Text;
            try{Text=Text.Substring(0,start)+keep+Text.Substring(end);Parse();Bytes();}
            catch{Text=before;Parse();throw;}
        }
    }
    public sealed partial class SkinProject
    {
        public PropertyResetInfo PreviewPropertyReset(SkinControl control,string key)
        {
            RequireCurrent(control);
            var result=new PropertyResetInfo();ElementSpan definition;
            if(defaults.TryGetValue(control.Get("type"),out definition))
            {var node=definition.Child(key);if(node!=null)result.Inherited=new PropertyValue(node.Element.InnerText,"references.xml · "+control.Get("type"),false);}
            string style=control.Node.Element.GetAttribute("Style");
            if(style.Length>0&&styles.TryGetValue(style,out definition))
            {var node=definition.Child(key);if(node!=null)result.Inherited=new PropertyValue(node.Element.InnerText,"Style "+style,false);}
            var current=control.Source.Elements.Where(n=>n.Name=="control").ElementAtOrDefault(control.Ordinal);
            result.HasLocal=current!=null&&current.Children.Any(n=>n.Name==key);
            result.LocalValue=current==null?"":current.Value(key)??"";
            try{control.Source.ResetNode(control.Ordinal,key);result.Allowed=result.HasLocal;result.Reason=result.HasLocal?"Lokale Überschreibung entfernen":"Keine lokale Überschreibung vorhanden.";}
            catch(InvalidOperationException ex){result.Allowed=false;result.Reason=ex.Message;}
            return result;
        }
        public void ResetProperty(IEnumerable<SkinControl> selection,string key,bool replaceBindings)
        {
            var controls=selection.GroupBy(c=>c.Key).Select(g=>RequireCurrent(g.First())).ToArray();
            if(controls.Any(IsLocked))throw new InvalidOperationException("Die Auswahl enthält ein gesperrtes Element. Zuerst entsperren.");
            var changed=new List<SkinControl>();
            foreach(var control in controls)
            {
                // Validate even a no-op property name before starting a transaction.
                var node=control.Source.ResetNode(control.Ordinal,key);if(node==null)continue;
                if(!replaceBindings&&node.Element.InnerText.Contains("#"))throw new InvalidOperationException("Die lokale Eigenschaft enthält eine Bindung. Zum Entfernen ausdrücklich lokal freigeben.");
                changed.Add(control);
            }
            if(changed.Count==0)return;
            Transaction(changed.Select(c=>c.Source),()=>{foreach(var control in changed)control.Source.ResetControlProperty(control.Ordinal,key);},"Lokale Eigenschaft entfernen: "+key);
        }
    }
}
