using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject Demo(){var p=new SkinProject(StudioForm.DemoRoot());p.OpenPage("demo.xml");return p;}
        static SkinControl Named(SkinProject p,string name){return p.Controls.First(c=>c.Get("description")==name);}
        static void RunDesignerTests(string root)
        {
            Test("Cached control ordinals preserve nested source order after reparsing",()=>
            {
                var source=SkinSource.FromText("<window><controls><control><type>group</type><control><type>label</type><label>Child</label></control></control><control><type>label</type><label>Next</label></control></controls></window>","ordinals.xml");
                Check(source.Elements.Where(n=>n.Name=="control").Select(n=>n.ControlOrdinal).SequenceEqual(new[]{0,1,2})&&source.Elements.Where(n=>n.Name!="control").All(n=>n.ControlOrdinal==-1),"Cached source ordinals do not follow XML preorder.");
                source.SetControlProperties(1,Set("label","A much longer child label"));Check(source.Elements.Where(n=>n.Name=="control").Select(n=>n.ControlOrdinal).SequenceEqual(new[]{0,1,2})&&source.Elements.Single(n=>n.ControlOrdinal==1).Value("label")=="A much longer child label","Reparsing changed control identity/order.");
            });
            Test("Compatible batch entries for one control merge without losing properties",()=>
            {
                var p=Demo();var c=Named(p,"Movies");string before=c.Source.Text;
                p.EditMany(new[]{new ControlEdit(c,Set("posX","220")),new ControlEdit(c,Set("width","600")),new ControlEdit(c,Set("posX","220"))},"Merged batch");
                Check(Named(p,"Movies").Get("posX")=="220"&&Named(p,"Movies").Get("width")=="600"&&p.Undo.Count==1,"Duplicate control entries lost values or split history.");p.UndoChange();Check(Named(p,"Movies").Source.Text==before&&!p.Dirty,"Merged batch undo is not exact.");
            });
            Test("Conflicting batch entries reject all changes and retain redo history",()=>
            {
                var p=Demo();p.Edit(Named(p,"Movies"),Set("posX","220"),"Earlier");p.UndoChange();var c=Named(p,"Movies");string signature=ProjectFile.ContentSignature(p);
                Reject(()=>p.EditMany(new[]{new ControlEdit(c,Set("posX","300")),new ControlEdit(c,Set("posX","400")),new ControlEdit(Named(p,"Time"),Set("posX","1500"))},"Conflicting batch"));
                Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==0&&p.Redo.Count==1,"Rejected batch changed XML or history.");p.RedoChange();Check(Named(p,"Movies").Get("posX")=="220","Redo no longer works after batch rejection.");
            });
            Test("A stale duplicate batch entry cannot bypass selection validation",()=>
            {
                var p=Demo();var stale=Named(p,"Movies");p.Edit(stale,Set("posX","220"),"Earlier");var current=Named(p,"Movies");string signature=ProjectFile.ContentSignature(p);
                Reject(()=>p.EditMany(new[]{new ControlEdit(current,Set("width","600")),new ControlEdit(stale,Set("posX","300"))},"Stale batch"));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==1,"Stale duplicate entry was silently ignored.");
            });
            Test("One undo/redo restores changes across multiple files exactly",()=>
            {
                var p=Demo();var a=Named(p,"Movies");var b=Named(p,"Time");string sa=a.Source.Text,sb=b.Source.Text;
                p.EditMany(new[]{new ControlEdit(a,Set("posX","220")),new ControlEdit(b,Set("posX","1700"))},"gemeinsam");
                Check(p.Undo.Count==1&&p.Sources.Values.Count(s=>s.Changed)==2,"No shared transaction.");
                p.UndoChange();Check(a.Source.Text==sa&&b.Source.Text==sb&&!p.Dirty,"Cross-file undo is incomplete.");
                p.RedoChange();Check(Named(p,"Movies").Get("posX")=="220"&&Named(p,"Time").Get("posX")=="1700","Cross-file redo is incomplete.");
            });
            Test("An error in the second element rolls back the entire batch",()=>
            {
                var p=Demo();var a=Named(p,"Movies");var b=Named(p,"Time");string sa=a.Source.Text,sb=b.Source.Text;
                Reject(()=>p.EditMany(new[]{new ControlEdit(a,Set("posX","400")),new ControlEdit(b,Set("id","9"))},"malformed"));
                Check(a.Source.Text==sa&&b.Source.Text==sb&&!p.Dirty&&p.Undo.Count==0,"Failed transaction leaves changes behind.");
                p.Edit(Named(p,"Movies"),Set("posX","440"),"Corrected input");p.UndoChange();Check(Named(p,"Movies").Source.Text==sa&&!p.Dirty,"Correcting input after rollback no longer works.");p.RedoChange();Check(Named(p,"Movies").Get("posX")=="440","Corrected transaction redo failed.");
            });
            Test("Insertion creates a complete control and undo/redo preserves its identity",()=>
            {
                var p=Demo();string before=Named(p,"Movies").Source.Text;int count=p.Controls.Count;
                string key=p.InsertControl("demo.menu.xml","button");var c=p.Controls.First(x=>x.Key==key);string id=c.Get("id");
                Check(p.Controls.Count==count+1&&SkinProject.Focusable(c)&&Int32.Parse(id)>0&&p.Controls.Count(x=>x.Get("id")==id)==1,"New button is invalid.");
                p.UndoChange();Check(Named(p,"Movies").Source.Text==before&&p.Controls.Count==count,"Insertion undo is incomplete.");p.RedoChange();Check(p.Controls.First(x=>x.Key==key).Get("id")==id,"Redo assigns a different identity.");
            });
            Test("Text and image templates are inserted as valid XML",()=>
            {
                var p=Demo();string a=p.InsertControl("demo.menu.xml","label"),b=p.InsertControl("demo.menu.xml","image");
                Check(p.Controls.First(c=>c.Key==a).Get("type")=="label"&&p.Controls.First(c=>c.Key==b).Get("type")=="image","Invalid templates.");
                Check(p.Controls.First(c=>c.Key==a).Get("id")!=p.Controls.First(c=>c.Key==b).Get("id"),"New IDs collide.");
            });
            Test("New IDs account for unopened pages and navigation targets",()=>
            {
                string folder=Path.Combine(root,"id-scan");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>10</id></control></controls></window>");
                Write(folder,"other.xml","<window><controls><control><type>button</type><id>10000</id><onleft>10001</onleft></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Check(p.NextControlId()==10002,"Another page or reserved navigation target was missed.");
            });
            Test("Duplication preserves the original, comments, styles and unknown entries",()=>
            {
                var p=Demo();var original=Named(p,"Movies");string raw=original.Source.Text.Substring(original.Node.Start,original.Node.End-original.Node.Start);
                string key=p.DuplicateControl(original,false);var clone=p.Controls.First(c=>c.Key==key);
                Check(original.Source.Text.Contains(raw),"Original control was modified.");
                Check(clone.Get("id")!="10"&&clone.Get("posX")=="170"&&clone.Get("posY")=="610","New identity or offset is missing.");
                Check(clone.Node.Child("editorUnknown")!=null&&clone.Source.Text.Substring(clone.Node.Start,clone.Node.End-clone.Node.Start).Contains("Dieser Kommentar"),"Unknown XML data in the copy was lost.");
            });
            Test("Bound positions are protected when duplicating",()=>
            {
                string folder=Path.Combine(root,"clone-bindings");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><define>#x:120</define><controls><control><type>label</type><id>0</id><posX>#x</posX><posY>20</posY></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Reject(()=>p.DuplicateControl(p.Controls[0],false));Check(!p.Dirty,"Rejected copy changes the original.");
                string key=p.DuplicateControl(p.Controls[0],true);Check(p.Controls.First(c=>c.Key==key).Get("posX")=="150","Incorrect approved define offset.");
            });
            Test("Alignment respects right-aligned label anchors",()=>
            {
                var p=Demo();var time=Named(p,"Time");var film=Named(p,"Movies");
                var edits=LayoutTools.Align(new[]{time,film},"left",false);p.EditMany(edits,"links");
                Check(LayoutTools.Bounds(Named(p,"Time")).Left==LayoutTools.Bounds(Named(p,"Movies")).Left,"Incorrect label anchor during alignment.");
            });
            Test("Equal horizontal spacing preserves outer edges",()=>
            {
                var p=Demo();p.Edit(Named(p,"Series"),Set("posX","800"),"verschieben");
                var selection=new[]{Named(p,"Movies"),Named(p,"Series"),Named(p,"Music")};
                p.EditMany(LayoutTools.Align(selection,"hspace",false),"verteilen");
                var a=LayoutTools.Bounds(Named(p,"Movies"));var b=LayoutTools.Bounds(Named(p,"Series"));var c=LayoutTools.Bounds(Named(p,"Music"));
                Check(a.Left==140&&c.Right==1770&&Math.Abs((b.Left-a.Right)-(c.Left-b.Right))<=1,"Incorrect spacing or outer edges.");
            });
            Test("Alignment requires approval before replacing bound positions",()=>
            {
                var p=Demo();p.Edit(Named(p,"Movies"),Set("posX","#unresolved"),"Bindung");
                Reject(()=>LayoutTools.Align(new[]{Named(p,"Movies"),Named(p,"Music")},"left",false));
                Check(Named(p,"Movies").Get("posX")=="#unresolved","Binding was lost.");
            });
            Test("Navigation connects only the selected direction and supports undo",()=>
            {
                var p=Demo();string before=Named(p,"Movies").Source.Text;
                p.ConnectNavigation(Named(p,"Movies"),Named(p,"Music"),"onright",false);
                Check(p.NavigationTarget(Named(p,"Movies"),"onright").Name=="Music"&&Named(p,"Movies").Get("onleft")=="12","Wrong direction was changed.");
                p.UndoChange();Check(Named(p,"Movies").Source.Text==before,"Navigation undo changes other data.");
            });
            Test("Navigation rejects duplicate IDs and non-focusable targets",()=>
            {
                var p=Demo();Reject(()=>p.ConnectNavigation(Named(p,"Movies"),Named(p,"Time"),"onright",false));
                string folder=Path.Combine(root,"duplicate-nav");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>1</id></control><control><type>button</type><id>2</id></control><control><type>button</type><id>2</id></control></controls></window>");
                var q=new SkinProject(folder);q.OpenPage("main.xml");Reject(()=>q.ConnectNavigation(q.Controls[0],q.Controls[1],"onright",false));Check(!q.Dirty,"Ambiguous connection was saved.");
            });
            Test("Navigation protects dynamic target bindings",()=>
            {
                var p=Demo();p.Edit(Named(p,"Movies"),Set("onright","#target"),"Bindung");
                Reject(()=>p.ConnectNavigation(Named(p,"Movies"),Named(p,"Music"),"onright",false));
                p.ConnectNavigation(Named(p,"Movies"),Named(p,"Music"),"onright",true);Check(Named(p,"Movies").Get("onright")=="12","Approved connection was not saved.");
            });
            Test("Navigation recognizes equal numeric IDs with leading zeros",()=>
            {
                string folder=Path.Combine(root,"numeric-ids");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>1</id></control><control><type>button</type><id>02</id></control><control><type>button</type><id>2</id></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Reject(()=>p.ConnectNavigation(p.Controls[0],p.Controls[2],"onright",false));Check(!p.Dirty,"Numerically duplicate ID was not detected.");
            });
            Test("Empty self-closing controls sections support insertion and exact undo",()=>
            {
                string folder=Path.Combine(root,"empty-controls");Directory.CreateDirectory(folder);string xml="<window><id>0</id><controls retain=\"yes\" /></window>";Write(folder,"main.xml",xml);
                var p=new SkinProject(folder);p.OpenPage("main.xml");p.InsertControl("main.xml","label");Check(p.Controls.Count==1&&p.Controls[0].Source.Text.Contains("retain=\"yes\""),"Empty section or attribute was corrupted.");p.UndoChange();Check(p.Source(p.Resolve("main.xml")).Text==xml,"Undo normalizes the original.");
            });
            Test("Unresolved geometry is not guessed even when binding replacement is allowed",()=>
            {
                var p=Demo();p.Edit(Named(p,"Movies"),Set("posX","#(unknown())"),"Formel");
                Reject(()=>LayoutTools.Align(new[]{Named(p,"Movies"),Named(p,"Music")},"left",true));Reject(()=>p.DuplicateControl(Named(p,"Movies"),true));
                Check(Named(p,"Movies").Get("posX")=="#(unknown())","Expression was replaced with a guessed value.");
            });
        }
    }
}
