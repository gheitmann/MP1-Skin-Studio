using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject StructureFixture(string root,string name)
        {
            return AuditFixture(root,name,"<!--outside--><control custom=\"keep\"><description>A</description><type>label</type><id>1</id><label>A</label><!--inside--></control>\n<control><description>G</description><type>group</type><id>2</id><control><description>Child</description><type>label</type><id>0</id><label>Kind</label></control></control>\n<control><description>B</description><type>button</type><id>3</id><label>B</label></control>");
        }
        static void RunStructuralTests(string root)
        {
            RunGroupDuplicationTests(root);
            Test("Projects with fully cleared sources support opening, undo and redo",()=>
            {
                var p=ResetFixture(root,"structure-empty-project");p.DeleteControls(p.Controls,false,true);string file=Path.Combine(root,"structure-empty.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(loaded.Controls.Count==0,"Empty state was not restored.");loaded.UndoChange();Check(loaded.Controls.Count==2&&!loaded.Dirty,"Empty sources were not restored.");loaded.RedoChange();Check(loaded.Controls.Count==0,"Incorrect redo of empty sources.");
            });
            Test("Replacement controls at the same ordinal position do not inherit original identities or locks",()=>
            {
                var p=AuditFixture(root,"structure-replacement","<control><type>label</type><id>0</id><description>Original</description></control>");p.DeleteControls(p.Controls);string key=p.InsertControl("main.xml","button");p.Editor.Locked.Add(key);string file=Path.Combine(root,"structure-replacement.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls[0].Name=="Original"&&!loaded.IsLocked(loaded.Controls[0]),"New identity lock was assigned to the original.");loaded.RedoChange();Check(loaded.Controls[0].Get("type")=="button"&&loaded.IsLocked(loaded.Controls[0]),"New identity or lock was lost during redo.");
            });
            Test("Stale control objects cannot edit other controls after structural changes",()=>
            {
                var p=StructureFixture(root,"structure-stale");var old=Named(p,"A");p.ReorderControl(old,"front");string signature=ProjectFile.ContentSignature(p);
                Reject(()=>p.Edit(old,Set("label","Falsch"),"stale"));Reject(()=>p.ResetProperty(new[]{old},"label",false));Reject(()=>p.DuplicateControl(old,false));
                Check(signature==ProjectFile.ContentSignature(p)&&Named(p,"A").Get("label")=="A","Stale selection changed another control.");
            });
            Test("Layer changes preserve raw controls, extensions, IDs and exact undo/redo",()=>
            {
                var p=StructureFixture(root,"structure-order");string before=p.Controls[0].Source.Text;var raw=p.Controls.Where(c=>c.Node.Parent.Name=="controls").ToDictionary(c=>c.Name,c=>c.Source.Text.Substring(c.Node.Start,c.Node.End-c.Node.Start));
                p.ReorderControl(Named(p,"A"),"front");string after=p.Controls[0].Source.Text;
                Check(p.Controls.Select(c=>c.Name).SequenceEqual(new[]{"G","Child","B","A"})&&raw.All(pair=>after.Contains(pair.Value))&&after.Contains("<!--outside-->"),"Raw text or ordering was lost.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&!p.Dirty,"Undo is not exact.");p.RedoChange();Check(p.Controls[0].Source.Text==after,"Redo is not exact.");
            });
            Test("Locks and hiding follow reordered controls and nested children",()=>
            {
                var p=StructureFixture(root,"structure-flags");p.Editor.Locked.Add(Named(p,"Child").Key);p.Editor.Hidden.Add(Named(p,"B").Key);
                p.ReorderControl(Named(p,"A"),"front");Check(p.IsLocked(Named(p,"Child"))&&!p.IsLocked(Named(p,"A"))&&p.IsEditorHidden(Named(p,"B")),"Marker is on the wrong control.");
                p.UndoChange();Check(p.IsLocked(Named(p,"Child"))&&p.IsEditorHidden(Named(p,"B")),"Undo loses markers.");
            });
            Test("Markers added later are remapped during undo rather than overwritten",()=>
            {
                var p=StructureFixture(root,"structure-later-flags");p.ReorderControl(Named(p,"G"),"front");p.Editor.Hidden.Add(Named(p,"Child").Key);p.Editor.Locked.Add(Named(p,"B").Key);p.UndoChange();
                Check(p.IsEditorHidden(Named(p,"Child"))&&p.IsLocked(Named(p,"B"))&&!p.IsLocked(Named(p,"G")),"Later setting was reset to an earlier full state.");
            });
            Test("Locked children block group changes without partial edits",()=>
            {
                var p=StructureFixture(root,"structure-locked-group");p.Editor.Locked.Add(Named(p,"Child").Key);string before=ProjectFile.ContentSignature(p);
                Reject(()=>p.ReorderControl(Named(p,"G"),"front"));Reject(()=>p.DeleteControls(new[]{Named(p,"G")},true,true));Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Group protection was bypassed.");
            });
            Test("Layer boundaries and include barriers are no-ops without extra undo steps",()=>
            {
                var p=AuditFixture(root,"structure-boundary","<control><type>label</type><description>A</description></control><import>part.xml</import><control><type>label</type><description>B</description></control>");Write(p.Root,"part.xml","<window><controls/></window>");p.OpenPage("main.xml");p.ReorderControl(Named(p,"A"),"front");
                Check(!p.Dirty&&p.Undo.Count==0,"Moved across an include boundary.");
                var q=StructureFixture(root,"structure-modes");q.ReorderControl(Named(q,"A"),"forward");Check(q.Controls.First().Name=="G","Incorrect single-layer forward movement.");q.ReorderControl(Named(q,"A"),"backward");Check(q.Controls.First().Name=="A","Incorrect single-layer backward movement.");q.ReorderControl(Named(q,"B"),"back");Check(q.Controls.First().Name=="B","Incorrect send-to-back operation.");
            });
            Test("Group removal counts children and restores XML and hidden state",()=>
            {
                var p=StructureFixture(root,"structure-delete-group");string before=p.Controls[0].Source.Text;p.Editor.Hidden.Add(Named(p,"Child").Key);var plan=p.InspectDeletion(new[]{Named(p,"G"),Named(p,"Child")});
                Check(plan.Roots.Length==1&&plan.RemovedKeys.Count==2,"Parent/child selection was counted twice.");p.DeleteControls(plan.Roots);Check(p.Controls.Count==2&&p.Editor.Hidden.Count==0,"Group or orphaned markers remain.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&p.IsEditorHidden(Named(p,"Child")),"Restoration is incomplete.");p.RedoChange();Check(p.Controls.Count==2,"Redo does not remove the group.");
            });
            Test("Reference checks find navigation and visibility while ignoring jointly removed references",()=>
            {
                var p=AuditFixture(root,"structure-refs","<control><description>A</description><type>button</type><id>10</id><onright>11</onright></control><control><description>B</description><type>button</type><id>11</id><onleft>10</onleft><visible>control.hasfocus(10)</visible></control>");
                var plan=p.InspectDeletion(new[]{Named(p,"A")});Check(plan.References.Count==2,"Navigation or visibility reference is missing.");Reject(()=>p.DeleteControls(new[]{Named(p,"A")}));Check(!p.Dirty,"Reference protection changes XML.");
                Check(p.InspectDeletion(p.Controls).References.Count==0,"Internal references were reported as remaining.");p.DeleteControls(p.Controls);Check(p.Controls.Count==0,"Batch removal is incomplete.");
            });
            Test("Include removal requires approval and remains a cross-file transaction",()=>
            {
                var p=ResetFixture(root,"structure-shared");var plan=p.InspectDeletion(p.Controls);Check(plan.SharedFiles.SequenceEqual(new[]{"part.xml"}),"Include is not identified.");Reject(()=>p.DeleteControls(p.Controls));Check(!p.Dirty,"Partial change occurred before approval.");p.DeleteControls(p.Controls,false,true);Check(p.Controls.Count==0&&p.Undo.Count==1,"No shared transaction.");p.UndoChange();Check(p.Controls.Count==2&&!p.Dirty,"Shared undo is incomplete.");
            });
            Test("Insertion undo removes markers and redo restores them on the new control",()=>
            {
                var p=StructureFixture(root,"structure-insert-flags");string key=p.InsertControl("main.xml","label");p.Editor.Hidden.Add(key);p.Editor.Locked.Add(key);p.UndoChange();Check(p.Editor.Hidden.Count==0&&p.Editor.Locked.Count==0,"Orphaned marker remains after undo.");p.RedoChange();Check(p.IsLocked(p.Controls.First(c=>c.Key==key)),"Redo loses the marker.");p.UndoChange();string next=p.InsertControl("main.xml","button");Check(!p.IsLocked(p.Controls.First(c=>c.Key==next)),"Marker was transferred to another new control.");
            });
            Test("Saved layer order preserves identities and markers during restoration undo",()=>
            {
                var p=StructureFixture(root,"structure-project-order");p.Editor.Hidden.Add(Named(p,"Child").Key);p.ReorderControl(Named(p,"A"),"front");p.Editor.Locked.Add(Named(p,"B").Key);string file=Path.Combine(root,"structure-order.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.IsLocked(Named(loaded,"B")),"Project state differs.");loaded.UndoChange();Check(loaded.Controls.First().Name=="A"&&loaded.IsLocked(Named(loaded,"B"))&&loaded.IsEditorHidden(Named(loaded,"Child")),"Undo mixes element identities.");loaded.RedoChange();Check(loaded.Controls.Last().Name=="A"&&loaded.IsLocked(Named(loaded,"B")),"Redo mixes identities.");
            });
            Test("Saved deletion remaps remaining locks correctly during undo and redo",()=>
            {
                var p=StructureFixture(root,"structure-project-delete");p.Editor.Locked.Add(Named(p,"B").Key);p.DeleteControls(new[]{Named(p,"A")});string file=Path.Combine(root,"structure-delete.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls.First().Name=="A"&&loaded.IsLocked(Named(loaded,"B"))&&!loaded.IsLocked(Named(loaded,"Child")),"Lock was transferred to the wrong element.");loaded.RedoChange();Check(loaded.Controls.Count==3&&loaded.IsLocked(Named(loaded,"B")),"Redo loses the lock.");
            });
            Test("Legacy projects without element mappings load appended controls compatibly",()=>
            {
                var p=StructureFixture(root,"structure-legacy");p.InsertControl("main.xml","label");string file=Path.Combine(root,"structure-legacy.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","1");foreach(XmlElement node in doc.SelectNodes("/skinStudioProject/files/file"))node.RemoveAttribute("controlOrder");return doc.OuterXml;});var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls.Count==4&&!loaded.Dirty,"Legacy project is incompatible.");
            });
            Test("Project formats 2 and 3 require complete element mappings",()=>
            {
                var p=StructureFixture(root,"structure-missing-order");p.ReorderControl(Named(p,"A"),"front");string file=Path.Combine(root,"structure-missing.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);foreach(XmlElement node in doc.SelectNodes("/skinStudioProject/files/file"))node.RemoveAttribute("controlOrder");return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
            Test("Corrupted element mappings are rejected before loading projects",()=>
            {
                var p=StructureFixture(root,"structure-invalid-order");p.ReorderControl(Named(p,"A"),"front");string file=Path.Combine(root,"structure-invalid.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);((XmlElement)doc.SelectSingleNode("/skinStudioProject/files/file")).SetAttribute("controlOrder","0,0,2,3");return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunStructuralUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));string before=selected.Source.Text,oldKey=selected.Key;
            project.Editor.Hidden.Add(project.Controls.First(c=>c.Get("description")=="Series").Key);project.Editor.Locked.Add(project.Controls.First(c=>c.Get("description")=="Music").Key);
            var menu=(StudioMenu)ProtectionMenu();menu.Show(tree,new System.Drawing.Point(12,12));int command=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Move forward one layer");menu.ChooseForTest(command);Application.DoEvents();
            verify(selected.Name=="Movies"&&selected.Key!=oldKey&&project.IsEditorHidden(project.Controls.First(c=>c.Get("description")=="Series"))&&project.IsLocked(project.Controls.First(c=>c.Get("description")=="Music")),"Layer menus keep selection and editor markers on the correct element");
            undo.PerformClick();verify(selected.Name=="Movies"&&selected.Key==oldKey&&selected.Source.Text==before,"Layer undo restores XML and the original selection");redo.PerformClick();verify(selected.Name=="Movies"&&selected.Key!=oldKey,"Layer redo selects the same logical element again");
            fields["width"].Text="490";Apply();verify(selected.Get("width")=="490"&&project.Controls.First(c=>c.Get("description")=="Series").Get("width")=="470","Property editing after layer changes still targets the selected control");undo.PerformClick();undo.PerformClick();
            fields["posY"].Text="640";bool blocked=false;try{ReorderSelection("front");}catch(InvalidOperationException){blocked=true;}verify(blocked&&fields["posY"].Text=="640"&&!project.Dirty,"Layer changes preserve pending property edits");
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));before=selected.Source.Text;var plan=project.InspectDeletion(SelectedControls());
            using(var dialog=new DeleteControlsDialog(plan)){verify(!dialog.RemovalEnabled,"Removal involving references and includes starts without approval");dialog.ApproveForTest();verify(dialog.RemovalEnabled,"Explicit approvals enable removal");}
            bool reviewed=false;using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<DeleteControlsDialog>().FirstOrDefault();if(dialog!=null){timer.Stop();reviewed=true;dialog.ApproveForTest();dialog.AcceptForTest();}};timer.Start();
                menu=(StudioMenu)ProtectionMenu();menu.Show(tree,new System.Drawing.Point(12,12));command=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Remove selection",StringComparison.OrdinalIgnoreCase));menu.ChooseForTest(command);timer.Stop();
            }
            verify(reviewed&&!project.Controls.Any(c=>c.Get("description")=="Movies")&&selected.Get("description")=="Series"&&!menu.IsDisposed,"Removal through menus and audit dialogs selects the next element");
            undo.PerformClick();verify(selected.Name=="Movies"&&selected.Source.Text==before&&project.Controls.First(c=>c.Get("description")=="Series").Get("onleft")=="10","Removal undo restores selection, complete XML and unchanged references");redo.PerformClick();verify(selected.Name=="Series"&&!project.Controls.Any(c=>c.Name=="Movies"),"Removal redo restores the post-removal selection");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
