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
                string tooltip=control.Get("type")+" · ID "+control.Get("id","-")+" · "+control.Source.RelativePath+(context?" · Gruppe als Suchkontext":"")+" · Rechtsklick: Element";
                var leaf=new TreeNode(EditorMark(control)+(visible==false?"− ":visible==null?"? ":"")+(container?"[Gruppe] ":"")+DisplayName(control)){Tag=control,Name=context?"search-context":"",ToolTipText=tooltip,ForeColor=context?Color.FromArgb(141,177,211):visible==false?Color.FromArgb(108,125,145):ink};
                nodes.Add(control.Node,leaf);var parent=control.Node.Parent;while(parent!=null&&!nodes.ContainsKey(parent))parent=parent.Parent;
                if(parent==null)file.Nodes.Add(leaf);else nodes[parent].Nodes.Add(leaf);
            }
            file.ExpandAll();
        }
        void SelectContainedLeaves()
        {
            if(PendingProperties())throw new InvalidOperationException("Offene Eigenschaftseingaben zuerst übernehmen.");
            if(selected==null)throw new InvalidOperationException("Zuerst eine Gruppe auswählen.");
            var controls=project.ContainedLeaves(selected);if(controls.Length==0)throw new InvalidOperationException("Keine enthaltenen Einzel-Controls in dieser Quelldatei gefunden.");
            elementSearch.Clear();canvas.SelectedKeys.Clear();foreach(var c in controls)canvas.SelectedKeys.Add(c.Key);SelectControl(controls[0],false,true);
            ShowNotice(controls.Length+" enthaltene Einzel-Controls ausgewählt (gleiche XML-Datei). Sperren und Editor-Ausblendungen bleiben wirksam; automatische Layouts schützen die Geometrie.");
        }
        void AddGroupCommands(ContextMenuStrip menu)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Gruppenlayout bearbeiten …",null,(s,e)=>Attempt(EditStackLayout)).Enabled=SelectedControls().Length==1&&selected!=null&&selected.Get("type")=="group"&&!project.IsLocked(selected);
            menu.Items.Add("Enthaltene Einzel-Controls auswählen",null,(s,e)=>Attempt(SelectContainedLeaves)).Enabled=SelectedControls().Length==1&&selected!=null&&project.ContainedLeaves(selected).Length>0;
        }
        public void PrepareGroupPreview(){LoadProject(DemoRoot(),"demo.gruppen.xml");SelectControl(project.Controls.First(c=>c.Name=="Medienbereich"));}
    }
}
