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
            var bar=new Panel{Dock=DockStyle.Top,Height=82};
            bar.Controls.Add(new Label{Text="ELEMENTE SUCHEN",ForeColor=muted,Location=new Point(0,0),Size=new Size(208,21)});
            elementSearch=new TextBox{AccessibleName="Elemente der geöffneten Seite suchen",BackColor=Color.FromArgb(20,26,35),ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,Location=new Point(0,23),Width=174};bar.Controls.Add(elementSearch);
            var clear=ButtonFor("×",()=>{elementSearch.Clear();elementSearch.Focus();});clear.SetBounds(180,22,28,25);clear.Margin=Padding.Empty;clear.AccessibleName="Elementsuche löschen";bar.Controls.Add(clear);
            searchCount=new Label{ForeColor=muted,Location=new Point(0,52),Size=new Size(208,26),AutoEllipsis=true};bar.Controls.Add(searchCount);
            tips.SetToolTip(elementSearch,"Name, ID, Typ, Text, Quelle oder Eigenschaft suchen. Mehrere Wörter müssen alle passen. Gezielt: id:10, typ:button, quelle:menu. Enter: nächster Treffer; Umschalt+Enter: zurück; Esc: löschen. Die Vorschau bleibt vollständig.");
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
                    tree.Nodes.Add(node);AddTreeGroup(node,project.Controls.Where(c=>c.Source==group.First().Source).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray(),group.ToArray());
                }
                int total=project.Controls.Select(c=>c.Key).Distinct().Count();searchCount.Text=matches.Length+" von "+total+" Elementen";tree.ShowNodeToolTips=true;SyncElementTreeSelection();
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
            var matches=SearchResults();if(matches.Length==0){ShowNotice("Keine passenden Elemente in der geöffneten Seite und ihren Includes.");return;}
            int current=Array.FindIndex(matches,c=>selected!=null&&c.Key==selected.Key);
            int next=current<0?(previous?matches.Length-1:0):(current+(previous?-1:1)+matches.Length)%matches.Length;
            SelectFoundControl(matches[next]);
        }
        void SelectFromTree(SkinControl control)
        {
            if(PendingProperties())
            {SyncElementTreeSelection();ShowNotice("Offene Eigenschaftseingaben zuerst übernehmen; die bisherige Auswahl bleibt erhalten.");return;}
            SelectControl(control,(ModifierKeys&Keys.Control)!=0,false);
        }
        void SelectFoundControl(SkinControl control)
        {
            if(PendingProperties())throw new InvalidOperationException("Eigenschaften haben noch nicht übernommene Eingaben. Diese zuerst übernehmen.");
            SelectControl(control);SyncElementTreeSelection();
            ShowNotice(DisplayName(control)+" · "+control.Source.RelativePath+" · ID "+control.Get("id","-")+(project.IsLocked(control)?" · gesperrt":"")+(project.IsEditorHidden(control)?" · im Editor ausgeblendet":""));
        }
        void PickOverlapping(SkinControl[] controls,Point location)
        {
            var menu=StudioMenus.For(this,"overlap");menu.Items.Add(controls.Length+" Elemente unter dem Mauszeiger (vorn zuerst)").Enabled=false;
            menu.Items.Add(new ToolStripSeparator());
            foreach(var item in controls)
            {
                var control=item;var entry=(ToolStripMenuItem)menu.Items.Add((EditorMark(control)+DisplayName(control)+" · "+control.Get("type")+" · ID "+control.Get("id","-")+" · "+control.Source.RelativePath+" ["+control.Ordinal+"]").Replace("&","&&"),null,(s,e)=>Attempt(()=>SelectFoundControl(control)));
                entry.Checked=selected!=null&&selected.Key==control.Key;
            }
            if(controls.Length==0)menu.Items.Add("Hier liegt kein sichtbares Element.").Enabled=false;
            menu.Show(canvas,location);
        }
        public void PrepareSearchPreview(){LoadProject(DemoRoot(),"demo.xml");elementSearch.Text="typ:button quelle:menu";}
    }
    public sealed partial class SkinCanvas
    {
        public event Action<SkinControl[],Point> PickRequest;
        internal SkinControl[] OverlappingAt(Point location)
        {
            if(Project==null)return new SkinControl[0];UpdateTransform();var point=Logical(location);
            if(point.X<0||point.Y<0||point.X>=Project.Width||point.Y>=Project.Height)return new SkinControl[0];
            return Project.Controls.Where(c=>Drawn(c)&&BoundsOf(c).Contains(point)).Reverse().GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
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
