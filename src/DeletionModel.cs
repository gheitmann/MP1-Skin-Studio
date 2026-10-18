using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed class DeletionReference
    {public string ControlKey,Name,Source,Property,Value;public int TargetId;}
    public sealed class DeletionPlan
    {
        public SkinControl[] Roots;
        public readonly HashSet<string> RemovedKeys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<DeletionReference> References=new List<DeletionReference>();
        public string[] SharedFiles;
        public int BoundControls;
    }
    public sealed partial class SkinProject
    {
        public DeletionPlan InspectDeletion(IEnumerable<SkinControl> selection)
        {
            var controls=selection.GroupBy(c=>c.Key).Select(g=>RequireCurrent(g.First())).ToArray();if(controls.Length==0)throw new InvalidOperationException("Zuerst Elemente auswählen.");
            var roots=controls.Where(c=>!controls.Any(parent=>parent.Key!=c.Key&&parent.Source==c.Source&&Inside(c.Node,parent.Node))).ToArray();
            foreach(var control in roots){GuardStructural(control);if(control.Node.Parent==null||(control.Node.Parent.Name!="controls"&&control.Node.Parent.Name!="control"))throw new InvalidOperationException("Diese XML-Struktur kann nicht entfernt werden.");}
            var plan=new DeletionPlan{Roots=roots};
            foreach(var control in Controls.Where(c=>roots.Any(root=>root.Source==c.Source&&Inside(c.Node,root.Node))))plan.RemovedKeys.Add(control.Key);
            var removed=Controls.Where(c=>plan.RemovedKeys.Contains(c.Key)).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
            plan.BoundControls=removed.Count(c=>c.Properties.Values.Any(p=>p.Value.Contains("#")));
            var ids=new HashSet<int>(removed.Select(ControlId).Where(id=>id.HasValue&&id.Value>0).Select(id=>id.Value));
            foreach(var control in Controls.Where(c=>!plan.RemovedKeys.Contains(c.Key)))
            {
                foreach(string property in new[]{"onleft","onright","onup","ondown"})
                {int target;if(Int32.TryParse(control.Resolved(property),out target)&&ids.Contains(target))plan.References.Add(new DeletionReference{ControlKey=control.Key,Name=control.Name,Source=control.Source.RelativePath,Property=property,Value=control.Get(property),TargetId=target});}
                foreach(var pair in new[]{new KeyValuePair<string,string>("visible",control.Get("visible")),new KeyValuePair<string,string>("Include-Bedingung",control.IncludeCondition)})
                    foreach(Match match in Regex.Matches(pair.Value??"",@"control\.[A-Za-z]+\(\s*([0-9]+)\s*\)",RegexOptions.IgnoreCase))
                    {int target;if(Int32.TryParse(match.Groups[1].Value,out target)&&ids.Contains(target))plan.References.Add(new DeletionReference{ControlKey=control.Key,Name=control.Name,Source=control.Source.RelativePath,Property=pair.Key,Value=pair.Value,TargetId=target});}
            }
            var unique=plan.References.GroupBy(r=>r.ControlKey+"\n"+r.Property+"\n"+r.TargetId+"\n"+r.Value).Select(g=>g.First()).ToArray();plan.References.Clear();plan.References.AddRange(unique);
            string current=Relative(Resolve(Page));plan.SharedFiles=roots.Select(c=>c.Source.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Where(path=>!String.Equals(path,current,StringComparison.OrdinalIgnoreCase)).ToArray();return plan;
        }
        public void DeleteControls(IEnumerable<SkinControl> selection,bool allowReferences=false,bool allowShared=false)
        {
            var plan=InspectDeletion(selection);
            if(plan.References.Count>0&&!allowReferences)throw new InvalidOperationException("Die Auswahl hat mögliche Verweise in der geöffneten Seite. Diese zuerst prüfen und das unveränderte Beibehalten ausdrücklich bestätigen.");
            if(plan.SharedFiles.Length>0&&!allowShared)throw new InvalidOperationException("Die Auswahl liegt in eingebundenen Quelldateien. Die Wirkung auf gemeinsam verwendete Dateien ausdrücklich bestätigen.");
            var remap=new ControlKeyRemap();
            foreach(var group in plan.Roots.GroupBy(c=>c.Source))
            {
                var all=group.Key.Elements.Where(n=>n.Name=="control").ToArray();
                remap.Add(group.Key,Enumerable.Range(0,all.Length).Where(i=>!group.Any(root=>Inside(all[i],root.Node))).ToArray());
            }
            Transaction(plan.Roots.Select(c=>c.Source),()=>
            {
                foreach(var group in plan.Roots.GroupBy(c=>c.Source))foreach(var control in group.OrderByDescending(c=>c.Node.Start))
                    group.Key.Text=group.Key.Text.Substring(0,control.Node.Start)+group.Key.Text.Substring(control.Node.End);
            },"Ausgewählte Elemente entfernen",remap);
        }
    }
}
