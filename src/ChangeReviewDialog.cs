using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class XmlReviewPane : UserControl
    {
        internal const int SectionSize=60000;
        readonly TextBox content=new TextBox();readonly Label location=new Label();
        readonly Button previous=new Button(),next=new Button();
        string source="";int section;
        public XmlReviewPane(string heading)
        {
            var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=37,WrapContents=false};
            top.Controls.Add(new Label{Text=heading,AutoSize=true,Margin=new Padding(4,9,12,0)});
            previous.Text="Previous";previous.Width=85;previous.Click+=(s,e)=>ShowSection(section-1,0);top.Controls.Add(previous);
            next.Text="Next";next.Width=70;next.Click+=(s,e)=>ShowSection(section+1,0);top.Controls.Add(next);
            content.Dock=DockStyle.Fill;content.Multiline=true;content.ReadOnly=true;content.WordWrap=false;
            content.ScrollBars=ScrollBars.Both;content.MaxLength=Int32.MaxValue;content.Font=new Font("Consolas",9.5f);content.HideSelection=false;
            location.Dock=DockStyle.Bottom;location.Height=43;location.Padding=new Padding(4);
            Controls.Add(content);Controls.Add(location);Controls.Add(top);
        }
        internal void SetSource(string text,int focus)
        {source=text??"";ShowSection(Math.Min(focus,Math.Max(0,source.Length-1))/SectionSize,focus);}
        void ShowSection(int index,int focus)
        {
            int last=Math.Max(0,(source.Length-1)/SectionSize);section=Math.Max(0,Math.Min(index,last));
            int start=section*SectionSize,count=Math.Min(SectionSize,source.Length-start);
            content.Text=source.Substring(start,count);int position=Math.Max(0,Math.Min(count,focus-start));
            content.Select(position,position<count?1:0);content.ScrollToCaret();
            previous.Enabled=section>0;next.Enabled=section<last;
            location.Text=source.Length==0?"Empty text":"Section "+(section+1)+" / "+(last+1)+" | characters "+(start+1)+"–"+(start+count)+" of "+source.Length+" | starts at line "+ChangeReview.LineAt(source,start)+". Continue with Previous / Next.";
        }
    }
    public sealed class ChangeReviewDialog : Form
    {
        readonly Func<ReviewedXmlChange[]> capture;
        ReviewedXmlChange[] files;
        readonly ListView list=new ListView();readonly TextBox search=new TextBox(),details=new TextBox();
        readonly Label summary=new Label();readonly XmlReviewPane original=new XmlReviewPane("Loaded original"),edited=new XmlReviewPane("Edited XML");
        public ChangeReviewDialog(Func<ReviewedXmlChange[]> capture,bool pending)
        {
            this.capture=capture;files=capture();Text="Review XML changes — MP1 Skin Studio";
            Size=new Size(1220,790);MinimumSize=new Size(980,680);StartPosition=FormStartPosition.CenterParent;
            AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);
            var top=new TableLayoutPanel{Dock=DockStyle.Top,Height=113,Padding=new Padding(10),ColumnCount=1,RowCount=3};
            summary.Dock=DockStyle.Fill;top.Controls.Add(summary);
            top.Controls.Add(new Label{Dock=DockStyle.Fill,Text="All modified XML in this session, including loaded shared/theme files and earlier pages. Preview settings are excluded."});
            var row=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};row.Controls.Add(new Label{Text="File filter",Width=65,Padding=new Padding(0,5,0,0)});
            search.Width=480;search.TextChanged+=(s,e)=>Filter();row.Controls.Add(search);top.Controls.Add(row);
            list.Dock=DockStyle.Top;list.Height=145;list.View=View.Details;list.FullRowSelect=true;list.HideSelection=false;list.MultiSelect=false;
            list.Columns.Add("Modified XML",540);list.Columns.Add("Original bytes",115);list.Columns.Add("Edited bytes",110);list.Columns.Add("Source check",220);
            list.SelectedIndexChanged+=(s,e)=>SelectFile();
            details.Dock=DockStyle.Top;details.Height=91;details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Both;details.WordWrap=false;
            var panes=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            original.Dock=DockStyle.Fill;edited.Dock=DockStyle.Fill;panes.Controls.Add(original,0,0);panes.Controls.Add(edited,1,0);
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=46,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};
            var close=new Button{Text="Close",DialogResult=DialogResult.Cancel,Width=100,Height=29};CancelButton=close;bottom.Controls.Add(close);
            var refresh=new Button{Text="Refresh source checks",Width=180,Height=29};refresh.Click+=(s,e)=>RefreshSnapshot();bottom.Controls.Add(refresh);
            var difference=new Button{Text="First difference",Width=140,Height=29};difference.Click+=(s,e)=>SelectFile();bottom.Controls.Add(difference);
            var note=new Label{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(10,4,10,0),Text=(pending?"Pending property inputs are excluded; Apply or Revert them before export. ":"")+"Read-only snapshot. Source checks can become stale; export rechecks them. Shared includes may affect other pages. Each pane shows sections of up to 60,000 characters."};
            Controls.Add(panes);Controls.Add(details);Controls.Add(list);Controls.Add(top);Controls.Add(note);Controls.Add(bottom);Filter();
        }
        void Filter(string keep=null)
        {
            list.BeginUpdate();list.Items.Clear();
            foreach(var file in ChangeReview.Filter(files,search.Text))
            {
                var item=new ListViewItem(new[]{file.RelativePath,file.OriginalBytes.ToString(),file.EditedBytes.ToString(),file.SourceStatus}){Tag=file};
                if(!file.SourceMatches)item.ForeColor=Color.Firebrick;
                list.Items.Add(item);if(file.RelativePath==keep)item.Selected=true;
            }
            if(list.SelectedItems.Count==0&&list.Items.Count>0)list.Items[0].Selected=true;
            list.EndUpdate();summary.Text=files.Length+" modified XML file(s) | "+list.Items.Count+" shown | "+files.Count(f=>!f.SourceMatches)+" source conflict(s) or unavailable checks. "+(files.Length==0?"Preview-only changes do not produce XML exports.":"");SelectFile();
        }
        void SelectFile()
        {
            var file=list.SelectedItems.Count==1?list.SelectedItems[0].Tag as ReviewedXmlChange:null;
            details.Text=file==null?"No modified XML matches this filter.":file.Details;
            original.SetSource(file==null?"":file.Original,file==null?0:file.FirstDifference);
            edited.SetSource(file==null?"":file.Edited,file==null?0:file.FirstDifference);
        }
        void RefreshSnapshot()
        {
            string keep=list.SelectedItems.Count==1?((ReviewedXmlChange)list.SelectedItems[0].Tag).RelativePath:null;
            try{var next=capture();files=next;Filter(keep);}
            catch(Exception error){MessageBox.Show(this,error.Message,"Review could not be refreshed",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
    public sealed partial class StudioForm
    {
        void ShowChangeReview()
        {
            if(project==null)return;
            using(var dialog=new ChangeReviewDialog(()=>ChangeReview.Capture(project),PendingProperties()))dialog.ShowDialog(this);
        }
    }
}
