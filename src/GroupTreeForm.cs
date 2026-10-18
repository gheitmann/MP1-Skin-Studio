using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        static IEnumerable<TreeNode> TreeDescendants(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes){yield return node;foreach(var child in TreeDescendants(node.Nodes))yield return child;}
        }
        TreeNode[] TreeControlNodes(){return TreeDescendants(tree.Nodes).Where(n=>n.Tag is SkinControl).ToArray();}
        void AddTreeGroup(TreeNode file,SkinControl[] all,SkinControl[] matches)
        {
            var bySpan=all.ToDictionary(c=>c.Node);var include=new HashSet<ElementSpan>();var direct=new HashSet<string>(matches.Select(c=>c.Key));
            foreach(var control in matches)
                for(var node=control.Node;node!=null;node=node.Parent)if(bySpan.ContainsKey(node))include.Add(node);
            var nodes=new Dictionary<ElementSpan,TreeNode>();
            foreach(var control in all.Where(c=>include.Contains(c.Node)))
            {
                bool context=!direct.Contains(control.Key),container=LayoutTools.IsContainer(control);bool? visible=state.Visible(control);
                string tooltip=control.Get("type")+" · ID "+control.Get("id","-")+" · "+control.Source.RelativePath+(context?" · group as search context":"")+" · Right-click: Element";
                var leaf=new TreeNode(EditorMark(control)+(visible==false?"− ":visible==null?"? ":"")+(container?"[Group] ":"")+DisplayName(control)){Tag=control,Name=context?"search-context":"",ToolTipText=tooltip,ForeColor=context?Color.FromArgb(141,177,211):visible==false?Color.FromArgb(108,125,145):ink};
                nodes.Add(control.Node,leaf);var parent=control.Node.Parent;while(parent!=null&&!nodes.ContainsKey(parent))parent=parent.Parent;
                if(parent==null)file.Nodes.Add(leaf);else nodes[parent].Nodes.Add(leaf);
            }
            file.ExpandAll();
        }
        void SelectContainedLeaves()
        {
            if(PendingProperties())throw new InvalidOperationException("Apply pending property edits first.");
            if(selected==null)throw new InvalidOperationException("Select a group first.");
            var controls=project.ContainedLeaves(selected);if(controls.Length==0)throw new InvalidOperationException("No individual child controls found in this source file.");
            elementSearch.Clear();canvas.SelectedKeys.Clear();foreach(var c in controls)canvas.SelectedKeys.Add(c.Key);SelectControl(controls[0],false,true);
            ShowNotice(controls.Length+" individual child controls selected (same XML file). Locks and editor hiding remain effective; automatic layouts protect geometry.");
        }
        void AddGroupCommands(ContextMenuStrip menu)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Group selected controls",null,(s,e)=>Attempt(GroupSelection)).Enabled=SelectedControls().Length>=2&&!SelectedControls().Any(c=>project.IsLocked(c));
            menu.Items.Add("Edit group layout …",null,(s,e)=>Attempt(EditStackLayout)).Enabled=SelectedControls().Length==1&&selected!=null&&selected.Get("type")=="group"&&!project.IsLocked(selected);
            menu.Items.Add("Edit grid layout ...",null,(s,e)=>Attempt(EditGridLayout)).Enabled=SelectedControls().Length==1&&selected!=null&&selected.Get("type")=="group"&&!project.IsLocked(selected);
            menu.Items.Add("Select individual child controls",null,(s,e)=>Attempt(SelectContainedLeaves)).Enabled=SelectedControls().Length==1&&selected!=null&&project.ContainedLeaves(selected).Length>0;
        }
        public void PrepareGroupPreview(){LoadProject(DemoRoot(),"demo.groups.xml");SelectControl(project.Controls.First(c=>c.Name=="Media section"));}
    }
}
