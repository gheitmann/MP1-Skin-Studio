using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        static readonly Regex CloneControlReference=new Regex(@"\bcontrol\.(?<function>[a-z]+)\(\s*(?<id>[0-9]+)\s*\)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        string DuplicateGroup(SkinControl original,bool replaceBindings)
        {
            if(original.Node.Parent==null||original.Node.Parent.Name!="controls")throw new InvalidOperationException("Select a top-level group to duplicate.");
            GuardStructural(original);
            var nodes=original.Source.Elements.Where(n=>Inside(n,original.Node)).ToArray();
            if(nodes.Any(n=>n.Name=="include"||n.Name=="import"))throw new InvalidOperationException("This group includes external controls. Duplicate a self-contained group instead.");
            var controls=nodes.Where(n=>n.Name=="control").ToArray();
            var runtime=controls.Select(n=>Controls.First(c=>c.Source==original.Source&&c.Node==n)).ToArray();
            if(controls.Length>512)throw new InvalidOperationException("Duplicate groups containing at most 512 controls.");
            if(controls.Skip(1).Any(n=>n.Parent.Name!="control"||n.Parent.Value("type")!="group"))throw new InvalidOperationException("Nested view templates cannot be duplicated as a group.");
            if(!replaceBindings&&(original.IsBound("posX")||original.IsBound("posY")))throw new InvalidOperationException("Explicitly allow local replacement of bound group positions first.");
            LayoutTools.RequireNumeric(original,"posX","posY");
            var oldIds=new HashSet<int>();
            for(int index=0;index<controls.Length;index++)
            {
                var node=controls[index];int id;string text=runtime[index].Get("id","0");
                if(node.Children.Count(n=>n.Name=="id")>1||!Int32.TryParse(text,out id)||id<0)throw new InvalidOperationException("Group controls require single, static non-negative IDs.");
                if(id>0&&(!oldIds.Add(id)||LogicalControls().Count(c=>ControlId(c)==id)!=1))throw new InvalidOperationException("A group control ID is ambiguous on the open page.");
            }
            var identities=AllocateControlIds(controls.Length);var map=new Dictionary<int,int>();
            for(int index=0;index<controls.Length;index++){int id=Int32.Parse(runtime[index].Get("id","0"));if(id>0)map.Add(id,identities[index]);}
            foreach(var node in nodes)foreach(System.Xml.XmlAttribute attribute in node.Element.Attributes)
                foreach(Match match in CloneControlReference.Matches(attribute.Value))
                {int id;if(Int32.TryParse(match.Groups["id"].Value,out id)&&map.ContainsKey(id))throw new InvalidOperationException("Internal group references in XML attributes require manual duplication.");}
            string raw=original.Source.Text.Substring(original.Node.Start,original.Node.End-original.Node.Start);
            var copy=SkinSource.FromText(raw,"group-copy.xml");
            for(int index=0;index<controls.Length;index++)
            {
                var node=copy.Elements.First(n=>n.ControlOrdinal==index);var changes=new Dictionary<string,string>();
                foreach(var property in runtime[index].Properties)
                {
                    string value=property.Value.Value,rewritten=value;
                    if(runtime[index].IsBound(property.Key)&&CloneControlReference.IsMatch(runtime[index].Resolved(property.Key))&&runtime[index].Resolved(property.Key)!=value)throw new InvalidOperationException("Resolve bound control references before duplicating a group.");
                    if(new[]{"onleft","onright","onup","ondown","defaultcontrol"}.Contains(property.Key))
                    {
                        int target,next;if(!Int32.TryParse(value,out target)||target<0)throw new InvalidOperationException("Group navigation requires static non-negative target IDs.");
                        if(map.TryGetValue(target,out next))rewritten=next.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        rewritten=CloneControlReference.Replace(value,match=>
                        {
                            int target,next;if(!Int32.TryParse(match.Groups["id"].Value,out target)||!map.TryGetValue(target,out next))return match.Value;
                            if(property.Key!="visible"||!new[]{"hasfocus","isvisible","hastext","hasthumb"}.Contains(match.Groups["function"].Value.ToLowerInvariant()))throw new InvalidOperationException("An internal control reference uses an unsupported group property or function: "+property.Key+".");
                            var digits=match.Groups["id"];int offset=digits.Index-match.Index;
                            return match.Value.Substring(0,offset)+next.ToString(CultureInfo.InvariantCulture)+match.Value.Substring(offset+digits.Length);
                        });
                    }
                    if(rewritten!=value)
                    {
                        if(node.Children.Count(n=>n.Name==property.Key)>1)throw new InvalidOperationException("A group reference has duplicate XML properties.");
                        changes[property.Key]=rewritten;
                    }
                }
                if(changes.Count>0)copy.SetControlProperties(index,changes);
                copy.SetFragmentIdentity(index,identities[index]);
            }
            float x=original.Number("posX",0)+30,y=original.Number("posY",0)+30;
            if(Single.IsNaN(x)||Single.IsInfinity(x)||Single.IsNaN(y)||Single.IsInfinity(y))throw new InvalidOperationException("The group position must be finite.");
            copy.SetControlProperties(0,new Dictionary<string,string>{{"posX",x.ToString("R",CultureInfo.InvariantCulture)},{"posY",y.ToString("R",CultureInfo.InvariantCulture)},{"description",original.Name+" (Copy)"}});
            var remap=new ControlKeyRemap();remap.Add(original.Source,Enumerable.Range(0,original.Source.OriginalOrdinals.Count).Concat(Enumerable.Repeat(-1,controls.Length)).ToArray());int ordinal=-1;
            Transaction(new[]{original.Source},()=>ordinal=original.Source.AppendControl(copy.Text),"Duplicate group",remap);
            return original.Source.RelativePath+"|"+ordinal;
        }
    }
}
