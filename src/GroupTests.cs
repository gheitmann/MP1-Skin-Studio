using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject GroupDemo(){var p=Demo();p.OpenPage("demo.groups.xml");return p;}
        static void RunGroupTests(string root)
        {
            Test("Group contents contain unique leaf controls without group frames",()=>
            {var p=GroupDemo();var leaves=p.ContainedLeaves(Named(p,"Media section"));Check(leaves.Length==4&&leaves.All(c=>!LayoutTools.IsContainer(c))&&p.ContainedLeaves(Named(p,"Actions")).Length==2&&!p.Dirty&&p.Undo.Count==0,"Leaf selection changes structure or includes group frames.");});
            Test("Free siblings within a group support atomic alignment",()=>
            {var p=GroupDemo();string before=Named(p,"Actions").Source.Text;p.EditMany(LayoutTools.Align(p.ContainedLeaves(Named(p,"Actions")),"left",false),"links");Check(Named(p,"Open series").Get("posX")=="330"&&Named(p,"Actions").Get("posX")=="300","Group frame or incorrect position was changed.");p.UndoChange();Check(Named(p,"Actions").Source.Text==before,"Group alignment was not undone exactly.");});
            Test("Group frames and controls with different nested parents cannot be aligned",()=>
            {var p=GroupDemo();Reject(()=>LayoutTools.Align(new[]{Named(p,"Media section"),Named(p,"Actions")},"left",false));Reject(()=>LayoutTools.Align(p.ContainedLeaves(Named(p,"Media section")),"left",false));Check(!p.Dirty,"Rejected alignment changes the project.");});
            Test("Automatic layouts protect deeply nested controls",()=>
            {var p=GroupDemo();p.Edit(Named(p,"Media section"),Set("layout","StackLayout"),"Layout");Check(p.ContainedLeaves(Named(p,"Media section")).All(c=>c.AutomaticLayout),"Layout protection was not inherited.");Reject(()=>LayoutTools.Align(p.ContainedLeaves(Named(p,"Actions")),"left",false));p.UndoChange();Check(p.Controls.All(c=>!c.AutomaticLayout),"Layout protection is stale after undo.");});
            Test("Style layouts and include contents inherit automatic-position protection",()=>
            {
                string folder=Path.Combine(root,"group-layout-style");Directory.CreateDirectory(folder);Write(folder,"references.xml","<controls><style Name=\"auto\"><layout>StackLayout</layout></style></controls>");
                Write(folder,"part.xml","<window><controls><control><type>button</type><id>7</id></control></controls></window>");Write(folder,"main.xml","<window><controls><control Style=\"auto\"><type>group</type><id>5</id><control><type>label</type><id>6</id></control><import>part.xml</import></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");var included=p.Controls.Single(c=>c.Get("id")=="7");Check(included.AutomaticLayout,"Import loses layout protection.");Reject(()=>LayoutTools.RequireManualGeometry(included));
            });
            Test("Content selection rejects stale group objects",()=>
            {var p=GroupDemo();var group=Named(p,"Media section");p.Edit(group,Set("description","Neu"),"Name");Reject(()=>p.ContainedLeaves(group));Check(p.ContainedLeaves(Named(p,"Neu")).Length==4,"Current group cannot be selected.");});
            Test("Repeated XML controls retain layout protection in every active use",()=>
            {
                string folder=Path.Combine(root,"group-layout-reuse");Directory.CreateDirectory(folder);Write(folder,"part.xml","<window><controls><control><type>button</type><id>7</id></control></controls></window>");Write(folder,"main.xml","<window><controls><import>part.xml</import><control><type>group</type><layout>StackLayout</layout><control><type>label</type><id>6</id></control><import>part.xml</import></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");var reused=p.Controls.Where(c=>c.Get("id")=="7").ToArray();Check(reused.Length==2&&reused.All(c=>c.AutomaticLayout),"First free include bypasses layout protection of the same XML source.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        SkinControl GroupNamed(string name){return project.Controls.First(c=>c.Name==name);}
        TreeNode NodeNamed(string name){return TreeControlNodes().First(n=>((SkinControl)n.Tag).Name==name);}
        void RunGroupUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.groups.xml");var media=GroupNamed("Media section");string before=media.Source.Text;
            verify(NodeNamed("Open movies").Parent==NodeNamed("Actions")&&NodeNamed("Actions").Parent==NodeNamed("Media section")&&TreeControlNodes().Length==7,"Group trees show XML nesting without missing or duplicate controls");
            SelectControl(media);canvas.Update();canvas.ExerciseDrag(media,40,30,true);verify(!project.Dirty&&project.Undo.Count==0&&status.Text.Contains("group frames"),"Group frames cannot accidentally be resized as individual rectangles");
            fields["width"].Text="1600";bool blocked=false;try{SelectContainedLeaves();}catch(InvalidOperationException){blocked=true;}verify(blocked&&fields["width"].Text=="1600"&&selected.Key==media.Key,"Content selection preserves pending property edits");fields["width"].Text="1500";
            elementSearch.Text="id:310";verify(SearchResults().Length==1&&TreeControlNodes().Length==3&&NodeNamed("Actions").Name=="search-context","Search matches retain the full ancestor group chain without counting it as matches");
            NextSearchResult(false);verify(selected.Name=="Open movies"&&tree.SelectedNode==NodeNamed("Open movies")&&NodeNamed("Actions").Text.Contains("[Group]"),"Match selection reaches deeply nested controls and preserves group markers");
            SelectFoundControl(GroupNamed("Media section"));ShowProtectionMenu(protectButton);var menu=StudioMenus.PeekForTest(this,"protection");int index=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Select individual child controls");menu.ChooseForTest(index);Application.DoEvents();
            verify(SelectedControls().Length==4&&SelectedControls().All(c=>!LayoutTools.IsContainer(c))&&elementSearch.Text.Length==0&&!project.Dirty&&project.Undo.Count==0,"Real group-content menus clear search filters without XML edits or undo steps");
            verify(TreeControlNodes().Where(n=>canvas.SelectedKeys.Contains(((SkinControl)n.Tag).Key)).All(n=>n.BackColor.ToArgb()==System.Drawing.Color.FromArgb(33,72,102).ToArgb()),"Multiple selection highlights deeply nested tree nodes");
            canvas.Update();canvas.ExerciseDrag(GroupNamed("Panel"),40,30,false);verify(GroupNamed("Panel").Get("posX")=="250"&&GroupNamed("Open movies").Get("posX")=="370"&&GroupNamed("Open series").Get("posY")=="680"&&GroupNamed("Media section").Get("posX")=="210"&&project.Undo.Count==1,"Selected leaf controls move together while group frames remain unchanged");undo.PerformClick();verify(GroupNamed("Media section").Source.Text==before&&SelectedControls().Length==4,"Undoing content movement preserves complete XML and multiple selection");
            SelectControl(GroupNamed("Actions"));SelectContainedLeaves();AlignSelection("left");verify(GroupNamed("Open series").Get("posX")=="330","Free sibling controls within groups align through the UI");undo.PerformClick();
            SelectControl(GroupNamed("Media section"));SetProtection(true,true);SelectContainedLeaves();verify(SelectedControls().All(c=>project.IsLocked(c))&&fields.Values.All(f=>f.ReadOnly),"Content selection respects inherited group locks");
            SelectControl(GroupNamed("Media section"));SetProtection(true,false);project.Edit(selected,new System.Collections.Generic.Dictionary<string,string>{{"layout","StackLayout"}},"Layout");RefreshProject(selected.Key);SelectControl(GroupNamed("Actions"));SelectContainedLeaves();canvas.Update();string layoutXml=selected.Source.Text;int steps=project.Undo.Count;canvas.ExerciseDrag(selected,40,20,false);verify(selected.Source.Text==layoutXml&&project.Undo.Count==steps&&status.Text.Contains("layout rules"),"Dragging within automatically arranged groups changes no controls");
            undo.PerformClick();SelectControl(GroupNamed("Actions"));StructuralAction(()=>project.DeleteControls(new[]{selected}));verify(!TreeControlNodes().Any(n=>((SkinControl)n.Tag).Name=="Actions")&&TreeControlNodes().Length==4,"Removing subgroups removes their complete tree chain");undo.PerformClick();verify(NodeNamed("Open movies").Parent==NodeNamed("Actions")&&GroupNamed("Media section").Source.Text==before,"Undo restores nested tree structure and complete source text");
            elementSearch.Text="id:311";SelectFoundControl(GroupNamed("Open series"));project.Editor.Hidden.Add(selected.Key);RefreshProject(selected.Key);verify(NodeNamed("Open series").Text.StartsWith("[H]")&&NodeNamed("Actions").Name=="search-context","Editor status updates also reach filtered deep tree nodes");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
