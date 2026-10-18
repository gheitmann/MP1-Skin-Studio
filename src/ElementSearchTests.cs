using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunElementSearchTests()
        {
            Test("Element search combines words across names, sources and properties",()=>
            {var c=Named(Demo(),"Movies");Check(ElementSearch.Matches(c,"MOVIES menu button")&&!ElementSearch.Matches(c,"Movies series")&&ElementSearch.Matches(c,"  \t "),"Search terms were combined incorrectly.");});
            Test("ID search compares complete numeric IDs rather than substrings",()=>
            {var c=Named(Demo(),"Movies");Check(ElementSearch.Matches(c,"id:0010")&&!ElementSearch.Matches(c,"id:1")&&!ElementSearch.Matches(c,"id:")&&!ElementSearch.Matches(c,"id:abc"),"ID was not checked exactly.");});
            Test("Targeted search separates type, text and source file",()=>
            {var c=Named(Demo(),"Movies");Check(ElementSearch.Matches(c,"type:BUTTON source:menu text:Mov")&&!ElementSearch.Matches(c,"type:but")&&!ElementSearch.Matches(c,"text:menu"),"Field search is ambiguous.");});
            Test("Bindings and image names remain searchable",()=>
            {var p=Demo();Check(ElementSearch.Matches(Named(p,"Time"),"#time")&&ElementSearch.Matches(Named(p,"Movies"),"Button-blue.png")&&!ElementSearch.Matches(Named(p,"Movies"),"texture"),"Binding or image was not found, or property names were incorrectly searched.");});
            Test("Element search preserves XML, undo and editor state",()=>
            {var p=Demo();string before=ProjectFile.ContentSignature(p);foreach(var c in p.Controls)ElementSearch.Matches(c,"type:button source:menu");Check(!p.Dirty&&p.Undo.Count==0&&ProjectFile.ContentSignature(p)==before,"Search changes the project.");});
        }
    }
    public sealed partial class StudioForm
    {
        SkinControl[] VisibleTreeControls(){return TreeControlNodes().Select(n=>(SkinControl)n.Tag).ToArray();}
        void RunSearchUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");var film=project.Controls.First(c=>c.Name=="Movies");SelectControl(film);fields["width"].Text="512";string signature=ProjectFile.ContentSignature(project);int total=project.Controls.Count;
            elementSearch.Text="type:button source:menu";
            verify(VisibleTreeControls().Length==3&&project.Controls.Count==total&&fields["width"].Text=="512"&&ProjectFile.ContentSignature(project)==signature,"Search filters affect only the tree and preserve pending edits and the full preview");
            tree.SelectedNode=tree.Nodes[0].Nodes[1];verify(selected.Key==film.Key&&fields["width"].Text=="512"&&((SkinControl)tree.SelectedNode.Tag).Key==film.Key,"Tree matches preserve pending property edits and the previous selection");
            bool blocked=false;try{NextSearchResult(false);}catch(InvalidOperationException){blocked=true;}
            verify(blocked&&selected.Key==film.Key&&fields["width"].Text=="512","Match selection is blocked while properties remain unapplied");Apply();
            NextSearchResult(false);verify(selected.Name=="Series"&&((SkinControl)tree.SelectedNode.Tag).Key==selected.Key,"Enter selects the next match and highlights it in the tree");
            NextSearchResult(true);verify(selected.Name=="Movies","Reverse search finds the previous match");
            NextSearchResult(true);verify(selected.Name=="Music","Reverse search wraps from the first to the last match");
            elementSearch.Text="id:10";verify(VisibleTreeControls().Length==1&&VisibleTreeControls()[0].Name=="Movies","Targeted ID searches filter include controls in the real tree");
            elementSearch.Text="no-results-xyz";string key=selected.Key;NextSearchResult(false);
            verify(tree.Nodes.Count==0&&selected.Key==key&&status.Text.StartsWith("No matching"),"Empty result lists preserve selection and properties");
            var escape=new KeyEventArgs(Keys.Escape);SearchKey(escape);verify(elementSearch.Text.Length==0&&VisibleTreeControls().Length==total&&escape.SuppressKeyPress,"Escape clears search filters and restores all tree nodes");
            SelectControl(project.Controls.First(c=>c.Name=="Movies"));elementSearch.Text="id:10";undo.PerformClick();verify(selected.Get("width")=="470"&&VisibleTreeControls().Single().Get("width")=="470","Undo refreshes filtered control objects without stale results");
            page.SelectedIndex=page.FindStringExact("demo.layers.xml");verify(elementSearch.Text=="id:10"&&tree.Nodes.Count==0,"Page changes apply the current search filter to the new page");
            elementSearch.Clear();canvas.Update();var point=canvas.PointForTest(950,650);var hits=canvas.OverlappingAt(point);
            verify(String.Join(",",hits.Select(c=>c.Name))=="Layer C,Layer B,Layer A","Pointer hits are sorted in visible front-to-back order");
            project.Editor.Locked.Add(hits[0].Key);string before=ProjectFile.ContentSignature(project);elementSearch.Text="id:201";canvas.ExercisePick(point);var menu=StudioMenus.PeekForTest(this,"overlap");
            verify(menu.Items.Count==5&&menu.Items[2].Text.Contains("[L]")&&menu.Items[2].Enabled,"Overlap lists include locked elements for inspection");menu.ChooseForTest(2);Application.DoEvents();
            verify(selected.Name=="Layer C"&&fields.Values.All(b=>b.ReadOnly)&&tree.SelectedNode==null&&ProjectFile.ContentSignature(project)==before,"Real context menus select obscured locked elements despite tree filters without XML edits");
            canvas.ExercisePick(point);menu=StudioMenus.PeekForTest(this,"overlap");menu.ChooseForTest(3);Application.DoEvents();
            verify(selected.Name=="Layer B"&&project.Undo.Count==0,"Reused overlap lists select middle layers without dragging or undo steps");
            project.Editor.Hidden.Add(selected.Key);hits=canvas.OverlappingAt(point);verify(hits.Length==2&&hits.All(c=>c.Name!="Layer B"),"Editor-hidden elements are excluded from pointer hit lists");
            var a=project.Controls.First(c=>c.Name=="Layer A");project.Edit(a,SelfTestsForSearch("visible","false"),"Bedingung");RefreshProject(a.Key);canvas.ShowHidden=false;hits=canvas.OverlappingAt(point);
            verify(hits.Length==1&&hits[0].Name=="Layer C","Conditionally invisible controls are excluded from overlap lists");canvas.ShowHidden=true;
            verify(canvas.OverlappingAt(point).Length==2,"Show-hidden includes conditionally invisible controls while preserving editor hiding");
            verify(canvas.OverlappingAt(canvas.PointForTest(-20,200)).Length==0,"No controls are offered outside the skin surface");
            canvas.ShowHidden=false;LoadProject(DemoRoot(),"demo.xml");verify(elementSearch.Text.Length==0,"Project changes reset temporary element searches");
        }
        static System.Collections.Generic.Dictionary<string,string> SelfTestsForSearch(string key,string value){return new System.Collections.Generic.Dictionary<string,string>{{key,value}};}
    }
}
