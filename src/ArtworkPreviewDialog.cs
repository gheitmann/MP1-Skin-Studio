using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class ArtworkPreviewDialog : Form
    {
        readonly SkinProject project;
        readonly PreviewSettings draft;
        readonly ListBox sources=new ListBox();
        readonly TextBox search=new TextBox();
        readonly string[] expressions;
        readonly TextBox sourceText=new TextBox(),sampleText=new TextBox();
        readonly Button choose=new Button(),empty=new Button(),restore=new Button();
        public PreviewSettings Result{get;private set;}
        public ArtworkPreviewDialog(SkinProject skin,string current=null)
        {
            project=skin;draft=skin.Editor.Preview.Copy();Text="Sample Artwork — MP1 Skin Studio";Size=new Size(1040,650);MinimumSize=new Size(820,560);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(224,235,245);
            var top=new Panel{Dock=DockStyle.Top,Height=139};top.Controls.Add(new Label{Dock=DockStyle.Top,Height=94,Padding=new Padding(14,12,14,0),Text="SAMPLE ARTWORK\r\nChoose an image expression, then a skin image or explicit empty artwork. Identical expressions share the sample across pages and themes. Samples affect image controls and Control.HasThumb only; original bindings stay unchanged."});
            var searchRow=new Panel{Dock=DockStyle.Bottom,Height=43,Padding=new Padding(14,4,14,8)};searchRow.Controls.Add(new Label{Text="Search",Dock=DockStyle.Left,Width=70});search.Dock=DockStyle.Fill;search.BackColor=Color.FromArgb(18,24,33);search.ForeColor=ForeColor;searchRow.Controls.Add(search);search.BringToFront();top.Controls.Add(searchRow);
            sources.Dock=DockStyle.Fill;sources.BackColor=Color.FromArgb(18,24,33);sources.ForeColor=ForeColor;sources.HorizontalScrollbar=true;sources.IntegralHeight=false;sources.SelectedIndexChanged+=(s,e)=>UpdateSelection();
            var right=new TableLayoutPanel{Dock=DockStyle.Right,Width=395,Padding=new Padding(12),ColumnCount=1,RowCount=8};
            right.RowStyles.Add(new RowStyle(SizeType.Absolute,27));right.RowStyles.Add(new RowStyle(SizeType.Percent,50));right.RowStyles.Add(new RowStyle(SizeType.Absolute,27));right.RowStyles.Add(new RowStyle(SizeType.Percent,50));right.RowStyles.Add(new RowStyle(SizeType.Absolute,44));right.RowStyles.Add(new RowStyle(SizeType.Absolute,44));right.RowStyles.Add(new RowStyle(SizeType.Absolute,44));right.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
            right.Controls.Add(new Label{Text="Original image expression",Dock=DockStyle.Fill});StyleText(sourceText);right.Controls.Add(sourceText);
            right.Controls.Add(new Label{Text="Current sample",Dock=DockStyle.Fill});StyleText(sampleText);right.Controls.Add(sampleText);
            StyleButton(choose,"Choose skin image …");choose.Click+=(s,e)=>ChooseImage();right.Controls.Add(choose);
            StyleButton(empty,"Empty artwork");empty.Click+=(s,e)=>SetSample("");right.Controls.Add(empty);
            StyleButton(restore,"Use original expression");restore.Click+=(s,e)=>SetSample(null);right.Controls.Add(restore);
            right.Controls.Add(new Label{Dock=DockStyle.Fill,Text="Removing a sample restores the original expression, which may remain unverified. Save the editor project to retain samples."});
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=54,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,8)};
            var apply=new Button{Width=130,Height=33};StyleButton(apply,"Apply",false);apply.Click+=(s,e)=>AcceptSamples();bottom.Controls.Add(apply);
            var cancel=new Button{Width=112,Height=33,DialogResult=DialogResult.Cancel};StyleButton(cancel,"Cancel",false);bottom.Controls.Add(cancel);CancelButton=cancel;
            Controls.Add(sources);Controls.Add(right);Controls.Add(bottom);Controls.Add(top);
            expressions=ArtworkPreview.Sources(project);search.TextChanged+=(s,e)=>FilterSources();FilterSources();if(current!=null&&sources.Items.Contains(current.Trim()))sources.SelectedItem=current.Trim();
        }
        void StyleText(TextBox text){text.ReadOnly=true;text.Multiline=true;text.ScrollBars=ScrollBars.Vertical;text.Dock=DockStyle.Fill;text.BackColor=Color.FromArgb(18,24,33);text.ForeColor=ForeColor;}
        void StyleButton(Button button,string text,bool fill=true){button.Text=text;button.FlatStyle=FlatStyle.Flat;button.ForeColor=ForeColor;if(fill)button.Dock=DockStyle.Fill;}
        string Source{get{return sources.SelectedItem as string;}}
        void FilterSources()
        {string prior=Source;sources.BeginUpdate();try{sources.Items.Clear();sources.Items.AddRange(expressions.Where(x=>x.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0).Cast<object>().ToArray());if(prior!=null&&sources.Items.Contains(prior))sources.SelectedItem=prior;else if(sources.Items.Count>0)sources.SelectedIndex=0;}finally{sources.EndUpdate();}UpdateSelection();}
        void UpdateSelection()
        {
            sourceText.Text=Source??"No image expressions on this page.";string sample;
            sampleText.Text=Source!=null&&draft.Artwork.TryGetValue(Source,out sample)?(sample.Length==0?"Empty artwork (explicit sample)":"Media/"+sample):"Original expression (no sample)";
            choose.Enabled=empty.Enabled=restore.Enabled=Source!=null;
        }
        void ChooseImage()
        {
            if(Source==null)return;string current;draft.Artwork.TryGetValue(Source,out current);
            using(var browser=new TextureBrowser(project,current))if(browser.ShowDialog(this)==DialogResult.OK)SetSample(browser.SelectedPath);
        }
        void SetSample(string sample)
        {if(Source==null)return;try{ArtworkPreview.Set(draft,Source,sample);UpdateSelection();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Sample artwork",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
        void AcceptSamples(){Result=draft.Copy();DialogResult=DialogResult.OK;Close();}
        internal void SelectForTest(string expression){sources.SelectedItem=expression;}
        internal void FilterForTest(string query){search.Text=query;}
        internal void SampleForTest(string sample){SetSample(sample);}
        internal void AcceptForTest(){AcceptSamples();}
    }
    public sealed partial class StudioForm
    {
        void ShowArtworkPreview()
        {
            RequireAppliedProperties();using(var dialog=new ArtworkPreviewDialog(project,selected!=null&&selected.Get("type")=="image"?selected.Get("texture"):null))if(dialog.ShowDialog(this)==DialogResult.OK)
            {ApplyPreviewSettings(dialog.Result);ShowNotice("Artwork samples applied to the preview. Original image bindings and XML history are preserved. Save the editor project.");}
        }
    }
}
