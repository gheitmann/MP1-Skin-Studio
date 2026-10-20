using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class LanguageDialog : Form
    {
        readonly LanguagePreview working;readonly Label summary;readonly TextBox search;readonly CheckBox prefixes;readonly DataGridView grid;readonly bool picking;List<int> ids=new List<int>();Button use;
        public LanguagePreview Result{get;private set;}public int? SelectedId{get;private set;}
        public LanguageDialog(LanguagePreview current,bool pick=false)
        {
            picking=pick;working=current.Copy();Text=pick?"Select Language Text - MP1 Skin Studio":"Preview Language - MP1 Skin Studio";Size=new Size(1050,740);MinimumSize=new Size(860,650);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(225,234,244);
            var top=new Panel{Dock=DockStyle.Top,Height=173,Width=ClientSize.Width};top.Controls.Add(new Label{Text=pick?"SELECT LANGUAGE TEXT ID":"LANGUAGE TEXT FOR PREVIEWS",Font=new Font(Font,FontStyle.Bold),Location=new Point(14,12),Size=new Size(800,25)});
            var buttons=new FlowLayoutPanel{Location=new Point(14,44),Size=new Size(990,36),WrapContents=false,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};top.Controls.Add(buttons);
            buttons.Controls.Add(Command("Load primary language",()=>Import(false)));buttons.Controls.Add(Command("Load fallback",()=>Import(true)));buttons.Controls.Add(Command("Clear primary language",()=>{working.Primary=null;RefreshList();}));buttons.Controls.Add(Command("Clear fallback",()=>{working.Fallback=null;RefreshList();}));buttons.Visible=!pick;
            summary=new Label{Location=new Point(14,83),Size=new Size(980,38),Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right,AutoEllipsis=true};top.Controls.Add(summary);
            prefixes=new CheckBox{Text="Show prefixes (for example My)",Checked=working.Prefixes,Location=new Point(14,128),Size=new Size(310,29),Enabled=!pick};prefixes.CheckedChanged+=(s,e)=>{working.Prefixes=prefixes.Checked;RefreshList();};top.Controls.Add(prefixes);
            search=new TextBox{Location=new Point(410,130),Width=430,BackColor=Color.FromArgb(18,24,33),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,AccessibleName="Search language entries by ID or text"};search.TextChanged+=(s,e)=>RefreshList();top.Controls.Add(search);top.Controls.Add(new Label{Text="Search ID / text",Location=new Point(310,133),Size=new Size(100,23)});
            grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,VirtualMode=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=Color.FromArgb(18,24,33),BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false};
            grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(36,48,64);grid.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;grid.DefaultCellStyle.BackColor=Color.FromArgb(20,27,37);grid.DefaultCellStyle.ForeColor=ForeColor;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(25,89,135);grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.GridColor=Color.FromArgb(55,71,91);grid.RowTemplate.Height=27;
            grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="ID",Width=95,SortMode=DataGridViewColumnSortMode.NotSortable});grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Effective text",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,SortMode=DataGridViewColumnSortMode.NotSortable});grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Language / source",Width=210,SortMode=DataGridViewColumnSortMode.NotSortable});
            grid.CellValueNeeded+=(s,e)=>{if(e.RowIndex<0||e.RowIndex>=ids.Count)return;int id=ids[e.RowIndex];string text,origin;working.TryGet(id,out text,out origin);e.Value=e.ColumnIndex==0?(object)id:e.ColumnIndex==1?text:origin;};grid.SelectionChanged+=(s,e)=>UpdateUse();grid.CellDoubleClick+=(s,e)=>{if(picking&&e.RowIndex>=0)AcceptLanguage();};
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=99,Padding=new Padding(14,7,14,12)};bottom.Controls.Add(new Label{Text="Language files are read-only and saved as preview snapshots in editor projects. Skin XML and original language files remain unchanged. Fallback is used only for missing IDs.",Dock=DockStyle.Top,Height=39,ForeColor=Color.FromArgb(149,168,189)});
            var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=36,FlowDirection=FlowDirection.RightToLeft};bottom.Controls.Add(actions);use=Command(pick?"Select ID":"Apply",AcceptLanguage);use.Height=32;actions.Controls.Add(use);var cancel=Command("Cancel",()=>{DialogResult=DialogResult.Cancel;Close();});cancel.Height=32;cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(cancel);CancelButton=cancel;
            Controls.Add(grid);Controls.Add(top);Controls.Add(bottom);RefreshList();
        }
        Button Command(string text,Action action){var button=new Button{Text=text,Width=165,Height=31,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(29,43,59),ForeColor=ForeColor};button.Click+=(s,e)=>action();return button;}
        void Import(bool fallback)
        {
            using(var dialog=new OpenFileDialog{Filter="MediaPortal language file|strings_*.xml|XML files|*.xml",Title=fallback?"Choose fallback language file":"Choose primary language file"})if(dialog.ShowDialog(this)==DialogResult.OK)
                try{var pack=LanguagePack.Load(dialog.FileName);if(fallback)working.Fallback=pack;else working.Primary=pack;RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Could not load language file",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        void RefreshList()
        {
            if(search==null||grid==null)return;string query=search.Text.Trim();ids=working.Ids().Where(id=>{string text,origin;working.TryGet(id,out text,out origin);return query.Length==0||id.ToString().Contains(query)||text.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0;}).ToList();grid.RowCount=ids.Count;grid.Invalidate();
            summary.Text="Primary language: "+(working.Primary==null?"not loaded":working.Primary.Name+" · "+working.Primary.Count+" entries · "+working.Primary.FileName)+"\nFallback: "+(working.Fallback==null?"not loaded":working.Fallback.Name+" · "+working.Fallback.Count+" entries")+" · "+ids.Count+" matches";UpdateUse();
        }
        void UpdateUse(){if(use!=null)use.Enabled=!picking||(grid.CurrentCell!=null&&grid.CurrentCell.RowIndex<ids.Count);}
        void AcceptLanguage(){UpdateUse();if(!use.Enabled)return;Result=working.Copy();if(picking)SelectedId=ids[grid.CurrentCell.RowIndex];DialogResult=DialogResult.OK;Close();}
        internal void ImportForTest(LanguagePack pack,bool fallback){if(fallback)working.Fallback=pack;else working.Primary=pack;RefreshList();}
        internal void SearchForTest(string query){search.Text=query;}
        internal int VisibleCount{get{return ids.Count;}}
        internal bool SearchFits{get{return search.Right<=search.Parent.ClientSize.Width-10&&search.Width>=150;}}
        internal void PrefixesForTest(bool value){prefixes.Checked=value;}
        internal void AcceptForTest(){AcceptLanguage();}
        public void RenderTo(string path){Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
    }
    public sealed partial class StudioForm
    {
        void ShowLanguageSettings(){if(project==null)return;if(PendingProperties())throw new InvalidOperationException("Apply pending property edits first.");using(var dialog=new LanguageDialog(project.Editor.Language))if(dialog.ShowDialog(this)==DialogResult.OK)ApplyLanguage(dialog.Result);}
        void ApplyLanguage(LanguagePreview language)
        {
            if(PendingProperties())throw new InvalidOperationException("Apply pending property edits first.");project.Editor.Language.SetFrom(language);RebuildElementTree();UpdatePreviewTree();UpdateSelectionTools();if(selected!=null)selectionName.Text=(canvas.SelectedKeys.Count>1?canvas.SelectedKeys.Count+" items · ":"")+DisplayName(selected);canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice("Preview language applied; skin and language files are unchanged. Save the editor project.");
        }
        string DisplayName(SkinControl control){if(!LanguagePreview.UseLabelName(control))return control.Name;return state.Label(control,control.Get("label")).Replace("\r"," ").Replace("\n"," ");}
        public void RenderLanguageTo(string path){using(var dialog=new LanguageDialog(project.Editor.Language))dialog.RenderTo(path);}
        public void LoadPreviewLanguages(string primary,string fallback){ApplyLanguage(new LanguagePreview{Primary=LanguagePack.Load(primary),Fallback=String.IsNullOrEmpty(fallback)?null:LanguagePack.Load(fallback)});}
    }
}
