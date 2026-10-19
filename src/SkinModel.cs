using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MP1SkinStudio
{
    // The runtime XML document is never serialized back to the user's skin.
    // Edits patch source spans instead, retaining unknown XML, comments and formatting.
    public sealed class ElementSpan
    {
        public string Name;
        public int Start, OpenEnd, CloseStart, End;
        public int ControlOrdinal = -1;
        public bool Empty;
        public ElementSpan Parent;
        public List<ElementSpan> Children = new List<ElementSpan>();
        public XmlElement Element;
        public ElementSpan Child(string name) { return Children.LastOrDefault(n => n.Name == name); }
        public string Value(string name) { var c = Child(name); return c == null ? null : c.Element.InnerText; }
    }

    public sealed partial class SkinSource
    {
        public string Path, RelativePath, Original, Text;
        public Encoding Encoding;
        public byte[] Preamble;
        public List<ElementSpan> Elements;
        public ElementSpan Root;
        public bool Changed { get { return Text != Original; } }

        public static SkinSource Read(string path, string relative)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > 20 * 1024 * 1024) throw new InvalidDataException("XML file exceeds 20 MB: " + relative);
            int offset = 0;
            Encoding enc = new UTF8Encoding(false, true);
            byte[] bom = new byte[0];
            if (bytes.Length >= 4 && ((bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0 && bytes[3] == 0) || (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xfe && bytes[3] == 0xff)))
                throw new InvalidDataException("UTF-32 is unsupported by this prototype.");
            if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            { offset = 3; bom = new byte[] {239,187,191}; }
            else if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
            { enc = new UnicodeEncoding(false, false, true); offset = 2; bom = new byte[] {255,254}; }
            else if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
            { enc = new UnicodeEncoding(true, false, true); offset = 2; bom = new byte[] {254,255}; }
            else if (bytes.Length >= 4 && bytes[0] == 60 && bytes[1] == 0)
                enc = new UnicodeEncoding(false, false, true);
            else if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 60)
                enc = new UnicodeEncoding(true, false, true);
            else
            {
                string prefix = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 300));
                var m = Regex.Match(prefix, "^\\s*<\\?xml[^?]*encoding\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase);
                if (m.Success) enc = System.Text.Encoding.GetEncoding(m.Groups[1].Value, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                if (enc.CodePage == 1200 || enc.CodePage == 1201) throw new InvalidDataException("XML encoding does not match the file bytes.");
            }
            var s = new SkinSource { Path = path, RelativePath = relative, Encoding = enc, Preamble = bom };
            s.Text = s.Original = enc.GetString(bytes, offset, bytes.Length - offset);
            s.Parse();
            return s;
        }

        public static SkinSource FromText(string text, string name)
        {
            var s = new SkinSource { Text = text, Original = text, RelativePath = name, Path = name,
                Encoding = new UTF8Encoding(false, true), Preamble = new byte[0] };
            s.Parse(); return s;
        }

        public byte[] Bytes()
        {
            var data = Encoding.GetBytes(Text);
            return Preamble.Concat(data).ToArray();
        }

        public void Parse()
        {
            var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20 * 1024 * 1024 };
            using (var reader = XmlReader.Create(new StringReader(Text), settings)) doc.Load(reader);
            Elements = new List<ElementSpan>();
            var stack = new Stack<ElementSpan>();
            int p = 0, controlOrdinal = 0;
            while ((p = Text.IndexOf('<', p)) >= 0)
            {
                if (Text.IndexOf("<!--", p, StringComparison.Ordinal) == p) { p = Text.IndexOf("-->", p + 4, StringComparison.Ordinal) + 3; continue; }
                if (Text.IndexOf("<![CDATA[", p, StringComparison.Ordinal) == p) { p = Text.IndexOf("]]>", p + 9, StringComparison.Ordinal) + 3; continue; }
                if (Text.IndexOf("<?", p, StringComparison.Ordinal) == p) { p = Text.IndexOf("?>", p + 2, StringComparison.Ordinal) + 2; continue; }
                int end = p + 1; char quote = '\0';
                for (; end < Text.Length; end++)
                {
                    char ch = Text[end];
                    if (quote != '\0') { if (ch == quote) quote = '\0'; }
                    else if (ch == '\'' || ch == '"') quote = ch;
                    else if (ch == '>') break;
                }
                if (Text[p + 1] == '/')
                {
                    var n = stack.Pop(); n.CloseStart = p; n.End = end + 1;
                }
                else
                {
                    var match = Regex.Match(Text.Substring(p + 1, end - p - 1), "^([^\\s/>]+)");
                    if (!match.Success) throw new InvalidDataException("Invalid XML tag.");
                    var n = new ElementSpan { Name = match.Groups[1].Value, Start = p, OpenEnd = end + 1,
                        Empty = Text[end - 1] == '/', CloseStart = end + 1, End = end + 1 };
                    if(n.Name=="control")n.ControlOrdinal=controlOrdinal++;
                    if (stack.Count > 0) { n.Parent = stack.Peek(); n.Parent.Children.Add(n); }
                    Elements.Add(n);
                    if (!n.Empty) stack.Push(n);
                }
                p = end + 1;
            }
            var xmlElements = doc.SelectNodes("//*").Cast<XmlElement>().ToArray();
            if (xmlElements.Length != Elements.Count) throw new InvalidDataException("XML source mapping failed.");
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].Name != xmlElements[i].Name) throw new InvalidDataException("XML source mapping is ambiguous.");
                Elements[i].Element = xmlElements[i];
            }
            Root = Elements.First();
            if(OriginalOrdinals==null)OriginalOrdinals=Enumerable.Range(0,controlOrdinal).ToList();
        }

        public void SetControlProperties(int ordinal, IDictionary<string, string> values)
        {SetControlPropertiesCore(ordinal,values,false);}
        internal void SetFragmentIdentity(int ordinal,int identity)
        {
            if(Root.Name!="control"||identity<=0)throw new InvalidOperationException("Identity assignment requires a new control fragment.");
            SetControlPropertiesCore(ordinal,new Dictionary<string,string>{{"id",identity.ToString(CultureInfo.InvariantCulture)}},true);
        }
        void SetControlPropertiesCore(int ordinal, IDictionary<string, string> values,bool allowIdentity)
        {
            var controls = Elements.Where(n => n.Name == "control").ToArray();
            if (ordinal < 0 || ordinal >= controls.Length) throw new InvalidOperationException("Control no longer exists.");
            var control = controls[ordinal];
            var patches = new List<Tuple<int, int, string>>();
            var additions = new StringBuilder();
            string newline = Text.Contains("\r\n") ? "\r\n" : "\n";
            string childIndent = IndentAt(control.Start) + "  ";
            if (control.Children.Count > 0) childIndent = IndentAt(control.Children[0].Start);
            if (childIndent.Length == 0) childIndent = "  ";
            foreach (var kv in values)
            {
                if (!Regex.IsMatch(kv.Key, "^[a-zA-Z][a-zA-Z0-9]*$") || (kv.Key == "id"&&!allowIdentity) || kv.Key == "type")
                    throw new InvalidOperationException("This property cannot be changed: " + kv.Key);
                var node = control.Child(kv.Key);
                if (node == null)
                {
                    additions.Append(newline).Append(childIndent).Append('<').Append(kv.Key).Append('>')
                        .Append(SecurityElement.Escape(kv.Value)).Append("</").Append(kv.Key).Append('>');
                }
                else
                {
                    if (node.Children.Count > 0) throw new InvalidOperationException("Nested XML property remains protected: " + kv.Key);
                    if (node.Element.InnerText == kv.Value) continue;
                    if((kv.Key=="posX"||kv.Key=="posY")&&node.Element.GetAttribute("property").Length>0)throw new InvalidOperationException("Property-driven positions remain protected. Adjust preview samples to inspect the position.");
                    if (node.Empty)
                    {
                        string opening = Text.Substring(node.Start, node.End - node.Start);
                        int slash = opening.LastIndexOf('/');
                        string replacement = opening.Substring(0, slash) + ">" + SecurityElement.Escape(kv.Value) + "</" + node.Name + ">";
                        patches.Add(Tuple.Create(node.Start, node.End, replacement));
                    }
                    else
                    {
                        string inner = Text.Substring(node.OpenEnd, node.CloseStart - node.OpenEnd);
                        // Retain comments/PIs even when they occur inside a scalar property.
                        var chunks = Regex.Matches(inner, @"<!--[\s\S]*?-->|<\?[\s\S]*?\?>|<!\[CDATA\[[\s\S]*?\]\]>|[^<]+");
                        var b = new StringBuilder(); bool written = false;
                        foreach (Match m in chunks)
                        {
                            if (m.Value.StartsWith("<!--") || m.Value.StartsWith("<?")) { b.Append(m.Value); continue; }
                            if (m.Value.StartsWith("<![CDATA["))
                            { if (!written) b.Append("<![CDATA[").Append(kv.Value.Replace("]]>", "]]]]><![CDATA[>")).Append("]]>"); written = true; }
                            else if (!String.IsNullOrWhiteSpace(m.Value))
                            { if (!written) b.Append(SecurityElement.Escape(kv.Value)); written = true; }
                            else b.Append(m.Value);
                        }
                        if (!written) b.Append(SecurityElement.Escape(kv.Value));
                        patches.Add(Tuple.Create(node.OpenEnd, node.CloseStart, b.ToString()));
                    }
                }
            }
            if (additions.Length > 0)
            {
                if (control.Empty) throw new InvalidOperationException("Empty controls cannot be expanded.");
                int insertion = control.CloseStart;
                while (insertion > control.OpenEnd && Char.IsWhiteSpace(Text[insertion - 1])) insertion--;
                patches.Add(Tuple.Create(insertion, insertion, additions.ToString()));
            }
            string old = Text;
            try
            {
                foreach (var patch in patches.OrderByDescending(x => x.Item1))
                    Text = Text.Substring(0, patch.Item1) + patch.Item3 + Text.Substring(patch.Item2);
                Parse(); Bytes(); // Reject values that cannot be encoded in the original encoding.
            }
            catch { Text = old; Parse(); throw; }
        }
        string IndentAt(int pos)
        {
            int line = Text.LastIndexOf('\n', Math.Max(0, pos - 1));
            string before = Text.Substring(line + 1, pos - line - 1);
            return before.All(c => c == ' ' || c == '\t') ? before : "";
        }
    }

    public sealed class PropertyValue
    {
        public string Value, Origin, Expression="", ExpressionOrigin="";
        public bool Local;
        public PropertyValue(string value, string origin, bool local) { Value = value; Origin = origin; Local = local; }
    }

    public sealed class SkinControl
    {
        public SkinSource Source;
        public ElementSpan Node;
        public int Ordinal, Depth;
        public bool AutomaticLayout;
        public string Key, IncludeCondition;
        public SkinControl RuntimeParent;public VisibilityTerm[] VisibilityTerms;
        public Dictionary<string, PropertyValue> Properties = new Dictionary<string, PropertyValue>(StringComparer.Ordinal);
        public Dictionary<string, string> Defines;
        public string Get(string key, string fallback = "") { PropertyValue p; return Properties.TryGetValue(key, out p) ? p.Value : fallback; }
        public string Resolved(string key, string fallback = "")
        {
            string value = Get(key, fallback), result;
            var visited = new HashSet<string>();
            while (Defines != null && Defines.TryGetValue(value, out result) && visited.Add(value)) value = result;
            return value;
        }
        public float Number(string key, float fallback)
        { float n; return Single.TryParse(Resolved(key), NumberStyles.Float, CultureInfo.InvariantCulture, out n) ? n : fallback; }
        public string Name { get { return Get("description", Get("label", Get("type", "Control"))); } }
        public bool IsBound(string key) { PropertyValue p;return Get(key).Contains("#")||(Properties.TryGetValue(key,out p)&&p.Expression.Length>0); }
    }

    public sealed class FileChange
    { public SkinSource Source; public string Before, After; }
    public sealed class Change
    { public List<FileChange> Files = new List<FileChange>(); public string Description; public ControlKeyRemap Remap; public EditorSelection SelectionBefore,SelectionAfter; }

    public sealed partial class SkinProject
    {
        public string Root, Theme = "", Page;
        public Dictionary<string, SkinSource> Sources = new Dictionary<string, SkinSource>(StringComparer.OrdinalIgnoreCase);
        public List<SkinControl> Controls = new List<SkinControl>();
        public List<string> Warnings = new List<string>();
        public Dictionary<string, float> FontSizes = new Dictionary<string, float>();
        public List<string> FontNames = new List<string>();
        public Stack<Change> Undo = new Stack<Change>(), Redo = new Stack<Change>();
        public int Width = 1920, Height = 1080, WindowId,DefaultControlId=-1;
        Dictionary<string, ElementSpan> defaults = new Dictionary<string, ElementSpan>();
        Dictionary<string, ElementSpan> styles = new Dictionary<string, ElementSpan>();
        Dictionary<string, string> globalDefines = new Dictionary<string, string>();
        public SkinProject(string root) { Root = System.IO.Path.GetFullPath(root); }
        public bool Dirty { get { return Sources.Values.Any(s => s.Changed); } }
        public string Relative(string full) { return full.Substring(Root.TrimEnd('\\').Length + 1).Replace('\\', '/'); }
        public string SafePath(string relative)
        {
            if (String.IsNullOrWhiteSpace(relative)) throw new InvalidDataException("Empty file path.");
            string result = System.IO.Path.GetFullPath(System.IO.Path.Combine(Root, relative.Replace('/', '\\').TrimStart('\\')));
            if (!result.StartsWith(Root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("File path is outside the skin: " + relative);
            return result;
        }
        public string Resolve(string relative)
        {
            if (!String.IsNullOrEmpty(Theme))
            {
                string candidate = SafePath("Themes/" + Theme + "/" + relative);
                if (File.Exists(candidate)) return candidate;
            }
            return SafePath(relative);
        }
        public SkinSource Source(string path)
        {
            path=System.IO.Path.GetFullPath(path.Replace('/', '\\'));
            if(!path.StartsWith(Root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("File path is outside the skin: "+path);
            SkinSource s;
            if (Sources.TryGetValue(path, out s)) return s;
            s = SkinSource.Read(path, Relative(path)); Sources[path] = s; return s;
        }
        public string[] Pages()
        {
            var paths = Directory.GetFiles(Root, "*.xml").Select(System.IO.Path.GetFileName);
            if (!String.IsNullOrEmpty(Theme))
                paths = paths.Concat(Directory.GetFiles(SafePath("Themes/" + Theme), "*.xml").Select(System.IO.Path.GetFileName));
            return paths.Where(p => !new [] {"references.xml", "fonts.xml", "skin.xml", "theme.xml"}.Contains(p.ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        void LoadDefines(ElementSpan root, Dictionary<string,string> dict)
        {
            foreach (var d in root.Children.Where(n => n.Name == "define"))
            {
                string text = d.Element.InnerText; int colon = text.IndexOf(':');
                if (colon > 0) dict[text.Substring(0,colon)] = text.Substring(colon+1);
            }
        }
        void OpenPageCore(string page)
        {
            Page = page; Controls.Clear(); Warnings.Clear(); defaults.Clear(); styles.Clear(); globalDefines.Clear(); FontSizes.Clear(); FontNames.Clear();
            Width = 1920; Height = 1080; WindowId = 0;DefaultControlId=-1;FontSpecs.Clear();
            string refs = Resolve("references.xml");
            if (File.Exists(refs))
            {
                var r = Source(refs); LoadDefines(r.Root, globalDefines);
                foreach (var c in r.Root.Children.Where(n=>n.Name == "control")) { string type=c.Value("type"); if(type!=null) defaults[type]=c; }
                foreach (var s in r.Root.Children.Where(n=>n.Name == "style")) { string name=s.Element.GetAttribute("Name"); if(name.Length>0) styles[name]=s; }
                var skin = r.Root.Child("skin"); int dimension;
                if (skin != null) { if(Int32.TryParse(skin.Value("width"),out dimension) && dimension>0) Width=dimension; if(Int32.TryParse(skin.Value("height"),out dimension) && dimension>0) Height=dimension; }
            }
            else Warnings.Add("references.xml is missing; default values are incomplete.");
            string fonts = Resolve("fonts.xml");
            if (File.Exists(fonts))
                foreach (var f in Source(fonts).Root.Children.Where(n=>n.Name=="font"))
                { float h; var name=f.Value("name"); if (name!=null && Single.TryParse(f.Value("height"),NumberStyles.Float,CultureInfo.InvariantCulture,out h)) { FontNames.Add(name); FontSizes[name]=h; } }
            var start = Source(Resolve(page)); int.TryParse(start.Root.Value("id"), out WindowId);int defaultControl;if(Int32.TryParse(start.Root.Value("defaultcontrol"),out defaultControl)&&defaultControl>0)DefaultControlId=defaultControl;
            if(File.Exists(fonts))LoadFontSpecs(Source(fonts));
            AddWindow(start, new Dictionary<string,string>(globalDefines), new HashSet<string>(StringComparer.OrdinalIgnoreCase), "", 0);
            var automaticKeys=new HashSet<string>(Controls.Where(c=>c.AutomaticLayout).Select(c=>c.Key));foreach(var control in Controls)if(automaticKeys.Contains(control.Key))control.AutomaticLayout=true;
            Warnings.AddRange(new StackPreview(this,null).Warnings.Distinct());
            if (Controls.Any(c=>c.Get("type")=="group" || c.Get("type")=="actiongroup")) Warnings.Add("StackLayout and GridLayout are evaluated as static previews; animations and transformations remain approximate.");
        }
        void AddWindow(SkinSource source, Dictionary<string,string> context, HashSet<string> active, string inheritedCondition, int depth,bool automaticLayout=false,SkinControl runtimeParent=null,VisibilityTerm[] terms=null)
        {
            if (depth > 24 || !active.Add(source.Path)) { Warnings.Add("Cyclic or excessively deep include: " + source.RelativePath); return; }
            try
            {
                LoadDefines(source.Root,context);
                var parent=source.Root.Child("controls");
                if(parent==null) { if(!source.Root.Children.Any(n=>n.Name=="define"))Warnings.Add("No <controls> in " + source.RelativePath); return; }
                AddNodes(source,parent,context,active,inheritedCondition,depth,automaticLayout,runtimeParent,terms);
            }
            finally { active.Remove(source.Path); }
        }
        void AddNodes(SkinSource source, ElementSpan parent, Dictionary<string,string> context, HashSet<string> active, string condition, int depth,bool automaticLayout=false,SkinControl runtimeParent=null,VisibilityTerm[] terms=null)
        {
            foreach (var n in parent.Children)
            {
                if(n.Name=="import" || n.Name=="include")
                {
                    string target=n.Element.InnerText.Trim(), path;
                    string local=n.Element.GetAttribute("condition");
                    string next=JoinCondition(condition,local);var conditionContext=new Dictionary<string,string>(context);
                    try
                    {
                        path=Resolve(target);
                        if(File.Exists(path)) AddWindow(Source(path),context,active,next,depth+1,automaticLayout,runtimeParent,PreviewState.AddTerm(terms,local,conditionContext));
                        else Warnings.Add("Missing include: " + target + " (" + source.RelativePath + ")");
                        string otherwise=n.Element.GetAttribute("otherwise");
                        if(otherwise.Length>0) { path=Resolve(otherwise); if(File.Exists(path)) AddWindow(Source(path),context,active,JoinCondition(condition,"!["+local+"]"),depth+1,automaticLayout,runtimeParent,PreviewState.AddTerm(terms,"!["+local+"]",conditionContext)); }
                    }
                    catch(Exception ex) { Warnings.Add("Include " + target + ": " + ex.Message); }
                    continue;
                }
                if(n.Name!="control") continue;
                var c=new SkinControl { Source=source,Node=n,Depth=depth,IncludeCondition=condition,AutomaticLayout=automaticLayout,RuntimeParent=runtimeParent,VisibilityTerms=terms??new VisibilityTerm[0],
                    Ordinal=n.ControlOrdinal, Defines=new Dictionary<string,string>(context) };
                c.Key=source.RelativePath+"|"+c.Ordinal;
                ElementSpan d; string type=n.Value("type")??"";
                if(defaults.TryGetValue(type,out d)) Merge(c,d,"references.xml · " + type,false);
                string style=n.Element.GetAttribute("Style");
                if(style.Length>0) { if(styles.TryGetValue(style,out d)) Merge(c,d,"Style " + style,false); else Warnings.Add("Unknown style: " + style); }
                Merge(c,n,source.RelativePath,true); Controls.Add(c);
                if(n.Children.Any(e=>e.Name=="control"||e.Name=="include"||e.Name=="import")) AddNodes(source,n,context,active,JoinCondition(condition,c.Get("visible")),depth+1,automaticLayout||c.Get("layout").Trim().Length>0,c,PreviewState.AddTerm(terms,c.Get("visible"),c.Defines));
            }
        }
        static string JoinCondition(string a,string b) { return String.IsNullOrEmpty(a)?b:String.IsNullOrEmpty(b)?a:"["+a+"]+["+b+"]"; }
        static void Merge(SkinControl c, ElementSpan n,string origin,bool local)
        {
            foreach(var p in n.Children.Where(e=>e.Name!="control"))
            {
                PropertyValue inherited;c.Properties.TryGetValue(p.Name,out inherited);bool attribute=p.Element.HasAttribute("property");
                c.Properties[p.Name]=new PropertyValue(p.Element.InnerText,origin,local){Expression=attribute?p.Element.GetAttribute("property"):inherited==null?"":inherited.Expression,ExpressionOrigin=attribute?origin:inherited==null?"":inherited.ExpressionOrigin};
            }
        }
        public void Edit(SkinControl control, IDictionary<string,string> values, string description)
        {
            EditMany(new[]{new ControlEdit(control,values)},description);
        }
        public void UndoChange() { LastKeyTranslation.Clear();if(Undo.Count==0)return;var c=Undo.Pop();foreach(var f in c.Files){f.Source.Text=f.Before;f.Source.Parse();}if(c.Remap!=null){c.Remap.Apply(this,false);LastKeyTranslation=c.Remap.Translation(false);}Redo.Push(c);OpenPage(Page); }
        public void RedoChange() { LastKeyTranslation.Clear();if(Redo.Count==0)return;var c=Redo.Pop();foreach(var f in c.Files){f.Source.Text=f.After;f.Source.Parse();}if(c.Remap!=null){c.Remap.Apply(this,true);LastKeyTranslation=c.Remap.Translation(true);}Undo.Push(c);OpenPage(Page); }
        public void Export(string target)
        {
            if(!target.EndsWith(".zip",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Export as a ZIP file. Skin XML files are not overwritten directly.");
            var changed=Sources.Values.Where(s=>s.Changed).OrderBy(s=>s.RelativePath).ToArray();
            if(changed.Length==0) throw new InvalidOperationException("No changes yet.");
            // Validate every file before creating an archive; source directories are never written.
            foreach(var s in changed) { s.Parse(); s.Bytes(); }
            string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create))
                {
                    foreach(var s in changed)
                    {
                        var entry=zip.CreateEntry(s.RelativePath,CompressionLevel.Optimal);
                        using(var output=entry.Open()) { var bytes=s.Bytes(); output.Write(bytes,0,bytes.Length); }
                    }
                    var info=zip.CreateEntry("_SkinStudio-Instructions.txt");
                    using(var writer=new StreamWriter(info.Open(),new UTF8Encoding(false)))
                        writer.Write("MP1 Skin Studio — Prototype\r\n\r\nOnly modified XML files, relative to the selected skin directory.\r\nBack up or copy the complete skin directory first, then apply these files to the copy.\r\nUnmodified images and XML files remain in the existing skin directory.\r\nThe preview is approximate and is not an MP1 engine test. Validate in MP1.\r\n\r\n"+String.Join("\r\n",changed.Select(s=>s.RelativePath)));
                }
                if(File.Exists(target)) File.Replace(temp,target,null);
                else File.Move(temp,target);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }

    // Small, deliberately three-valued preview interpreter, never used for export.
    public sealed partial class PreviewState
    {
        public SkinProject Project;
        public int FocusId = -1;
        public HashSet<string> Plugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,string> Values = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public PreviewState(SkinProject p)
        {
            Project=p;RestoreFocus();
            Values=PreviewSettings.BaseValues();p.Editor.Preview.Validate();
            foreach(var pair in p.Editor.Preview.Values)Values[pair.Key]=pair.Value;
            foreach(string plugin in p.Editor.Preview.Plugins)Plugins.Add(plugin);
            UpdateCollectionValues();
        }
        public void RestoreFocus()
        {
            FocusId=Project.DefaultControlId;
            int saved;if(Project.Editor.TryFocus(Project.Page,Project.Theme,out saved)&&Project.LogicalControls().Count(c=>SkinProject.Focusable(c)&&SkinProject.ControlId(c)==saved)==1)FocusId=saved;
            UpdateCollectionValues();
        }
        public void SetFocus(int id)
        {
            int previous;
            if(id==Project.DefaultControlId&&!Project.Editor.TryFocus(Project.Page,Project.Theme,out previous)){FocusId=id;UpdateCollectionValues();return;}
            Project.Editor.RememberFocus(Project.Page,Project.Theme,id);FocusId=id;UpdateCollectionValues();
        }
        public string Text(SkinControl c,string value)
        {
            string result;TryText(c,value,out result);return result;
        }
        internal bool TryText(SkinControl c,string value,out string result)
        {
            string v;if(Project.Editor.Preview.Values.TryGetValue(value,out v)){result=v;return true;}
            var visited=new HashSet<string>();while(c.Defines!=null&&c.Defines.TryGetValue(value,out v)&&visited.Add(value))value=v;
            bool known;result=ExpressionText(c,value,out known);return known;
        }
        public bool? Visible(SkinControl c) { return VisibleFor(c,new HashSet<string>()); }
        static bool? And(bool? a,bool? b) { if(a==false||b==false)return false;if(a==null||b==null)return null;return true; }
        static bool Encloses(string s) {int depth=0;char quote='\0';for(int i=0;i<s.Length;i++){char c=s[i];if(quote!='\0'){if(c==quote)quote='\0';continue;}if(c=='\''||c=='"'){quote=c;continue;}if(c=='['||c=='(')depth++;if(c==']'||c==')')depth--;if(depth==0&&i<s.Length-1)return false;}return depth==0&&quote=='\0';}
        static List<string> Split(string s,char delimiter)
        {var result=new List<string>();int depth=0,start=0;char quote='\0';for(int i=0;i<s.Length;i++){char c=s[i];if(quote!='\0'){if(c==quote)quote='\0';continue;}if(c=='\''||c=='"'){quote=c;continue;}if(c=='['||c=='(')depth++;if(c==']'||c==')')depth--;if(c==delimiter&&depth==0){result.Add(s.Substring(start,i-start));start=i+1;}}result.Add(s.Substring(start));return result;}
    }
}
