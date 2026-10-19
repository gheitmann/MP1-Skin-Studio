using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace MP1SkinStudio
{
    public sealed class ControlEdit
    {
        public SkinControl Control;
        public IDictionary<string,string> Values;
        public ControlEdit(SkinControl c,IDictionary<string,string> values){Control=c;Values=values;}
    }

    public sealed partial class SkinSource
    {
        internal int AppendControl(string raw)
        {
            var fragment=FromText(raw,"new-control.xml");
            if(fragment.Root.Name!="control"||String.IsNullOrEmpty(fragment.Root.Value("type")))throw new InvalidDataException("Invalid control template.");
            var parent=Root.Child("controls");
            if(parent==null)throw new InvalidOperationException("This file contains no <controls> section.");
            int ordinal=Elements.Count(n=>n.Name=="control");
            string newline=Text.Contains("\r\n")?"\r\n":"\n";
            string indent=parent.Children.Count>0?IndentAt(parent.Children[0].Start):IndentAt(parent.Start)+"  ";
            string normalized=raw.Replace("\r\n","\n").Replace("\r","\n").Trim();
            string addition=newline+indent+normalized.Replace("\n",newline+indent);
            if(parent.Empty)
            {
                string opening=Text.Substring(parent.Start,parent.OpenEnd-parent.Start);
                opening=opening.Substring(0,opening.LastIndexOf('/'))+">";
                Text=Text.Substring(0,parent.Start)+opening+addition+newline+IndentAt(parent.Start)+"</controls>"+Text.Substring(parent.End);
                Parse();Bytes();return ordinal;
            }
            int insertion=parent.CloseStart;
            while(insertion>parent.OpenEnd&&Char.IsWhiteSpace(Text[insertion-1]))insertion--;
            Text=Text.Substring(0,insertion)+addition+Text.Substring(insertion);Parse();Bytes();return ordinal;
        }
        internal void AllocateIdentity(int id)
        {
            if(Root.Name!="control"||Elements.Count(n=>n.Name=="control")!=1||id<=0)throw new InvalidOperationException("Identity assignment is allowed only for new individual controls.");
            var identityNode=Root.Child("id");string digits=id.ToString(CultureInfo.InvariantCulture);
            if(identityNode==null)Text=Text.Insert(Root.OpenEnd,"<id>"+digits+"</id>");
            else if(identityNode.Empty)Text=Text.Substring(0,identityNode.Start)+"<id>"+digits+"</id>"+Text.Substring(identityNode.End);
            else Text=Text.Substring(0,identityNode.OpenEnd)+digits+Text.Substring(identityNode.CloseStart);
            Parse();
        }
    }

    public sealed partial class SkinProject
    {
        void Transaction(IEnumerable<SkinSource> files,Action mutate,string description,ControlKeyRemap remap=null)
        {
            LastKeyTranslation.Clear();var change=new Change{Description=description,Remap=remap};var oldLocked=Editor.Locked.ToArray();var oldHidden=Editor.Hidden.ToArray();
            foreach(var source in files.Distinct())change.Files.Add(new FileChange{Source=source,Before=source.Text});
            var oldOrders=change.Files.ToDictionary(f=>f.Source,f=>f.Source.OriginalOrdinals.ToArray());
            try
            {
                mutate();
                foreach(var file in change.Files){file.Source.Parse();file.Source.Bytes();file.After=file.Source.Text;}
                OpenPage(Page);
                if(change.Files.Any(f=>f.Before!=f.After)){if(remap!=null){remap.Apply(this,true);LastKeyTranslation=remap.Translation(true);}Undo.Push(change);Redo.Clear();}
            }
            catch
            {
                foreach(var file in change.Files){file.Source.Text=file.Before;file.Source.Parse();}
                foreach(var pair in oldOrders)pair.Key.OriginalOrdinals=pair.Value.ToList();
                Editor.Locked.Clear();foreach(string key in oldLocked)Editor.Locked.Add(key);Editor.Hidden.Clear();foreach(string key in oldHidden)Editor.Hidden.Add(key);LastKeyTranslation.Clear();
                OpenPage(Page);throw;
            }
        }
        public void EditMany(IEnumerable<ControlEdit> changes,string description)
        {
            var requested=changes.Where(e=>e.Values.Count>0).ToArray();
            foreach(var edit in requested)RequireCurrent(edit.Control);
            foreach(var edit in requested)foreach(var value in edit.Values)
            {PropertyValue property;if((value.Key=="posX"||value.Key=="posY")&&edit.Control.Properties.TryGetValue(value.Key,out property)&&property.Expression.Length>0&&value.Value!=property.Value)throw new InvalidOperationException("Property-driven positions remain protected. Adjust preview samples to inspect "+edit.Control.Name+" · "+value.Key+".");}
            var edits=requested.GroupBy(e=>e.Control.Key).Select(group=>
            {
                var values=new Dictionary<string,string>();
                foreach(var edit in group)foreach(var pair in edit.Values)
                {
                    string existing;if(values.TryGetValue(pair.Key,out existing)&&existing!=pair.Value)
                        throw new InvalidOperationException("Conflicting values for "+pair.Key+" on the same XML control. No changes were applied.");
                    values[pair.Key]=pair.Value;
                }
                return new ControlEdit(group.First().Control,values);
            }).ToArray();
            if(edits.Length==0)return;
            if(edits.Any(e=>IsLocked(e.Control)))throw new InvalidOperationException("The selection contains a locked element. Unlock it in the element tree first.");
            Transaction(edits.Select(e=>e.Control.Source),()=>
            {
                foreach(var edit in edits)edit.Control.Source.SetControlProperties(edit.Control.Ordinal,edit.Values);
            },description);
        }
        public int NextControlId()
        {return AllocateControlIds(1)[0];}
        int[] AllocateControlIds(int count)
        {
            var candidates=Directory.GetFiles(Root,"*.xml").ToList();
            string themes=System.IO.Path.Combine(Root,"Themes");
            if(Directory.Exists(themes))foreach(var folder in Directory.GetDirectories(themes))candidates.AddRange(Directory.GetFiles(folder,"*.xml"));
            var used=new HashSet<int>();
            foreach(var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var source=Source(path);
                foreach(var n in source.Elements.Where(n=>n.Name=="defaultcontrol"||n.Parent!=null&&n.Parent.Name=="control"&&(n.Name=="id"||n.Name=="onleft"||n.Name=="onright"||n.Name=="onup"||n.Name=="ondown")))
                {int id;if(Int32.TryParse(n.Element.InnerText,out id)&&id>0)used.Add(id);}
                foreach(var n in source.Elements)
                {
                    var values=n.Element.Attributes.Cast<System.Xml.XmlAttribute>().Select(a=>a.Value);
                    if(n.Children.Count==0)values=values.Concat(new[]{n.Element.InnerText});
                    foreach(string value in values)foreach(System.Text.RegularExpressions.Match match in CloneControlReference.Matches(value))
                    {int id;if(Int32.TryParse(match.Groups["id"].Value,out id)&&id>0)used.Add(id);}
                }
            }
            var result=new List<int>();int next=10000;
            while(result.Count<count){if(!used.Contains(next))result.Add(next);if(result.Count<count){if(next==Int32.MaxValue)throw new InvalidOperationException("No available control ID.");next++;}}
            return result.ToArray();
        }
        public string InsertControl(string sourceRelative,string kind)
        {
            if(!new[]{"label","button","image"}.Contains(kind))throw new InvalidOperationException("Unknown element template.");
            var source=Source(SafePath(sourceRelative));int identity=NextControlId(),ordinal=-1;
            string font=FontNames.Contains("TitanLight22")?"TitanLight22":FontNames.FirstOrDefault()??"font22";
            var fields=new List<KeyValuePair<string,string>>{
                new KeyValuePair<string,string>("description",kind=="label"?"New text":kind=="button"?"New button":"New image"),
                new KeyValuePair<string,string>("type",kind),new KeyValuePair<string,string>("id",identity.ToString()),
                new KeyValuePair<string,string>("posX","300"),new KeyValuePair<string,string>("posY","300"),
                new KeyValuePair<string,string>("width",kind=="image"?"300":"420"),new KeyValuePair<string,string>("height",kind=="image"?"300":kind=="button"?"110":"70")};
            if(kind=="image")fields.Add(new KeyValuePair<string,string>("texture",""));
            else{fields.Add(new KeyValuePair<string,string>("font",font));fields.Add(new KeyValuePair<string,string>("textcolor","ffffffff"));fields.Add(new KeyValuePair<string,string>("label",kind=="button"?"New button":"Your text"));fields.Add(new KeyValuePair<string,string>("align",kind=="button"?"center":"left"));}
            string raw="<control>\n"+String.Join("\n",fields.Select(k=>"  <"+k.Key+">"+SecurityElement.Escape(k.Value)+"</"+k.Key+">"))+"\n</control>";
            var remap=new ControlKeyRemap();remap.Add(source,Enumerable.Range(0,source.OriginalOrdinals.Count).Concat(new[]{-1}).ToArray());
            Transaction(new[]{source},()=>ordinal=source.AppendControl(raw),"Insert element",remap);
            return source.RelativePath+"|"+ordinal;
        }
        public string DuplicateControl(SkinControl original,bool replaceBindings)
        {
            RequireCurrent(original);
            if(original.Get("type")=="group")return DuplicateGroup(original,replaceBindings);
            if(original.Node.Children.Any(n=>n.Name=="control")||original.Node.Parent==null||original.Node.Parent.Name!="controls")throw new InvalidOperationException("Select a top-level element or a self-contained top-level group to duplicate.");
            if(!replaceBindings&&(original.IsBound("posX")||original.IsBound("posY")))throw new InvalidOperationException("The copy requires a new position. Explicitly allow local replacement of bound positions.");
            LayoutTools.RequireNumeric(original,"posX","posY");
            int id=NextControlId(),ordinal=-1;
            string raw=original.Source.Text.Substring(original.Node.Start,original.Node.End-original.Node.Start);
            var copy=SkinSource.FromText(raw,"clone.xml");copy.AllocateIdentity(id);
            copy.SetControlProperties(0,new Dictionary<string,string>{{"posX",((int)original.Number("posX",0)+30).ToString()},{"posY",((int)original.Number("posY",0)+30).ToString()},{"description",original.Name+" (Copy)"}});
            var remap=new ControlKeyRemap();remap.Add(original.Source,Enumerable.Range(0,original.Source.OriginalOrdinals.Count).Concat(new[]{-1}).ToArray());
            Transaction(new[]{original.Source},()=>ordinal=original.Source.AppendControl(copy.Text),"Duplicate element",remap);
            return original.Source.RelativePath+"|"+ordinal;
        }
        public static bool Focusable(SkinControl c)
        {return CollectionSettings.IsCollection(c)||c.Get("type").Contains("button")||new[]{"slider","spincontrol","textbox","checkmark"}.Contains(c.Get("type"));}
        public static int? ControlId(SkinControl c)
        {int id;return Int32.TryParse(c.Resolved("id"),out id)?(int?)id:null;}
        public SkinControl NavigationTarget(SkinControl source,string direction)
        {
            if(!new[]{"onleft","onright","onup","ondown"}.Contains(direction))throw new InvalidOperationException("Invalid navigation direction.");
            source=LogicalControl(source);
            if(source.Get("type")=="facadeview"){var view=CollectionSettings.Template(this,source,Editor.Collection.Mode);if(view!=null&&ControlId(view)==ControlId(source))source=view;}
            string value=source.Resolved(direction);int id;
            if(!Int32.TryParse(value,out id)||id<=0)throw new InvalidOperationException("No unambiguous navigation target for "+direction+".");
            var matches=LogicalControls().Where(c=>ControlId(c)==id).ToArray();
            if(matches.Length!=1||!Focusable(matches[0]))throw new InvalidOperationException("Navigation target ID "+value+" is missing, ambiguous or not focusable.");return matches[0];
        }
        public SkinControl PreviewNavigationTarget(PreviewState state,string direction)
        {
            if(state==null||state.Project!=this)throw new InvalidOperationException("Preview state belongs to another project.");
            if(!new[]{"onleft","onright","onup","ondown"}.Contains(direction))throw new InvalidOperationException("Invalid navigation direction.");
            var sources=LogicalControls().Where(c=>Focusable(c)&&ControlId(c)==state.FocusId).ToArray();
            if(sources.Length!=1)throw new InvalidOperationException("The simulated focus is missing or ambiguous.");
            var mappedSource=sources[0];
            if(mappedSource.Get("type")=="facadeview"){var view=CollectionSettings.Template(this,mappedSource,Editor.Collection.Mode);if(view!=null&&ControlId(view)==ControlId(mappedSource))mappedSource=view;}
            string mapping=mappedSource.Resolved(direction).Trim();
            var target=mapping.Length==0||mapping=="0"?StackNavigationTarget(state,sources[0],direction):NavigationTarget(sources[0],direction);int before=state.FocusId;
            try
            {
                state.FocusId=ControlId(target).Value;bool? visible=state.Visible(target);
                if(IsEditorHidden(target)||visible!=true)throw new InvalidOperationException(visible==null?"The navigation target has unverified visibility. Apply suitable sample data first.":"The navigation target is hidden in this preview.");
                return target;
            }
            finally{state.FocusId=before;}
        }
        public void ConnectNavigation(SkinControl source,SkinControl target,string direction,bool replaceBinding)
        {
            RequireCurrent(source);RequireCurrent(target);
            source=LogicalControl(source);target=LogicalControl(target);
            if(source.Get("type")=="facadeview"){var view=CollectionSettings.Template(this,source,Editor.Collection.Mode);if(view!=null&&ControlId(view)==ControlId(source))source=view;}
            if(!new[]{"onleft","onright","onup","ondown"}.Contains(direction))throw new InvalidOperationException("Invalid navigation direction.");
            int sourceId,targetId;
            if(!Focusable(source)||!Focusable(target)||!Int32.TryParse(source.Get("id"),out sourceId)||sourceId<=0||!Int32.TryParse(target.Get("id"),out targetId)||targetId<=0)
                throw new InvalidOperationException("Source and target must be focusable controls with positive IDs.");
            if(LogicalControls().Count(c=>ControlId(c)==sourceId)!=1||LogicalControls().Count(c=>ControlId(c)==targetId)!=1)throw new InvalidOperationException("Duplicate control ID: the connection is ambiguous.");
            if(source.IsBound(direction)&&!replaceBinding)throw new InvalidOperationException("This navigation target is bound. Explicitly allow local replacement.");
            Edit(source,new Dictionary<string,string>{{direction,targetId.ToString()}},"Connect navigation");
        }
    }

    public static class LayoutTools
    {
        public static bool IsContainer(SkinControl c){return c.Get("type")=="group"||c.Node.Children.Any(n=>n.Name=="control");}
        public static void RequireManualGeometry(SkinControl c)
        {
            if(IsContainer(c))throw new InvalidOperationException("Do not move or align group frames directly. Select their individual child controls through the Element menu.");
            if(c.AutomaticLayout)throw new InvalidOperationException("The control belongs to a group with layout rules. Automatic movement or alignment is unavailable; edit XML properties explicitly if needed.");
        }
        public static void RequireNumeric(SkinControl c,params string[] keys)
        {
            if(keys.Any(k=>{PropertyValue p;return c.Properties.TryGetValue(k,out p)&&p.Expression.Length>0;}))throw new InvalidOperationException("Property-driven geometry remains protected. Use preview samples to inspect its position.");
            foreach(var key in keys)
            {float value;if(c.Get(key).Length>0&&!Single.TryParse(c.Resolved(key),NumberStyles.Float,CultureInfo.InvariantCulture,out value))throw new InvalidOperationException("Geometry cannot be resolved: "+c.Name+" · "+key+". Set a concrete value first.");}
        }
        public static RectangleF Bounds(SkinControl c)
        {
            float x=c.Number("posX",0),y=c.Number("posY",0),w=c.Number("width",c.Get("type")=="label"?400:240),h=c.Number("height",c.Get("type")=="label"?64:70);
            if(c.Get("type")=="label"||c.Get("type")=="fadelabel"){if(c.Get("align")=="right")x-=w;else if(c.Get("align")=="center")x-=w/2;}
            return new RectangleF(x,y,Math.Max(1,w),Math.Max(1,h));
        }
        public static List<ControlEdit> Align(IEnumerable<SkinControl> selection,string mode,bool replaceBindings)
        {
            var controls=selection.GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
            if(controls.Length<2)throw new InvalidOperationException("Select at least two elements with CTRL.");
            bool horizontal=new[]{"left","right","hcenter","hspace"}.Contains(mode);
            if(!horizontal&&!new[]{"top","bottom","vcenter","vspace"}.Contains(mode))throw new InvalidOperationException("Unknown alignment.");
            if(mode.EndsWith("space")&&controls.Length<3)throw new InvalidOperationException("Select at least three elements for equal spacing.");
            string property=horizontal?"posX":"posY";
            if(!replaceBindings&&controls.Any(c=>c.IsBound(property)))throw new InvalidOperationException("At least one position is bound. Explicitly allow local replacement before aligning.");
            foreach(var c in controls){RequireManualGeometry(c);RequireNumeric(c,property,horizontal?"width":"height");}
            if(controls.Any(c=>c.Node.Parent.Name!="controls")&&!controls.All(c=>c.Source==controls[0].Source&&c.Node.Parent==controls[0].Node.Parent))throw new InvalidOperationException("Align nested controls together only within the same group.");
            var bounds=controls.ToDictionary(c=>c.Key,Bounds);float low=horizontal?bounds.Values.Min(r=>r.Left):bounds.Values.Min(r=>r.Top),high=horizontal?bounds.Values.Max(r=>r.Right):bounds.Values.Max(r=>r.Bottom);
            if(mode.EndsWith("space"))controls=controls.OrderBy(c=>horizontal?bounds[c.Key].Left:bounds[c.Key].Top).ToArray();
            float gap=mode.EndsWith("space")?(high-low-bounds.Values.Sum(r=>horizontal?r.Width:r.Height))/(controls.Length-1):0,cursor=low;
            var result=new List<ControlEdit>();
            foreach(var c in controls)
            {
                var r=bounds[c.Key];float length=horizontal?r.Width:r.Height,current=horizontal?r.Left:r.Top,target;
                if(mode=="left"||mode=="top")target=low;else if(mode=="right"||mode=="bottom")target=high-length;
                else if(mode=="hcenter"||mode=="vcenter")target=(low+high-length)/2;else{target=cursor;cursor+=length+gap;}
                float pos=c.Number(property,0)+target-current;
                string value=Math.Round(pos).ToString(CultureInfo.InvariantCulture);
                if(value!=c.Resolved(property))result.Add(new ControlEdit(c,new Dictionary<string,string>{{property,value}}));
            }
            return result;
        }
    }
}
