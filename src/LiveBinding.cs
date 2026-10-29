using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public sealed class LiveBinding
    {
        public readonly SkinProject Baseline;public readonly LiveItem[] Items;public readonly string Epoch;
        public readonly Dictionary<string,LiveItem[]> Matches=new Dictionary<string,LiveItem[]>(StringComparer.OrdinalIgnoreCase);
        public LiveBinding(SkinProject baseline,XmlDocument snapshot)
        {
            Baseline=baseline;var root=snapshot.DocumentElement;Epoch=LiveWire.Required(root,"epoch",100);
            if(!String.Equals(Path.GetFullPath(root.GetAttribute("skin")).TrimEnd('\\','/'),baseline.Root.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Open the same skin folder in Studio and MediaPortal.");
            string actualTheme=LiveWire.ThemeKey(root.GetAttribute("theme"));
            string editorTheme=LiveWire.ThemeKey(baseline.Theme);
            if(!String.Equals(actualTheme,editorTheme,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose the same theme in Studio and MediaPortal (Studio: "+editorTheme+"; MP: "+actualTheme+").");
            if(!String.Equals(Path.GetFileName(root.GetAttribute("page")),Path.GetFileName(baseline.Page),StringComparison.OrdinalIgnoreCase)||LiveWire.Int(root,"window",Int32.MinValue,Int32.MaxValue)!=baseline.WindowId)throw new InvalidOperationException("Waiting for MediaPortal page "+baseline.Page+". Use Show page in MP if required.");
            Items=root.SelectNodes("control").Cast<XmlElement>().Select(LiveItem.Read).ToArray();if(Items.Select(i=>i.Token).Distinct().Count()!=Items.Length)throw new InvalidDataException("Duplicate runtime control token.");
            var candidates=new Dictionary<string,LiveItem[]>(StringComparer.OrdinalIgnoreCase);
            foreach(var group in baseline.Controls.GroupBy(c=>c.Key))
            {
                var control=group.First();int id;if(!Int32.TryParse(control.Get("id","0"),out id))continue;
                var pool=Items.Where(i=>i.Kind.Equals(control.Get("type"),StringComparison.OrdinalIgnoreCase)&&i.Id==id).ToArray();
                string description=control.Resolved("description");var described=pool.Where(i=>i.Description==description).ToArray();if(described.Length>0)pool=described;
                if(pool.Length>group.Count())
                {
                    var geometry=pool.Where(i=>new[]{"posX","posY"}.All(key=>Near(control.Number(key,0),i.Value(key)))).ToArray();if(geometry.Length>0)pool=geometry;
                }
                if(pool.Length>0&&pool.Length<=group.Count())candidates[group.Key]=pool;
            }
            var owners=candidates.SelectMany(pair=>pair.Value.Select(item=>new{item.Token,Key=pair.Key})).GroupBy(x=>x.Token).ToDictionary(g=>g.Key,g=>g.Select(x=>x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach(var pair in candidates)if(pair.Value.All(item=>owners[item.Token]==1))Matches[pair.Key]=pair.Value;
        }
        static bool Near(float value,string text){float parsed;return Single.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out parsed)&&Math.Abs(parsed-value)<=1;}
        public XmlDocument Verify(SkinProject current)
        {
            var doc=LiveWire.Document("verify");var root=doc.DocumentElement;LiveWire.Attr(root,"epoch",Epoch);LiveWire.Attr(root,"skin",Baseline.Root);LiveWire.Attr(root,"theme",Baseline.Theme);
            using(var sha=SHA256.Create())foreach(var source in Baseline.Sources.Values)
            {
                SkinSource editorSource;if(current.Sources.TryGetValue(source.Path,out editorSource)&&editorSource.Original!=source.Original)throw new InvalidOperationException("The disk source changed since the editor loaded it: "+source.RelativePath);
                var node=LiveWire.Add(root,"file");LiveWire.Attr(node,"path",source.RelativePath);LiveWire.Attr(node,"sha256",BitConverter.ToString(sha.ComputeHash(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray())).Replace("-","").ToLowerInvariant());
            }
            return doc;
        }
        public void Link(SkinControl control,LiveItem item)
        {
            string key=OriginalKey(control);if(key==null)throw new InvalidOperationException("New elements are linked when created in MediaPortal.");
            if(!item.Kind.Equals(control.Get("type"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Link the same control type.");
            if(Matches.Any(pair=>!pair.Key.Equals(key,StringComparison.OrdinalIgnoreCase)&&pair.Value.Any(i=>i.Token==item.Token)))throw new InvalidOperationException("This runtime control is already linked to another XML element.");Matches[key]=new[]{item};
        }
        internal static string OriginalKey(SkinControl control)
        {
            int ordinal=control.Ordinal;if(control.Source.OriginalOrdinals==null||ordinal>=control.Source.OriginalOrdinals.Count)return null;int original=control.Source.OriginalOrdinals[ordinal];return original<0?null:control.Source.RelativePath+"|"+original;
        }
        static string NewToken(SkinControl control){using(var sha=SHA256.Create())return "new:"+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(control.Source.RelativePath+"|"+control.Get("id")))).Replace("-","").ToLowerInvariant();}
        public XmlDocument Frame(SkinProject current,Dictionary<string,Dictionary<string,string>> drafts,int revision)
        {
            if(!String.Equals(current.Root,Baseline.Root,StringComparison.OrdinalIgnoreCase)||current.Page!=Baseline.Page||current.Theme!=Baseline.Theme)throw new InvalidOperationException("Live context changed; reconnecting the page.");
            var doc=LiveWire.Document("frame");var root=doc.DocumentElement;LiveWire.Attr(root,"epoch",Epoch);LiveWire.Attr(root,"revision",revision);
            var originals=Baseline.Controls.GroupBy(c=>c.Key).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
            var present=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var runtimeTokens=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            bool structural=false;
            foreach(var control in current.Controls.GroupBy(c=>c.Key).Select(g=>g.First()))
            {
                string key=OriginalKey(control);Dictionary<string,string> pending;drafts.TryGetValue(control.Key,out pending);
                if(key==null)
                {
                    if(control.Node.Children.Any(n=>n.Name!="control"&&!LiveWire.Fields.Contains(n.Name)&&!new[]{"type","id","description"}.Contains(n.Name)))throw new InvalidOperationException("This inserted element contains XML behavior outside the live template fields. Its XML is retained for export; live creation was not approximated.");
                    structural=true;string token=NewToken(control);runtimeTokens[control.Key]=token;var add=LiveWire.Add(root,"create");LiveWire.Attr(add,"token",token);var node=LiveWire.Add(add,"control");
                    foreach(string field in new[]{"type","id","description"}.Concat(LiveWire.Fields))
                    {
                        string value=pending!=null&&pending.ContainsKey(field)?pending[field]:control.Resolved(field);if(field=="width"||field=="height"){if(control.Get("type")=="group"&&value=="0")continue;}
                        if(value.Length==0&&!(field=="label"&&(control.Get("type")=="label"||control.Get("type")=="button")))continue;var property=LiveWire.Add(node,field);property.InnerText=value;
                    }
                    continue;
                }
                present.Add(key);SkinControl original;if(!originals.TryGetValue(key,out original))continue;
                var changed=new Dictionary<string,string>();
                foreach(string field in LiveWire.Fields)
                {
                    string value=pending!=null&&pending.ContainsKey(field)?pending[field]:control.Get(field);
                    if(value==original.Get(field))continue;
                    if(original.IsBound(field)&&pending!=null&&pending.ContainsKey(field)&&control.Get(field)==original.Get(field)&&!drafts.ContainsKey("@bindings-approved"))throw new InvalidOperationException("Binding protected: enable Replace bindings locally before live replacement.");
                    changed[field]=pending!=null&&pending.ContainsKey(field)?value:control.Resolved(field);
                }
                LiveItem[] matches;if(!Matches.TryGetValue(key,out matches))
                {if(changed.Count>0)throw new InvalidOperationException("Link the selected XML element to its MediaPortal control before live editing: "+control.Name);continue;}
                runtimeTokens[control.Key]=matches[0].Token;
                foreach(var item in matches)
                {
                    if(changed.Keys.Any(field=>!item.Supported.Contains(field)))throw new InvalidOperationException("This MP control does not expose live "+changed.Keys.First(field=>!item.Supported.Contains(field))+". Other preview changes remain unchanged.");
                    if(changed.Count==0)continue;var node=LiveWire.Add(root,"control");LiveWire.Attr(node,"token",item.Token);foreach(var pair in changed){var value=LiveWire.Add(node,"set");LiveWire.Attr(value,"key",pair.Key);value.InnerText=pair.Value;}
                }
                int index=current.Controls.IndexOf(control);int originalIndex=Baseline.Controls.FindIndex(c=>c.Key==key);if(index!=originalIndex)structural=true;
            }
            foreach(var pair in Matches.Where(pair=>!present.Contains(pair.Key)))foreach(var item in pair.Value){structural=true;var remove=LiveWire.Add(root,"remove");LiveWire.Attr(remove,"token",item.Token);}
            if(structural)
            {
                if(Matches.Values.Any(m=>m.Length!=1))throw new InvalidOperationException("A shared XML element appears more than once in this MP window. Live property edits are supported; export structural changes for this page.");
                var owned=new HashSet<string>(Matches.Values.SelectMany(m=>m).Select(i=>i.Token),StringComparer.Ordinal);
                var parents=new HashSet<string>(Items.Where(i=>owned.Contains(i.Token)).Select(i=>i.Parent),StringComparer.Ordinal);
                if(Items.Any(i=>parents.Contains(i.Parent)&&!owned.Contains(i.Token)))throw new InvalidOperationException("Live structure changes require all sibling runtime elements to be linked. Property edits remain available; use Link element for ambiguous XML controls.");
                var indices=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                foreach(var control in current.Controls.GroupBy(c=>c.Key).Select(g=>g.First()))
                {
                    string token;if(!runtimeTokens.TryGetValue(control.Key,out token))continue;string parent="";
                    if(control.RuntimeParent!=null&&!runtimeTokens.TryGetValue(control.RuntimeParent.Key,out parent))throw new InvalidOperationException("The runtime parent group must be linked before changing structure.");
                    string kind=control.Get("type");if(control.RuntimeParent!=null&&control.RuntimeParent.Get("type")!="group")continue;
                    int index;indices.TryGetValue(parent??"",out index);indices[parent??""]=index+1;var place=LiveWire.Add(root,"place");LiveWire.Attr(place,"token",token);LiveWire.Attr(place,"parent",parent??"");LiveWire.Attr(place,"index",index);
                }
            }
            return doc;
        }
    }
}
