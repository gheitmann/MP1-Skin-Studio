using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    // Route wheel messages over the embedded list without stealing focus from
    // property inputs or turning a scroll into an expensive page selection.
    public sealed class PageListBox : ListBox,IMessageFilter
    {
        int wheelRemainder;
        public PageListBox()
        {IntegralHeight=false;HorizontalScrollbar=true;ScrollAlwaysVisible=true;BorderStyle=BorderStyle.FixedSingle;AccessibleName="Skin pages and XML files";}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Application.AddMessageFilter(this);}
        protected override void OnHandleDestroyed(EventArgs e){Application.RemoveMessageFilter(this);base.OnHandleDestroyed(e);}
        internal void ScrollWheel(int delta)
        {
            if(Items.Count==0||delta==0)return;
            wheelRemainder+=delta;int steps=wheelRemainder/SystemInformation.MouseWheelScrollDelta;wheelRemainder%=SystemInformation.MouseWheelScrollDelta;
            int lines=SystemInformation.MouseWheelScrollLines;
            if(lines<0)lines=Math.Max(1,ClientSize.Height/Math.Max(1,ItemHeight)-1);
            if(steps!=0&&lines>0)TopIndex=Math.Max(0,Math.Min(Items.Count-1,TopIndex-steps*lines));
        }
        public bool PreFilterMessage(ref Message message)
        {
            if(message.Msg!=0x020A||!Visible||!Enabled||!IsHandleCreated)return false;
            var owner=FindForm();if(owner==null||!owner.Enabled)return false;
            var target=Control.FromChildHandle(message.HWnd);if(target!=null&&target.FindForm()!=owner)return false;
            long position=message.LParam.ToInt64();var point=new Point(unchecked((short)(position&0xffff)),unchecked((short)((position>>16)&0xffff)));
            if(!ClientRectangle.Contains(PointToClient(point)))return false;
            ScrollWheel(unchecked((short)((message.WParam.ToInt64()>>16)&0xffff)));return true;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {ScrollWheel(e.Delta);var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;}
    }
    public sealed partial class StudioForm
    {
        string[] allPageNames=new string[0];
        TextBox pageSearch;Label pageCount;
        Control BuildPageBrowser()
        {
            var browser=new Panel{Dock=DockStyle.Fill};
            var header=new Panel{Dock=DockStyle.Top,Width=285,Height=76};
            header.Controls.Add(new Label{Text="PAGES / XML",ForeColor=muted,Location=new Point(0,0),Size=new Size(285,20),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right});
            pageSearch=new TextBox{AccessibleName="Filter skin pages and XML files",BackColor=Color.FromArgb(20,26,35),ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,Location=new Point(0,22),Width=285,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
            header.Controls.Add(pageSearch);pageCount=new Label{ForeColor=muted,Location=new Point(0,50),Size=new Size(285,23),AutoEllipsis=true,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};header.Controls.Add(pageCount);
            page.BackColor=Color.FromArgb(20,26,35);page.ForeColor=ink;page.Dock=DockStyle.Fill;
            browser.Controls.Add(new DarkScrollHost(page));browser.Controls.Add(header);
            pageSearch.TextChanged+=(s,e)=>{if(!loading)FilterPageChoices();};
            pageSearch.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){pageSearch.Clear();e.Handled=true;e.SuppressKeyPress=true;}};
            tips.SetToolTip(pageSearch,"Filter XML filenames. Filtering and scrolling keep the current preview and pending property inputs unchanged. Escape clears the filter.");
            tips.SetToolTip(page,"Click a file to open its controls. Scroll with the mouse wheel without changing the active page. Drag the divider below to resize the file list.");
            return browser;
        }
        void PopulatePageChoices(string[] pages)
        {
            allPageNames=pages;bool prior=loading;loading=true;
            try{pageSearch.Clear();}finally{loading=prior;}
            FilterPageChoices();
        }
        void FilterPageChoices()
        {
            bool prior=loading;loading=true;page.BeginUpdate();
            try
            {
                string query=pageSearch.Text.Trim();var shown=allPageNames.Where(p=>p.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
                page.Items.Clear();page.Items.AddRange(shown.Cast<object>().ToArray());
                page.SelectedIndex=project==null?-1:page.FindStringExact(project.Page);UpdatePageSummary();
            }
            finally{page.EndUpdate();loading=prior;}
        }
        void UpdatePageSummary()
        {
            if(pageCount==null)return;
            pageCount.Text=page.Items.Count+" of "+allPageNames.Length+" files"+(page.Items.Count==0?" · No matches":"");
            tips.SetToolTip(pageCount,"Active page: "+(project==null?"none":project.Page)+". The preview stays on this page while filtering or scrolling.");
        }
        void ChooseIntegratedPage()
        {
            if(loading||project==null||page.SelectedItem==null||String.Equals(page.SelectedItem.ToString(),project.Page,StringComparison.OrdinalIgnoreCase))return;
            if(PendingProperties())
            {
                bool prior=loading;loading=true;try{page.SelectedIndex=page.FindStringExact(project.Page);}finally{loading=prior;}
                ShowNotice("Apply Properties or Revert Inputs before changing pages. Your current inputs are preserved.");return;
            }
            Attempt(OpenSelectedPage);
        }
    }
}
