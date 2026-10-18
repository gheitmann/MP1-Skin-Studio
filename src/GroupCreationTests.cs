using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunGroupCreationTests(string root)
        {
            Test("Grouping preserves complete child XML, ordering and exact undo/redo",()=>
            {
                var p=StructureFixture(root,"create-group-raw");string before=p.Controls[0].Source.Text;
                var roots=p.Controls.Where(c=>c.Node.Parent.Name=="controls").ToArray();string raw=before.Substring(roots[0].Node.Start,roots.Last().Node.End-roots[0].Node.Start);
                string key=p.CreateGroup(roots.Reverse());string after=p.Controls[0].Source.Text;
                Check(after.Contains(raw)&&p.Controls[0].Key==key&&p.Controls[0].Get("type")=="group"&&p.Controls.Select(c=>c.Name).SequenceEqual(new[]{"New group","A","G","Child","B"}),"Children changed or lost their ordering.");
                Check(p.Controls[1].RuntimeParent==p.Controls[0]&&Named(p,"Child").RuntimeParent==Named(p,"G")&&p.Controls.All(c=>!c.AutomaticLayout),"Nesting or layout protection is incorrect.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&!p.Dirty,"Undo does not restore original bytes.");p.RedoChange();Check(p.Controls[0].Source.Text==after,"Redo is not exact.");
            });
            Test("Grouping nested siblings preserves visible bounds, navigation and hiding",()=>
            {
                var p=GroupDemo();var names=new[]{"Open movies","Open series"};var preview=new StackPreview(p,new PreviewState(p));var bounds=names.ToDictionary(n=>n,n=>preview.Bounds(Named(p,n)));
                p.Editor.Hidden.Add(Named(p,"Open series").Key);p.CreateGroup(names.Select(n=>Named(p,n)));preview=new StackPreview(p,new PreviewState(p));
                Check(names.All(n=>preview.Bounds(Named(p,n))==bounds[n])&&p.IsEditorHidden(Named(p,"Open series"))&&!p.IsEditorHidden(Named(p,"Open movies")),"Bounds or hiding changed.");
                Check(Named(p,"Open movies").Get("id")=="310"&&Named(p,"Open series").Get("id")=="311","Existing control IDs changed.");
                p.UndoChange();Check(p.IsEditorHidden(Named(p,"Open series")),"Undo lost hidden state.");
            });
            Test("Group creation survives project save/load and identity restoration",()=>
            {
                var p=StructureFixture(root,"create-group-project");string before=p.Controls[0].Source.Text;p.Editor.Hidden.Add(Named(p,"Child").Key);
                string key=p.CreateGroup(new[]{Named(p,"A"),Named(p,"G")});p.Editor.Locked.Add(key);
                string file=Path.Combine(root,"create-group.mp1skinproj");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);
                Check(q.IsLocked(Named(q,"New group"))&&q.IsEditorHidden(Named(q,"Child")),"Reload lost group or child markers.");
                q.UndoChange();Check(q.Controls[0].Source.Text==before&&q.Editor.Locked.Count==0&&q.IsEditorHidden(Named(q,"Child")),"Undo assigns group markers to old controls.");
                q.RedoChange();Check(q.IsLocked(Named(q,"New group"))&&q.IsEditorHidden(Named(q,"Child")),"Redo lost identities.");
            });
            Test("Grouping rejects gaps, mixed parents, stale and single selections atomically",()=>
            {
                var p=StructureFixture(root,"create-group-invalid");string signature=ProjectFile.ContentSignature(p);
                Reject(()=>p.CreateGroup(new[]{Named(p,"A"),Named(p,"B")}));Reject(()=>p.CreateGroup(new[]{Named(p,"G"),Named(p,"Child")}));Reject(()=>p.CreateGroup(new[]{Named(p,"A")}));
                Check(signature==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Invalid selection changed the project.");
                var stale=Named(p,"A");p.Edit(stale,Set("label","Updated"),"Edit");Reject(()=>p.CreateGroup(new[]{stale,Named(p,"G")}));
            });
            Test("Locked descendants and automatic layouts block grouping",()=>
            {
                var p=StructureFixture(root,"create-group-locked");p.Editor.Locked.Add(Named(p,"Child").Key);Reject(()=>p.CreateGroup(new[]{Named(p,"A"),Named(p,"G")}));Check(!p.Dirty,"Locked group changed.");
                var q=GroupDemo();q.Edit(Named(q,"Actions"),Set("layout","StackLayout(7)"),"Layout");string signature=ProjectFile.ContentSignature(q);Reject(()=>q.CreateGroup(new[]{Named(q,"Open movies"),Named(q,"Open series")}));Check(signature==ProjectFile.ContentSignature(q),"Automatic layout changed.");
            });
            Test("Grouping respects import barriers and shared-source boundaries",()=>
            {
                var p=AuditFixture(root,"create-group-includes","<control><type>label</type><description>A</description></control><import>part.xml</import><control><type>label</type><description>B</description></control>");
                Write(p.Root,"part.xml","<window><controls><control><type>label</type><description>C</description></control><control><type>label</type><description>D</description></control></controls></window>");p.OpenPage("main.xml");
                Reject(()=>p.CreateGroup(new[]{Named(p,"A"),Named(p,"B")}));Reject(()=>p.CreateGroup(new[]{Named(p,"C"),Named(p,"D")}));Check(!p.Dirty,"Imports were reparented or shared XML changed.");
                p.OpenPage("part.xml");p.CreateGroup(p.Controls.ToArray());Check(p.Controls[0].Name=="New group","Included source cannot be grouped when opened directly.");
            });
            Test("Group creation rejects inherited behavior and reserves unused IDs",()=>
            {
                var p=StructureFixture(root,"create-group-defaults");Write(p.Root,"references.xml","<controls><control><type>group</type><visible>no</visible></control></controls>");p=new SkinProject(p.Root);p.OpenPage("main.xml");Reject(()=>p.CreateGroup(new[]{Named(p,"A"),Named(p,"G")}));Check(!p.Dirty,"Inherited visibility was silently applied.");
                var q=StructureFixture(root,"create-group-id");Write(q.Root,"other.xml","<window><controls><control><type>button</type><id>10000</id><onright>10001</onright></control></controls></window>");
                q.CreateGroup(new[]{Named(q,"A"),Named(q,"G")});Check(Named(q,"New group").Get("id")=="10002","New group ID collides with existing IDs or navigation references.");
            });
            Test("Grouping export leaves original XML unchanged",()=>
            {
                var p=StructureFixture(root,"create-group-export");string path=Path.Combine(p.Root,"main.xml");var before=File.ReadAllBytes(path);p.CreateGroup(new[]{Named(p,"A"),Named(p,"G")});p.Export(Path.Combine(root,"create-group.zip"));Check(before.SequenceEqual(File.ReadAllBytes(path)),"Original skin was overwritten.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunGroupCreationUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.groups.xml");SelectControl(GroupNamed("Actions"));SelectContainedLeaves();string before=selected.Source.Text;
            fields["label"].Text="Pending group label";bool blocked=false;try{GroupSelection();}catch(InvalidOperationException){blocked=true;}
            verify(blocked&&project.Undo.Count==0&&SelectedControls().Length==2,"Grouping preserves pending property edits and selection");RefreshProject(selected.Key);
            ShowProtectionMenu(protectButton);var menu=StudioMenus.PeekForTest(this,"protection");int index=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Group selected controls");menu.ChooseForTest(index);Application.DoEvents();
            verify(selected.Name=="New group"&&SelectedControls().Length==1&&NodeNamed("Open movies").Parent==NodeNamed("New group")&&NodeNamed("New group").Parent==NodeNamed("Actions"),"Group command creates nested structure and selects the new group");
            undo.PerformClick();verify(selected.Source.Text==before&&SelectedControls().Length==2&&SelectedControls().All(c=>c.Name=="Open movies"||c.Name=="Open series"),"Group undo restores exact XML and the original multiple selection");
            redo.PerformClick();verify(selected.Name=="New group"&&SelectedControls().Length==1,"Group redo restores the new group selection");undo.PerformClick();LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
