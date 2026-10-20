using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        TextBox elementSearch;
        Label searchCount;
        Panel BuildElementSearch()
        {
            var bar=new Panel{Dock=DockStyle.Top,Width=285,Height=82};
            bar.Controls.Add(new Label{Text="SEARCH ELEMENTS",ForeColor=muted,Location=new Point(0,0),Size=new Size(285,21),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right});
            elementSearch=new TextBox{AccessibleName="Search elements on the open page",BackColor=Color.FromArgb(20,26,35),ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,Location=new Point(0,23),Width=251,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};bar.Controls.Add(elementSearch);
            var clear=ButtonFor("×",()=>{elementSearch.Clear();elementSearch.Focus();});clear.SetBounds(257,22,28,25);clear.Anchor=AnchorStyles.Top|AnchorStyles.Right;clear.Margin=Padding.Empty;clear.AccessibleName="Clear element search";bar.Controls.Add(clear);
            searchCount=new Label{ForeColor=muted,Location=new Point(0,52),Size=new Size(285,26),AutoEllipsis=true,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};bar.Controls.Add(searchCount);
            tips.SetToolTip(elementSearch,"Search names, IDs, types, text, sources or properties. All words must match. Filters: id:10, type:button, source:menu. Enter: next; Shift+Enter: previous; Esc: clear. The full preview remains visible.");
            elementSearch.TextChanged+=(s,e)=>{if(project!=null&&!loading)RebuildElementTree();};
            elementSearch.KeyDown+=(s,e)=>SearchKey(e);
            return bar;
        }
        SkinControl[] SearchResults()
        {return project.Controls.Where(c=>ElementSearch.Matches(c,elementSearch.Text,state.Label(c,c.Get("label")))).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();}
        void RebuildElementTree()
        {
            bool prior=loading;loading=true;tree.BeginUpdate();
            try
            {
                tree.Nodes.Clear();var matches=SearchResults();
                foreach(var group in matches.GroupBy(c=>c.Source.RelativePath))
                {
                    var node=new TreeNode(group.Key+(group.First().Source.Changed?"  *":"")){ForeColor=Color.FromArgb(141,177,211)};
                    AddTreeGroup(node,project.Controls.Where(c=>c.Source==group.First().Source).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray(),group.ToArray());tree.Nodes.Add(node);
                }
                int total=project.Controls.Select(c=>c.Key).Distinct().Count();searchCount.Text=matches.Length+" of "+total+" elements";tree.ShowNodeToolTips=true;SyncElementTreeSelection();
            }
            finally{tree.EndUpdate();loading=prior;}
        }
        void SyncElementTreeSelection()
        {
            bool prior=loading;loading=true;
            try
            {
                TreeNode match=null;
                foreach(TreeNode leaf in TreeControlNodes())if(selected!=null&&((SkinControl)leaf.Tag).Key==selected.Key)match=leaf;
                tree.SelectedNode=match;if(match!=null)match.EnsureVisible();
            }
            finally{loading=prior;}
        }
        void SearchKey(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Escape){elementSearch.Clear();e.Handled=true;e.SuppressKeyPress=true;}
            if(e.KeyCode==Keys.Enter){Attempt(()=>NextSearchResult(e.Shift));e.Handled=true;e.SuppressKeyPress=true;}
        }
        void NextSearchResult(bool previous)
        {
            var matches=SearchResults();if(matches.Length==0){ShowNotice("No matching elements on the open page or its includes.");return;}
            int current=Array.FindIndex(matches,c=>selected!=null&&c.Key==selected.Key);
            int next=current<0?(previous?matches.Length-1:0):(current+(previous?-1:1)+matches.Length)%matches.Length;
            SelectFoundControl(matches[next]);
        }
        void SelectFromTree(SkinControl control)
        {
            if(PendingProperties())
            {SyncElementTreeSelection();ShowNotice("Apply pending property edits first; the previous selection is preserved.");return;}
            SelectControl(control,(ModifierKeys&Keys.Control)!=0,false);
        }
        void SelectFoundControl(SkinControl control)
        {
            if(PendingProperties())throw new InvalidOperationException("Properties have pending edits. Apply them first.");
            SelectControl(control);SyncElementTreeSelection();
            ShowNotice(DisplayName(control)+" · "+control.Source.RelativePath+" · ID "+control.Get("id","-")+(project.IsLocked(control)?" · locked":"")+(project.IsEditorHidden(control)?" · hidden in editor":""));
        }
        void PickOverlapping(SkinControl[] controls,Point location)
        {
            var menu=StudioMenus.For(this,"overlap");menu.Items.Add(controls.Length+" elements under the pointer (front first)").Enabled=false;
            menu.Items.Add(new ToolStripSeparator());
            foreach(var item in controls)
            {
                var control=item;var entry=(ToolStripMenuItem)menu.Items.Add((EditorMark(control)+DisplayName(control)+" · "+control.Get("type")+" · ID "+control.Get("id","-")+" · "+control.Source.RelativePath+" ["+control.Ordinal+"]").Replace("&","&&"),null,(s,e)=>Attempt(()=>SelectFoundControl(control)));
                entry.Checked=selected!=null&&selected.Key==control.Key;
            }
            if(controls.Length==0)menu.Items.Add("No visible element at this position.").Enabled=false;
            menu.Show(canvas,location);
        }
        public void PrepareSearchPreview(){LoadProject(DemoRoot(),"demo.xml");elementSearch.Text="type:button source:menu";}
    }
    public sealed partial class SkinCanvas
    {
        public event Action<SkinControl[],Point> PickRequest;
        internal SkinControl[] OverlappingAt(Point location)
        {
            if(Project==null)return new SkinControl[0];UpdateTransform();var point=Logical(location);
            if(point.X<0||point.Y<0||point.X>=Project.Width||point.Y>=Project.Height)return new SkinControl[0];
            return WithMeasuredLayout(()=>Project.Controls.Where(c=>Drawn(c)&&SelectionContains(c,point)).Reverse().GroupBy(c=>c.Key).Select(g=>g.First()).ToArray());
        }
        bool PickMouseDown(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Right||Project==null||dragging||panning)return false;
            Focus();if(PickRequest!=null)PickRequest(OverlappingAt(e.Location),e.Location);return true;
        }
        internal Point PointForTest(float x,float y){UpdateTransform();return new Point((int)Math.Round(origin.X+x*scale),(int)Math.Round(origin.Y+y*scale));}
        internal void ExercisePick(Point location){OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,location.X,location.Y,0));}
    }
}
