using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject UngroupFixture(string root,string name,string extra="")
        {
            return AuditFixture(root,name,"<control><type>label</type><description>Before</description></control><control><type>group</type><id>10000</id><description><!--metadata comment-->Wrapper</description>"+extra+"<!--child comment--><control custom=\"keep\"><type>label</type><id>5</id><description>Leaf</description><posX>30</posX><unknown>keep</unknown></control><control><type>group</type><description>Nested</description><posX>50</posX><control><type>button</type><id>6</id><description>Deep</description></control></control><?keep source?></control><control><type>label</type><description>After</description></control>");
        }
        static void RunGroupRemovalTests(string root)
        {
            Test("Ungroup preserves nested child bytes, comments, order and exact undo/redo",()=>
            {
                var p=UngroupFixture(root,"ungroup-raw");string before=Named(p,"Wrapper").Source.Text;
                var raw=new[]{"Leaf","Nested"}.ToDictionary(n=>n,n=>{var c=Named(p,n);return c.Source.Text.Substring(c.Node.Start,c.Node.End-c.Node.Start);});
                string[] keys=p.UngroupControl(Named(p,"Wrapper"));string after=p.Controls[0].Source.Text;
                Check(p.Controls.Select(c=>c.Name).SequenceEqual(new[]{"Before","Leaf","Nested","Deep","After"})&&keys.SequenceEqual(new[]{Named(p,"Leaf").Key,Named(p,"Nested").Key})&&raw.All(pair=>after.Contains(pair.Value)),"Wrong promotion or altered child source.");
                Check(Named(p,"Leaf").RuntimeParent==null&&Named(p,"Deep").RuntimeParent==Named(p,"Nested")&&after.Contains("<!--metadata comment-->")&&after.Contains("<!--child comment-->")&&after.Contains("<?keep source?>"),"Comments, processing instructions or nested parents lost.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&!p.Dirty,"Undo did not restore exact source.");p.RedoChange();Check(p.Controls[0].Source.Text==after,"Redo changed source bytes.");
            });
            Test("Ungrouping newly created wrappers preserves preview geometry and child markers",()=>
            {
                var p=GroupDemo();p.CreateGroup(new[]{Named(p,"Open movies"),Named(p,"Open series")});var before=new StackPreview(p,new PreviewState(p));var bounds=before.Bounds(Named(p,"Open movies"));p.Editor.Hidden.Add(Named(p,"Open series").Key);
                p.UngroupControl(Named(p,"New group"));Check(Named(p,"Open movies").RuntimeParent==Named(p,"Actions")&&new StackPreview(p,new PreviewState(p)).Bounds(Named(p,"Open movies"))==bounds&&p.IsEditorHidden(Named(p,"Open series")),"Bounds, parent or hidden state changed.");
                p.UndoChange();Check(Named(p,"New group")!=null&&p.IsEditorHidden(Named(p,"Open series")),"Undo lost group or markers.");
            });
            Test("Ungroup survives saved projects with new and original group identities",()=>
            {
                var p=UngroupFixture(root,"ungroup-save");p.Editor.Hidden.Add(Named(p,"Deep").Key);string before=p.Controls[0].Source.Text;p.UngroupControl(Named(p,"Wrapper"));p.Editor.Locked.Add(Named(p,"After").Key);string path=Path.Combine(root,"ungroup.mp1skinproj");ProjectFile.Save(p,path);var q=ProjectFile.Load(path);
                q.UndoChange();Check(q.Controls[0].Source.Text==before&&q.IsEditorHidden(Named(q,"Deep"))&&q.IsLocked(Named(q,"After")),"Reload undo lost XML or reassigned flags.");q.RedoChange();Check(q.Controls.All(c=>c.Name!="Wrapper")&&q.IsEditorHidden(Named(q,"Deep")),"Reload redo lost state.");
                var created=GroupDemo();created.CreateGroup(new[]{Named(created,"Open movies"),Named(created,"Open series")});created.UngroupControl(Named(created,"New group"));string final=created.Controls[0].Source.Text;path=Path.Combine(root,"ungroup-new.mp1skinproj");ProjectFile.Save(created,path);q=ProjectFile.Load(path);Check(q.Controls.All(c=>c.Name!="New group")&&q.Controls[0].Source.Text==final,"Reload lost the final newly grouped/ungrouped snapshot.");q.UndoChange();q.RedoChange();Check(q.Controls[0].Source.Text==final,"Snapshot undo/redo lost the promoted children.");
            });
            Test("Ungroup rejects behavioral, styled, nonneutral and ambiguous groups atomically",()=>
            {
                foreach(string extra in new[]{"<layout>StackLayout(7)</layout>","<visible>yes</visible>","<animation effect=\"fade\">WindowOpen</animation>","<unknown />","<posX>40</posX>","<posX>#value</posX>","<id>11</id>","<layout custom=\"keep\" />"})
                {var p=UngroupFixture(root,"ungroup-reject-"+Guid.NewGuid().ToString("N"),extra);string before=ProjectFile.ContentSignature(p);Reject(()=>p.UngroupControl(Named(p,"Wrapper")));Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Rejected group changed.");}
                var hidden=UngroupFixture(root,"ungroup-hidden");var c=Named(hidden,"Wrapper");hidden.Editor.Hidden.Add(c.Key);Reject(()=>hidden.UngroupControl(c));Check(!hidden.Dirty,"Hidden wrapper changed.");
                var bound=UngroupFixture(root,"ungroup-bound-id");string path=Path.Combine(bound.Root,"main.xml");Write(bound.Root,"main.xml",File.ReadAllText(path).Replace("<id>10000</id>","<id>#group.id</id>"));bound=new SkinProject(bound.Root);bound.OpenPage("main.xml");Reject(()=>bound.UngroupControl(Named(bound,"Wrapper")));Check(!bound.Dirty,"Unresolved group ID was removed.");
            });
            Test("Ungroup rejects inherited group behavior and styled wrappers",()=>
            {
                var p=UngroupFixture(root,"ungroup-inherited");Write(p.Root,"references.xml","<controls><control><type>group</type><visible>yes</visible></control></controls>");p=new SkinProject(p.Root);p.OpenPage("main.xml");Reject(()=>p.UngroupControl(Named(p,"Wrapper")));Check(!p.Dirty,"Inherited group behavior was discarded.");
                var q=UngroupFixture(root,"ungroup-style");string path=Path.Combine(q.Root,"main.xml");Write(q.Root,"main.xml",File.ReadAllText(path).Replace("<control><type>group</type><id>10000", "<control Style=\"custom\"><type>group</type><id>10000"));q=new SkinProject(q.Root);q.OpenPage("main.xml");Reject(()=>q.UngroupControl(Named(q,"Wrapper")));Check(!q.Dirty,"Styled group was discarded.");
            });
            Test("Locked descendants, automatic ancestors and stale groups cannot be ungrouped",()=>
            {
                var p=UngroupFixture(root,"ungroup-lock");p.Editor.Locked.Add(Named(p,"Deep").Key);Reject(()=>p.UngroupControl(Named(p,"Wrapper")));Check(!p.Dirty,"Locked descendant was bypassed.");
                var q=GroupDemo();q.CreateGroup(new[]{Named(q,"Open movies"),Named(q,"Open series")});q.Edit(Named(q,"Actions"),Set("layout","GridLayout(2)"),"Layout");Reject(()=>q.UngroupControl(Named(q,"New group")));
                var r=UngroupFixture(root,"ungroup-stale");var group=Named(r,"Wrapper");r.Edit(group,Set("description","Updated"),"Rename");Reject(()=>r.UngroupControl(group));
            });
            Test("Ungroup blocks child, external page and unresolved condition references",()=>
            {
                foreach(string condition in new[]{"control.isvisible(10000)","control.isvisible(#unknown)","control.isvisible(#(iif(true,10000,3)))"})
                {var p=UngroupFixture(root,"ungroup-ref-"+Guid.NewGuid().ToString("N"));p.Edit(Named(p,"Leaf"),Set("visible",condition),"Condition");string before=ProjectFile.ContentSignature(p);Reject(()=>p.UngroupControl(Named(p,"Wrapper")));Check(before==ProjectFile.ContentSignature(p),"Referenced group changed.");}
                var q=UngroupFixture(root,"ungroup-external");Write(q.Root,"other.xml","<window><controls><control><type>button</type><onup>10000</onup></control></controls></window>");Reject(()=>q.UngroupControl(Named(q,"Wrapper")));Check(!q.Dirty,"External reference ignored.");
            });
            Test("Ungroup preserves original export files and rejects shared sources",()=>
            {
                var p=UngroupFixture(root,"ungroup-export");string path=Path.Combine(p.Root,"main.xml");byte[] original=File.ReadAllBytes(path);p.UngroupControl(Named(p,"Wrapper"));p.Export(Path.Combine(root,"ungroup.zip"));Check(original.SequenceEqual(File.ReadAllBytes(path)),"Original XML overwritten.");
                var q=UngroupFixture(root,"ungroup-include");Write(q.Root,"outer.xml","<window><controls><import>main.xml</import></controls></window>");q.OpenPage("outer.xml");Reject(()=>q.UngroupControl(Named(q,"Wrapper")));Check(!q.Dirty,"Included source changed indirectly.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunGroupRemovalUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.groups.xml");SelectControl(GroupNamed("Actions"));SelectContainedLeaves();GroupSelection();string grouped=selected.Source.Text;
            fields["posX"].Text="17";bool blocked=false;try{UngroupSelection();}catch(InvalidOperationException){blocked=true;}
            verify(blocked&&selected.Name=="New group"&&selected.Source.Text==grouped,"Ungroup protects pending property edits");RefreshProject(selected.Key);
            ShowProtectionMenu(protectButton);var menu=StudioMenus.PeekForTest(this,"protection");int index=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Ungroup selected group");menu.ChooseForTest(index);Application.DoEvents();
            verify(SelectedControls().Length==2&&NodeNamed("Open movies").Parent==NodeNamed("Actions")&&project.Controls.All(c=>c.Name!="New group"),"Ungroup menu promotes direct children and selects them");
            undo.PerformClick();verify(selected.Name=="New group"&&SelectedControls().Length==1&&selected.Source.Text==grouped,"Ungroup undo restores exact XML and group selection");
            redo.PerformClick();verify(SelectedControls().Length==2&&SelectedControls().All(c=>c.Name=="Open movies"||c.Name=="Open series"),"Ungroup redo restores promoted child selection");LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
