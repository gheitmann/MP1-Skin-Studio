using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunWorkflowToolsTests(string root)
        {
            Test("Position-only protection permits size and text edits",()=>{
                var p=SessionFixture(root,"position-only");string key=p.Controls[0].Key;p.Editor.PositionLocked.Add(key);string xml=ProjectFile.XmlSignature(p);
                Reject(()=>p.Edit(p.Controls[0],Set("posX","777"),"blocked"));Check(ProjectFile.XmlSignature(p)==xml,"Rejected position changed XML.");
                p.Edit(p.Controls[0],Set("width","444"),"size allowed");p.Edit(p.Controls[0],Set("label","Allowed"),"text allowed");Check(p.Controls[0].Get("width")=="444"&&p.IsPositionLocked(p.Controls[0])&&!p.IsSizeLocked(p.Controls[0]),"Position lock blocked unrelated fields.");
                Reject(()=>p.ResetProperty(new[]{p.Controls[0]},"posX",true));
            });
            Test("Size-only protection permits moving and rejects atomic mixed batches",()=>{
                var p=SessionFixture(root,"size-only");p.Editor.SizeLocked.Add(p.Controls[1].Key);string xml=ProjectFile.XmlSignature(p);
                Reject(()=>p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("width","444")),new ControlEdit(p.Controls[1],Set("height","555"))},"blocked batch"));Check(ProjectFile.XmlSignature(p)==xml&&p.Undo.Count==0,"Protected batch partially committed.");
                p.Edit(p.Controls[1],Set("posX","333"),"move allowed");Check(p.Controls[1].Get("posX")=="333"&&!p.IsPositionLocked(p.Controls[1]),"Size lock blocked movement.");
            });
            Test("Geometry locks inherit from groups",()=>{
                var p=StructureFixture(root,"geometry-inherited");p.Editor.PositionLocked.Add(Named(p,"G").Key);Check(p.IsPositionLocked(Named(p,"Child"))&&!p.IsSizeLocked(Named(p,"Child")),"Group position lock did not inherit independently.");
                Reject(()=>p.Edit(Named(p,"Child"),Set("posY","777"),"blocked"));p.Editor.PositionLocked.Clear();p.Editor.SizeLocked.Add(Named(p,"G").Key);Reject(()=>p.Edit(Named(p,"Child"),Set("width","777"),"blocked"));
            });
            Test("Geometry flags follow structure through undo redo and project reload",()=>{
                var p=StructureFixture(root,"geometry-remap");p.Editor.PositionLocked.Add(Named(p,"Child").Key);p.Editor.SizeLocked.Add(Named(p,"B").Key);p.ReorderControl(Named(p,"A"),"front");
                p.UndoChange();Check(p.IsPositionLocked(Named(p,"Child"))&&p.IsSizeLocked(Named(p,"B"))&&!p.IsPositionLocked(Named(p,"A")),"Undo misplaced geometry markers.");p.RedoChange();
                string file=Path.Combine(root,"geometry-remap.mp1skinproj");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);q.UndoChange();q.RedoChange();Check(q.IsPositionLocked(Named(q,"Child"))&&q.IsSizeLocked(Named(q,"B")),"Reload misplaced geometry markers.");
            });
            Test("Grid choices and independent locks roundtrip without XML mutation",()=>{
                var p=SessionFixture(root,"grid-state");string xml=ProjectFile.XmlSignature(p);p.Editor.SnapSpacing=25;p.Editor.SnapEnabled=false;p.Editor.GridVisible=false;p.Editor.PositionLocked.Add(p.Controls[0].Key);p.Editor.SizeLocked.Add(p.Controls[1].Key);
                string file=Path.Combine(root,"grid-state.mp1skinproj");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);Check(q.Editor.SnapSpacing==25&&!q.Editor.SnapEnabled&&!q.Editor.GridVisible&&ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(q)&&ProjectFile.XmlSignature(q)==xml&&!q.Dirty,"Grid state or independent flags changed XML or failed to restore.");
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","4");var state=(XmlElement)doc.SelectSingleNode("/skinStudioProject/editorState");foreach(string name in new[]{"snapSpacing","snapEnabled","gridVisible"})state.RemoveAttribute(name);foreach(XmlElement item in state.SelectNodes("control")){item.RemoveAttribute("positionLocked");item.RemoveAttribute("sizeLocked");}return doc.OuterXml;});
                var legacy=ProjectFile.Load(file);Check(legacy.Editor.SnapSpacing==10&&legacy.Editor.SnapEnabled&&legacy.Editor.GridVisible&&!legacy.IsPositionLocked(legacy.Controls[0]),"Old format defaults are incompatible.");
            });
            Test("Invalid grid settings and persisted booleans are rejected",()=>{
                var p=SessionFixture(root,"grid-invalid");p.Editor.SnapSpacing=0;Reject(()=>ProjectFile.Save(p,Path.Combine(root,"invalid-grid.mp1skinproj")));p.Editor.SnapSpacing=10;
                string file=Path.Combine(root,"bad-grid.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>text.Replace("snapEnabled=\"true\"","snapEnabled=\"unknown\""));Reject(()=>ProjectFile.Load(file));
            });
            Test("Positioned insertion and image properties form one source-preserving undo step",()=>{
                var p=SessionFixture(root,"drop-insert");string xml=ProjectFile.XmlSignature(p);string key=p.InsertControlAt("main.xml","image",125,250,"Studio/Picture.png",300,180);var c=p.Controls.First(x=>x.Key==key);
                Check(c.Get("posX")=="125"&&c.Get("posY")=="250"&&c.Get("height")=="180"&&c.Get("texture")=="Studio/Picture.png"&&c.Get("keepaspectratio")=="yes"&&p.Undo.Count==1,"Insertion lost properties or used multiple undo steps.");p.UndoChange();Check(ProjectFile.XmlSignature(p)==xml,"Undo did not restore original bytes.");p.RedoChange();Check(p.Controls.Any(x=>x.Key==key),"Redo did not restore the element.");
            });
            Test("Rejected positioned insertions preserve XML and history",()=>{
                var p=SessionFixture(root,"drop-invalid");string xml=ProjectFile.XmlSignature(p);Reject(()=>p.InsertControlAt("main.xml","image",-1,20,"",20,20));Reject(()=>p.InsertControlAt("main.xml","image",1,20,"../outside.png",20,20));Reject(()=>p.InsertControlAt("main.xml","unknown",1,20,"",20,20));Check(ProjectFile.XmlSignature(p)==xml&&p.Undo.Count==0,"Invalid insertion changed the session.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunWorkflowToolsUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));string xml=ProjectFile.XmlSignature(project);
            SetGeometryProtection("position",true);verify(fields["posX"].ReadOnly&&!fields["width"].ReadOnly&&!fields["label"].ReadOnly,"Position protection disables only position inputs");
            string key=selected.Key;canvas.Update();canvas.ExerciseDrag(selected,50,40,false);verify(ProjectFile.XmlSignature(project)==xml,"Position lock prevents canvas movement");SelectControl(project.Controls.First(c=>c.Key==key));SetGeometryProtection("position",false);SetGeometryProtection("size",true);
            verify(fields["width"].ReadOnly&&!fields["posX"].ReadOnly,"Size protection leaves movement inputs editable");canvas.Update();canvas.ExerciseDrag(selected,50,40,true);verify(ProjectFile.XmlSignature(project)==xml,"Size lock prevents resizing through canvas events");SelectControl(project.Controls.First(c=>c.Key==key));SetGeometryProtection("size",false);
            snapSpacing.SelectedIndex=snapSpacing.Items.IndexOf(20);verify(project.Editor.SnapSpacing==20&&SessionNeedsSaving()&&!project.Dirty,"Snap selector changes saved editor state without XML edits");
            ToggleDropPalette();verify(dropPalette.Visible&&dropTextures.Assets.Count>0&&dropTextures.DragImages,"Integrated toolbox lists draggable skin images");
            var data=new DataObject(typeof(ElementDrag).FullName,new ElementDrag{Kind="label",ProjectRoot=project.Root});int before=project.Controls.Count;
            fields["posX"].Text="222";verify(!CanAcceptInsertDrop(data),"Pending property drafts block drop acceptance");RevertPropertyInputs();
            var effect=canvas.ExerciseDrop(data,new PointF(362,358));verify(effect==DragDropEffects.Copy&&project.Controls.Count==before+1&&selected.Get("type")=="label"&&selected.Get("posX")=="360"&&selected.Get("posY")=="360"&&selected.Source.RelativePath=="demo.xml"&&project.Undo.Count==1,"Canvas drop inserts into the page at snapped skin coordinates in one action");undo.PerformClick();verify(ProjectFile.XmlSignature(project)==xml,"Undo of a canvas drop restores the entire source");
            var asset=dropTextures.Assets.First(a=>a.PhysicalPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase));data=new DataObject(DataFormats.FileDrop,new[]{asset.PhysicalPath});
            canvas.ExerciseDrop(data,new PointF(802,402));verify(selected.Get("type")=="image"&&selected.Get("texture")==asset.RelativePath&&selected.Number("width",0)>0&&project.Undo.Count==1,"Media-file drop uses its relative texture and natural dimensions");undo.PerformClick();
            verify(!CanAcceptInsertDrop(new DataObject(DataFormats.FileDrop,new[]{asset.PhysicalPath,asset.PhysicalPath}))&&!CanAcceptInsertDrop(new DataObject(DataFormats.FileDrop,new[]{Path.Combine(Path.GetTempPath(),"outside.png")})),"Multiple files and images outside active Media are rejected");
            var stale=new DataObject(typeof(ElementDrag).FullName,new ElementDrag{Kind="button",ProjectRoot="another skin"});verify(!CanAcceptInsertDrop(stale),"Tool payloads from another skin are rejected");
            string file=Path.Combine(directory,"WorkflowTools.mp1skinproj");SetGeometryProtection("position",true);SaveSession(file);ResumeSession(file);verify(project.Editor.SnapSpacing==20&&project.IsPositionLocked(selected)&&snapSpacing.SelectedItem.ToString()=="20","Saved project restores geometry protection and synchronizes grid tools");
            dropPalette.Visible=false;LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
