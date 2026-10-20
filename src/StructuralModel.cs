using System;
using System.Collections.Generic;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class EditorSelection
    {public string Primary;public string[] Keys;}
    public sealed partial class SkinSource
    {internal List<int> OriginalOrdinals;}

    // Ordinal keys are source positions, not identities. Explicit permutations
    // keep editor flags attached to XML controls across structural edits.
    public sealed class ControlKeyRemap
    {
        public readonly HashSet<string> Sources=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Before=new HashSet<string>(StringComparer.OrdinalIgnoreCase),After=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string,string> Forward=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<SkinSource,int[][]> IdentityOrders=new Dictionary<SkinSource,int[][]>();
        readonly HashSet<string> removedLocked=new HashSet<string>(StringComparer.OrdinalIgnoreCase),removedHidden=new HashSet<string>(StringComparer.OrdinalIgnoreCase),addedLocked=new HashSet<string>(StringComparer.OrdinalIgnoreCase),addedHidden=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> removedPosition=new HashSet<string>(StringComparer.OrdinalIgnoreCase),addedPosition=new HashSet<string>(StringComparer.OrdinalIgnoreCase),removedSize=new HashSet<string>(StringComparer.OrdinalIgnoreCase),addedSize=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public void Add(SkinSource source,int[] afterOldOrdinals)
        {
            int count=source.Elements.Count(n=>n.Name=="control");Sources.Add(source.RelativePath);
            if(source.OriginalOrdinals.Count!=count)throw new InvalidOperationException("Element identities no longer match the XML source.");
            IdentityOrders[source]=new[]{source.OriginalOrdinals.ToArray(),afterOldOrdinals.Select(i=>i<0?-1:source.OriginalOrdinals[i]).ToArray()};
            for(int index=0;index<count;index++)Before.Add(source.RelativePath+"|"+index);
            var seen=new HashSet<int>();
            for(int index=0;index<afterOldOrdinals.Length;index++)
            {
                string key=source.RelativePath+"|"+index;After.Add(key);int old=afterOldOrdinals[index];
                if(old<0)continue;if(old>=count||!seen.Add(old))throw new InvalidOperationException("Invalid structural element mapping.");Forward.Add(source.RelativePath+"|"+old,key);
            }
        }
        public Dictionary<string,string> Translation(bool forward)
        {
            var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(string key in forward?Before:After)result[key]=null;
            foreach(var pair in Forward)result[forward?pair.Key:pair.Value]=forward?pair.Value:pair.Key;return result;
        }
        void Flags(HashSet<string> flags,bool forward,HashSet<string> removed,HashSet<string> added)
        {
            var map=Translation(forward);var capture=forward?removed:added;var restore=forward?added:removed;capture.Clear();
            var next=new List<string>();
            foreach(string key in flags)
            {
                string mapped;if(map.TryGetValue(key,out mapped)){if(mapped==null)capture.Add(key);else next.Add(mapped);}
            }
            flags.RemoveWhere(key=>Sources.Any(source=>key.StartsWith(source+"|",StringComparison.OrdinalIgnoreCase)));
            foreach(string key in next)flags.Add(key);foreach(string key in restore)flags.Add(key);
        }
        public void Apply(SkinProject project,bool forward)
        {Flags(project.Editor.Locked,forward,removedLocked,addedLocked);Flags(project.Editor.PositionLocked,forward,removedPosition,addedPosition);Flags(project.Editor.SizeLocked,forward,removedSize,addedSize);Flags(project.Editor.Hidden,forward,removedHidden,addedHidden);foreach(var pair in IdentityOrders)pair.Key.OriginalOrdinals=pair.Value[forward?1:0].ToList();}
    }
    public sealed partial class SkinProject
    {
        public Dictionary<string,string> LastKeyTranslation=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        internal SkinControl RequireCurrent(SkinControl control)
        {
            if(control==null)throw new InvalidOperationException("Select an element first.");
            var current=Controls.FirstOrDefault(c=>c.Key==control.Key);
            if(current==null||current.Source!=control.Source||current.Node!=control.Node)throw new InvalidOperationException("The selection is stale. Select the element again.");return current;
        }
        static bool Inside(ElementSpan node,ElementSpan parent){return node.Start>=parent.Start&&node.End<=parent.End;}
        static bool RuntimeInside(SkinControl child,SkinControl parent){for(var owner=child.RuntimeParent;owner!=null;owner=owner.RuntimeParent)if(owner.Key==parent.Key)return true;return false;}
        internal void GuardStructural(SkinControl control)
        {
            if(IsLocked(control)||Controls.Any(c=>(c.Source==control.Source&&Inside(c.Node,control.Node)||RuntimeInside(c,control))&&IsLocked(c)))throw new InvalidOperationException("The element or one of its child controls is locked. Unlock it in the element tree first.");
        }
        public void ReorderControl(SkinControl selected,string mode)
        {
            var control=RequireCurrent(selected);GuardStructural(control);
            if(!new[]{"forward","backward","front","back"}.Contains(mode))throw new InvalidOperationException("Unknown layer direction.");
            var parent=control.Node.Parent;if(parent==null||(parent.Name!="controls"&&parent.Name!="control"))throw new InvalidOperationException("This XML structure does not yet support layer ordering.");
            int at=parent.Children.IndexOf(control.Node),first=at,last=at;
            while(first>0&&parent.Children[first-1].Name=="control")first--;
            while(last+1<parent.Children.Count&&parent.Children[last+1].Name=="control")last++;
            var slots=parent.Children.Skip(first).Take(last-first+1).ToArray();int index=at-first;
            int target=mode=="front"?slots.Length-1:mode=="back"?0:mode=="forward"?Math.Min(slots.Length-1,index+1):Math.Max(0,index-1);
            LastKeyTranslation.Clear();if(index==target)return;
            var ordered=slots.ToList();ordered.RemoveAt(index);ordered.Insert(target,control.Node);
            var source=control.Source;var all=source.Elements.Where(n=>n.Name=="control").ToArray();int low=Array.IndexOf(all,slots[0]),high=Array.FindLastIndex(all,n=>Inside(n,slots[slots.Length-1]));
            var middle=ordered.SelectMany(slot=>Enumerable.Range(0,all.Length).Where(i=>Inside(all[i],slot)));
            var order=Enumerable.Range(0,low).Concat(middle).Concat(Enumerable.Range(high+1,all.Length-high-1)).ToArray();var remap=new ControlKeyRemap();remap.Add(source,order);
            var raws=ordered.Select(n=>source.Text.Substring(n.Start,n.End-n.Start)).ToArray();
            Transaction(new[]{source},()=>{for(int i=slots.Length-1;i>=0;i--)source.Text=source.Text.Substring(0,slots[i].Start)+raws[i]+source.Text.Substring(slots[i].End);},"Change layer order",remap);
        }
    }
}
