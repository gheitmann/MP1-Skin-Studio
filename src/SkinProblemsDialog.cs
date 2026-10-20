using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class SkinProblemsDialog : Form
    {
        readonly SkinAuditReport report;
        readonly bool previewReport;
        readonly ListView list=new ListView();
        readonly TextBox search=new TextBox(),detail=new TextBox();
        readonly CheckBox information=new CheckBox();
        readonly Label summary=new Label();
        readonly Button jump=new Button();
        readonly ImageList rowSize=new ImageList();
        public string TargetKey{get;private set;}
        public string TargetProperty{get;private set;}
        public int VisibleCount{get{return list.Items.Count;}}
        const string scope=FindingReport.Scope;
        public SkinProblemsDialog(SkinAuditReport result,bool preview=false)
        {
            report=result;previewReport=preview;Text=preview?"Unverified Preview — MP1 Skin Studio":"Audit Page — MP1 Skin Studio";Size=new Size(1180,720);MinimumSize=new Size(1020,600);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);
            BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(224,235,245);
            var top=new Panel{Dock=DockStyle.Top,Height=114,Padding=new Padding(14)};
            var title=new Label{Text=(preview?"UNVERIFIED PREVIEW · ":"PAGE AUDIT · ")+report.Page+(String.IsNullOrEmpty(report.Theme)?"":" · Theme "+report.Theme),Location=new Point(14,10),Height=23,Width=1120,AutoEllipsis=true,Font=new Font(Font,FontStyle.Bold)};top.Controls.Add(title);
            summary.SetBounds(14,38,1110,22);top.Controls.Add(summary);
            top.Controls.Add(new Label{Text="Search",Location=new Point(14,78),Width=62,Height=24});
            search.SetBounds(78,73,680,28);search.BackColor=Color.FromArgb(16,23,32);search.ForeColor=ForeColor;search.BorderStyle=BorderStyle.FixedSingle;search.TextChanged+=(s,e)=>Filter();top.Controls.Add(search);
            information.Text="Unverified values / information";information.Checked=preview;information.AutoSize=true;information.SetBounds(790,76,340,28);information.CheckedChanged+=(s,e)=>Filter();top.Controls.Add(information);
            top.Resize+=(s,e)=>{title.Width=top.Width-28;summary.Width=top.Width-28;search.Width=top.Width-390;information.Left=top.Width-296;};
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=53,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,8)};
            jump.Text="Go to element";jump.Width=140;jump.Height=33;jump.FlatStyle=FlatStyle.Flat;jump.BackColor=Color.FromArgb(25,112,170);jump.ForeColor=ForeColor;jump.Click+=(s,e)=>Jump();bottom.Controls.Add(jump);
            var close=new Button{Text="Close",Width=115,Height=33,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,DialogResult=DialogResult.Cancel};bottom.Controls.Add(close);CancelButton=close;AcceptButton=jump;
            var save=new Button{Text="Save shown report",Width=160,Height=33,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor};save.Click+=(s,e)=>SaveReport();bottom.Controls.Add(save);
            detail.Dock=DockStyle.Bottom;detail.Height=186;detail.Multiline=true;detail.ReadOnly=true;detail.ScrollBars=ScrollBars.Vertical;detail.BackColor=Color.FromArgb(20,27,37);detail.ForeColor=ForeColor;detail.BorderStyle=BorderStyle.FixedSingle;
            list.Dock=DockStyle.Fill;list.View=View.Details;list.FullRowSelect=true;list.HideSelection=false;list.MultiSelect=false;list.BackColor=Color.FromArgb(18,24,33);list.ForeColor=ForeColor;list.BorderStyle=BorderStyle.None;
            rowSize.ImageSize=new Size(1,25);list.SmallImageList=rowSize;
            list.Columns.Add("Kind",85);list.Columns.Add("Element",150);list.Columns.Add("Property",115);list.Columns.Add("Source : Line",235);list.Columns.Add("Finding",550);
            list.Resize+=(s,e)=>list.Columns[4].Width=Math.Max(210,list.ClientSize.Width-605);
            list.SelectedIndexChanged+=(s,e)=>UpdateDetail();list.DoubleClick+=(s,e)=>Jump();
            Controls.Add(list);Controls.Add(detail);Controls.Add(bottom);Controls.Add(top);Filter();
        }
        void Filter()
        {
            var selected=list.SelectedItems.Count==1?list.SelectedItems[0].Tag as SkinIssue:null;string query=search.Text.Trim();
            list.BeginUpdate();list.Items.Clear();
            foreach(var issue in FindingReport.Filter(report,query,information.Checked))
            {
                var item=new ListViewItem(new[]{issue.Information?"Information":"Audit finding",issue.ControlName??"Page",issue.Property??"",issue.Location,issue.Message}){Tag=issue};
                item.ForeColor=issue.Information?Color.FromArgb(145,180,206):Color.FromArgb(245,193,108);list.Items.Add(item);if(issue==selected)item.Selected=true;
            }
            if(list.SelectedItems.Count==0&&list.Items.Count>0)list.Items[0].Selected=true;
            list.EndUpdate();summary.Text=report.Warnings+" findings · "+report.Information+" informational findings · "+list.Items.Count+" shown — "+scope;UpdateDetail();
        }
        void UpdateDetail()
        {
            var issue=list.SelectedItems.Count==1?list.SelectedItems[0].Tag as SkinIssue:null;
            jump.Enabled=issue!=null&&!String.IsNullOrEmpty(issue.ControlKey);
            detail.Text=issue==null?(list.Items.Count==0?"No findings match this filter. Enable informational findings if needed.\r\n\r\n":"")+scope:issue.Message+"\r\n\r\n"+issue.Detail+"\r\n\r\nAudit code: "+issue.Code;
        }
        void SaveReport()
        {
            using(var dialog=new SaveFileDialog{Title="Save shown findings",Filter="Text report (*.txt)|*.txt|HTML report (*.html)|*.html",DefaultExt="txt",AddExtension=true,OverwritePrompt=true,FileName=(previewReport?"Preview-Findings":"Page-Audit")})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try{SaveReportTo(dialog.FileName);}
                catch(Exception ex){MessageBox.Show(this,"The report could not be saved. Previous files and findings are preserved.\r\n\r\n"+ex.Message,"Save finding report",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            }
        }
        internal void SaveReportTo(string path){FindingReport.Save(report,search.Text,information.Checked,previewReport,path);}
        void Jump()
        {
            if(list.SelectedItems.Count!=1)return;var issue=list.SelectedItems[0].Tag as SkinIssue;
            if(issue==null||String.IsNullOrEmpty(issue.ControlKey))return;
            TargetKey=issue.ControlKey;TargetProperty=issue.Property;DialogResult=DialogResult.OK;Close();
        }
        internal void FilterForTest(string query,bool includeInformation){information.Checked=includeInformation;search.Text=query;Filter();}
        internal void SelectCodeForTest(string code)
        {foreach(ListViewItem item in list.Items)item.Selected=((SkinIssue)item.Tag).Code==code;UpdateDetail();}
        internal string DetailForTest{get{return detail.Text;}}
        internal bool CanJump{get{return jump.Enabled;}}
        internal void JumpForTest(){Jump();}
        public void RenderTo(string path)
        {Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
        protected override void Dispose(bool disposing){if(disposing)rowSize.Dispose();base.Dispose(disposing);}
    }
}
