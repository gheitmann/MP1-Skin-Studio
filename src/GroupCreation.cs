using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        SkinControl[] GroupCandidates(IEnumerable<SkinControl> selection)
        {
            var controls=selection.Select(RequireCurrent).GroupBy(c=>c.Key).Select(g=>g.First()).OrderBy(c=>c.Node.Start).ToArray();
            if(controls.Length<2)throw new InvalidOperationException("Select at least two neighboring controls to create a group.");
            var source=controls[0].Source;var parent=controls[0].Node.Parent;
            if(!String.Equals(source.RelativePath,Relative(Resolve(Page)),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Open the included source as a page before grouping its controls.");
            if(parent==null||(parent.Name!="controls"&&parent.Name!="control")||controls.Any(c=>c.Source!=source||c.Node.Parent!=parent))throw new InvalidOperationException("Grouping requires controls with the same XML parent and source file.");
            if(parent.Name=="control"&&Controls.Any(c=>c.Source==source&&c.Node==parent&&c.Get("type")!="group"))throw new InvalidOperationException("Only ordinary groups can contain a new group.");
            foreach(var c in controls)
            {
                GuardStructural(c);
                if(c.AutomaticLayout)throw new InvalidOperationException("Grouping inside an automatic layout would change its arrangement.");
            }
            var between=parent.Children.Where(n=>n.Start>=controls[0].Node.Start&&n.End<=controls.Last().Node.End).ToArray();
            if(between.Length!=controls.Length||between.Any(n=>!controls.Any(c=>c.Node==n)))throw new InvalidOperationException("Select consecutive XML siblings; do not skip controls, imports or definitions.");
            // A type default could add visibility, animations, layout or other
            // behavior to the wrapper. Do not silently inherit such semantics.
            ElementSpan defaultsForGroup;
            if(defaults.TryGetValue("group",out defaultsForGroup)&&defaultsForGroup.Children.Any(n=>n.Name!="type"&&n.Name!="id"&&n.Name!="description"&&n.Name!="posX"&&n.Name!="posY"&&n.Name!="width"&&n.Name!="height"&&n.Name!="layout"))throw new InvalidOperationException("This skin defines group behavior in references.xml. Group creation requires a neutral group default.");
            return controls;
        }
        public string CreateGroup(IEnumerable<SkinControl> selection)
        {
            var controls=GroupCandidates(selection);var source=controls[0].Source;
            int start=controls[0].Node.Start,end=controls.Last().Node.End,ordinal=controls[0].Ordinal,id=NextControlId();
            string newline=source.Text.Contains("\r\n")?"\r\n":"\n";
            int line=source.Text.LastIndexOf('\n',Math.Max(0,start-1));string indent=source.Text.Substring(line+1,start-line-1);
            if(indent.Any(ch=>ch!=' '&&ch!='\t'))indent="";
            string raw="<control>"+newline+indent+"  <type>group</type><id>"+id.ToString(CultureInfo.InvariantCulture)+"</id><description>New group</description>"+newline+indent+"  <posX>0</posX><posY>0</posY><width>0</width><height>0</height><layout />"+newline+indent;
            // Preserve every child byte and all comments between selected nodes.
            raw+=source.Text.Substring(start,end-start)+newline+indent+"</control>";
            var remap=new ControlKeyRemap();remap.Add(source,Enumerable.Range(0,ordinal).Concat(new[]{-1}).Concat(Enumerable.Range(ordinal,source.OriginalOrdinals.Count-ordinal)).ToArray());
            Transaction(new[]{source},()=>source.Text=source.Text.Substring(0,start)+raw+source.Text.Substring(end),"Group selected controls",remap);
            return source.RelativePath+"|"+ordinal;
        }
    }
    public sealed partial class StudioForm
    {
        void GroupSelection()
        {
            string key=null;
            if(!StructuralAction(()=>key=project.CreateGroup(SelectedControls())))return;
            RestoreSelection(new EditorSelection{Primary=key,Keys=new[]{key}});
            project.Undo.Peek().SelectionAfter=CaptureSelection();
            ShowNotice("Group created. Child XML and positions are preserved; undo restores the original structure.");
        }
    }
}
