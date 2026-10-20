using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class FontPickerDialog : Form
    {
        readonly SkinProject project;
        readonly ListBox names=new ListBox();
        readonly TextBox search=new TextBox(),sample=new TextBox();
        readonly Label details=new Label();
        readonly Panel preview=new Panel();
        readonly Button choose=new Button();
        readonly string[] available;
        Font sampleFont;
        public string SelectedFontName {get{return names.SelectedItem as string;}}
        public string PreviewDetails {get{return details.Text;}}
        public FontPickerDialog(SkinProject skin,string current,string caption)
        {
            project=skin;available=skin.FontNames.Distinct().OrderBy(n=>n,StringComparer.OrdinalIgnoreCase).ToArray();
            Text="Choose font · MP1 Skin Studio";ClientSize=new Size(790,490);MinimumSize=new Size(700,460);StartPosition=FormStartPosition.CenterParent;
            AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);
            var left=new Panel{Dock=DockStyle.Left,Width=260,Padding=new Padding(12)};
            search.Dock=DockStyle.Top;search.AccessibleName="Filter skin fonts";StyleInput(search);
            names.Dock=DockStyle.Fill;names.IntegralHeight=false;names.BorderStyle=BorderStyle.None;names.BackColor=Color.FromArgb(20,26,35);names.ForeColor=ForeColor;
            left.Controls.Add(new DarkScrollHost(names){Dock=DockStyle.Fill});left.Controls.Add(new Panel{Dock=DockStyle.Top,Height=8});left.Controls.Add(search);left.Controls.Add(new Label{Text="FILTER FONTS",Dock=DockStyle.Top,Height=26});Controls.Add(left);
            var right=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12)};Controls.Add(right);right.BringToFront();
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=46};
            choose.Text="Use font";choose.SetBounds(0,10,115,29);choose.FlatStyle=FlatStyle.Flat;choose.Click+=(s,e)=>{if(SelectedFontName!=null){DialogResult=DialogResult.OK;Close();}};
            var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat};cancel.SetBounds(125,10,100,29);bottom.Controls.Add(choose);bottom.Controls.Add(cancel);right.Controls.Add(bottom);CancelButton=cancel;AcceptButton=choose;
            details.Dock=DockStyle.Top;details.Height=100;details.Padding=new Padding(0,10,0,0);right.Controls.Add(details);
            sample.Text=String.IsNullOrWhiteSpace(caption)?"Movies, music and photos · Aa Bb 0123456789":caption.Substring(0,Math.Min(512,caption.Length));sample.MaxLength=512;sample.Dock=DockStyle.Top;sample.AccessibleName="Font preview sample text";StyleInput(sample);right.Controls.Add(sample);
            var title=new Label{Text="SAMPLE TEXT · editable",Dock=DockStyle.Top,Height=28};right.Controls.Add(title);
            preview.Dock=DockStyle.Fill;preview.BackColor=Color.FromArgb(20,26,35);preview.Paint+=PaintSample;right.Controls.Add(preview);preview.BringToFront();
            sample.TextChanged+=(s,e)=>preview.Invalidate();names.SelectedIndexChanged+=(s,e)=>UpdatePreview();search.TextChanged+=(s,e)=>Filter();
            Filter();if(current!=null&&names.Items.Contains(current))names.SelectedItem=current;
        }
        void StyleInput(TextBox box){box.BackColor=Color.FromArgb(20,26,35);box.ForeColor=ForeColor;box.BorderStyle=BorderStyle.FixedSingle;}
        void Filter()
        {
            string previous=SelectedFontName;names.BeginUpdate();names.Items.Clear();foreach(string name in available)if(name.IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0)names.Items.Add(name);
            if(previous!=null&&names.Items.Contains(previous))names.SelectedItem=previous;else if(names.Items.Count>0)names.SelectedIndex=0;names.EndUpdate();UpdatePreview();
        }
        void UpdatePreview()
        {
            if(sampleFont!=null){sampleFont.Dispose();sampleFont=null;}choose.Enabled=SelectedFontName!=null;
            if(!choose.Enabled){details.Text="No matching fonts. Clear the filter or load a skin with fonts.xml.";preview.Invalidate();return;}
            SkinFontSpec spec;bool defined=project.FontSpecs.TryGetValue(SelectedFontName,out spec);
            if(!defined){float height;if(!project.FontSizes.TryGetValue(SelectedFontName,out height)||height<=0||Single.IsNaN(height)||Single.IsInfinity(height))height=18;spec=new SkinFontSpec{Family="Segoe UI",Height=height};}
            bool fallback=false;FontFamily family;
            try{family=new FontFamily(spec.Family);}catch(ArgumentException){family=new FontFamily("Segoe UI");fallback=true;}
            using(family)
            {
                FontStyle style=spec.Style;if(!family.IsStyleAvailable(style))style=FontStyle.Regular;
                sampleFont=new Font(family,spec.Pixels(project.Height),style,GraphicsUnit.Pixel);
            }
            details.Text=SelectedFontName+" · skin height "+spec.Height.ToString("0.##",CultureInfo.InvariantCulture)+"\n"+"Preview: "+sampleFont.Name+" · "+sampleFont.Size.ToString("0.#",CultureInfo.InvariantCulture)+" px · "+sampleFont.Style+"\n"+(fallback?"Substitute for unavailable font: "+spec.Family:!defined?"Font definition unavailable; preview uses fallback settings.":"At 100% skin scale; approximate Windows font rendering.");
            preview.Invalidate();
        }
        void PaintSample(object sender,PaintEventArgs e)
        {
            if(sampleFont==null)return;e.Graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;
            using(var brush=new SolidBrush(ForeColor))using(var format=new StringFormat(StringFormat.GenericTypographic)){format.Trimming=StringTrimming.EllipsisCharacter;e.Graphics.DrawString(sample.Text,sampleFont,brush,new RectangleF(12,12,Math.Max(1,preview.Width-24),Math.Max(1,preview.Height-24)),format);}
        }
        protected override void Dispose(bool disposing){if(disposing&&sampleFont!=null){sampleFont.Dispose();sampleFont=null;}base.Dispose(disposing);}
    }
}
