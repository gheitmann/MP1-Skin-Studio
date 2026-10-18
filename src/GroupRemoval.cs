using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        void GuardGroupReferences(SkinControl group)
        {
            int id;if(!Int32.TryParse(group.Get("id"),out id)||id<=0)return;
            var controlReference=new Regex(@"\bcontrol\.[A-Za-z]+\(\s*([^()]*)\s*\)",RegexOptions.IgnoreCase);
            foreach(var c in Controls.Where(c=>c.Key!=group.Key))
            {
                foreach(string property in new[]{"onleft","onright","onup","ondown"})
                {int target;if(Int32.TryParse(c.Resolved(property),out target)&&target==id)throw new InvalidOperationException("Another control refers to this group ID. Remove that reference before ungrouping.");}
                foreach(string value in c.Properties.Values.Select(p=>p.Value).Concat(new[]{c.IncludeCondition??""}))
                {
                    if(Regex.Matches(value,@"\bcontrol\.[A-Za-z]+\(",RegexOptions.IgnoreCase).Count!=controlReference.Matches(value).Count)throw new InvalidOperationException("A control condition has a complex target. Review it before ungrouping.");
                    foreach(Match match in controlReference.Matches(value))
                    {
                        string operand=match.Groups[1].Value.Trim(),resolved;var seen=new System.Collections.Generic.HashSet<string>();
                        while(c.Defines!=null&&c.Defines.TryGetValue(operand,out resolved)&&seen.Add(operand))operand=resolved;
                        int target;if(!Int32.TryParse(operand,out target)||target==id)throw new InvalidOperationException("A control condition refers to this group or has an unresolved target. Review it before ungrouping.");
                    }
                }
            }
            // Also protect literal references on other root/theme pages. Dynamic
            // references outside the loaded page cannot be fully established.
            var paths=Directory.GetFiles(Root,"*.xml").ToList();string themes=Path.Combine(Root,"Themes");
            if(Directory.Exists(themes))foreach(string folder in Directory.GetDirectories(themes))paths.AddRange(Directory.GetFiles(folder,"*.xml"));
            foreach(string path in paths)
            {
                var source=Source(path);
                foreach(var node in source.Elements.Where(n=>n.Name!="control"))
                {
                    int target;if(new[]{"onleft","onright","onup","ondown"}.Contains(node.Name)&&Int32.TryParse(node.Element.InnerText,out target)&&target==id)throw new InvalidOperationException("A skin file contains a navigation reference to this group ID.");
                    if(node.Children.Count>0)continue;
                    foreach(Match match in controlReference.Matches(node.Element.InnerText))
                        if(Int32.TryParse(match.Groups[1].Value.Trim(),out target)&&target==id)throw new InvalidOperationException("A skin file contains a condition referring to this group ID.");
                }
            }
        }
        public string[] UngroupControl(SkinControl selection)
        {
            var group=RequireCurrent(selection);GuardStructural(group);
            if(group.Get("type")!="group"||group.Node.Parent==null||(group.Node.Parent.Name!="controls"&&group.Node.Parent.Name!="control"))throw new InvalidOperationException("Select an ordinary group to ungroup.");
            if(group.Node.Parent.Name=="control"&&Controls.Any(c=>c.Source==group.Source&&c.Node==group.Node.Parent&&c.Get("type")!="group"))throw new InvalidOperationException("Only an ordinary parent group can receive promoted child controls.");
            if(!String.Equals(group.Source.RelativePath,Relative(Resolve(Page)),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Open the included source as a page before ungrouping.");
            if(group.AutomaticLayout||group.Get("layout").Trim().Length>0)throw new InvalidOperationException("Remove automatic layout rules before ungrouping; child positions may otherwise change.");
            int identity;if(group.Get("id").Length>0&&(!Int32.TryParse(group.Get("id"),out identity)||identity<0))throw new InvalidOperationException("The group ID must be a concrete nonnegative number before ungrouping.");
            if(Editor.Hidden.Contains(group.Key))throw new InvalidOperationException("Show the group in the editor before ungrouping it.");
            string[] metadata={"type","id","description","posX","posY","width","height","layout"};
            if(group.Node.Element.HasAttributes||group.Node.Children.Any(n=>n.Name!="control"&&(!metadata.Contains(n.Name)||n.Element.HasAttributes||n.Children.Count>0))||group.Properties.Keys.Any(k=>!metadata.Contains(k))||group.Node.Children.Where(n=>n.Name!="control").GroupBy(n=>n.Name).Any(g=>g.Count()>1))throw new InvalidOperationException("The group has styles, conditions, extensions or other behavior that cannot be discarded safely.");
            foreach(string property in new[]{"posX","posY","width","height"})
            {
                float value;if(group.Get(property).Length>0&&(!Single.TryParse(group.Get(property),NumberStyles.Float,CultureInfo.InvariantCulture,out value)||value!=0))throw new InvalidOperationException("Ungrouping currently requires neutral group geometry (zero position and size).");
            }
            var children=Controls.Where(c=>c.Source==group.Source&&c.Node.Parent==group.Node).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
            if(children.Length==0)throw new InvalidOperationException("The group has no direct child controls to promote.");
            GuardGroupReferences(group);
            var source=group.Source;string inner=source.Text.Substring(group.Node.OpenEnd,group.Node.CloseStart-group.Node.OpenEnd);
            foreach(var node in group.Node.Children.Where(n=>n.Name!="control").OrderByDescending(n=>n.Start))
            {
                string raw=source.Text.Substring(node.Start,node.End-node.Start);
                string comments=String.Concat(Regex.Matches(raw,@"<!--[\s\S]*?-->|<\?[\s\S]*?\?>").Cast<Match>().Select(m=>m.Value));
                int offset=node.Start-group.Node.OpenEnd;inner=inner.Substring(0,offset)+comments+inner.Substring(offset+node.End-node.Start);
            }
            var remap=new ControlKeyRemap();remap.Add(source,Enumerable.Range(0,source.OriginalOrdinals.Count).Where(i=>i!=group.Ordinal).ToArray());
            string[] keys=children.Select(c=>remap.Forward[c.Key]).ToArray();
            Transaction(new[]{source},()=>source.Text=source.Text.Substring(0,group.Node.Start)+inner+source.Text.Substring(group.Node.End),"Ungroup selected group",remap);
            return keys;
        }
    }
    public sealed partial class StudioForm
    {
        void UngroupSelection()
        {
            if(SelectedControls().Length!=1)throw new InvalidOperationException("Select exactly one group to ungroup.");
            string[] keys=null;if(!StructuralAction(()=>keys=project.UngroupControl(selected)))return;
            RestoreSelection(new EditorSelection{Primary=keys[0],Keys=keys});project.Undo.Peek().SelectionAfter=CaptureSelection();
            ShowNotice("Group removed. Its direct children are selected; undo restores the complete group.");
        }
    }
}
