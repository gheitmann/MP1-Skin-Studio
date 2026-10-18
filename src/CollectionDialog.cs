using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class CollectionDialog : Form
    {
        readonly ChoiceBox mode=new ChoiceBox();readonly NumericUpDown focus=new NumericUpDown{Minimum=1,Maximum=50};readonly TextBox lines=new TextBox();
        public CollectionSettings Result{get;private set;}
        public CollectionDialog(CollectionSettings settings)
        {
            Text="Collection Preview · MP1 Skin Studio";Font=new Font("Segoe UI",10);ClientSize=new Size(940,600);MinimumSize=new Size(820,500);StartPosition=FormStartPosition.CenterParent;
            var top=new Panel{Dock=DockStyle.Top,Height=145,Width=ClientSize.Width};top.Controls.Add(new Label{Text="View",Location=new Point(20,22),AutoSize=true});mode.SetBounds(120,16,245,30);mode.Items.AddRange(new object[]{"Automatic","List","Small icons","Large icons","Filmstrip","Album list","Playlist","Coverflow (approximate)"});mode.SelectedIndex=Array.IndexOf(CollectionSettings.Modes,settings.Mode);mode.BackColor=Color.White;mode.ForeColor=Color.Black;top.Controls.Add(mode);
            top.Controls.Add(new Label{Text="Selected item",Location=new Point(420,22),AutoSize=true});focus.SetBounds(605,18,80,28);focus.Value=settings.Focus+1;top.Controls.Add(focus);
            top.Controls.Add(new Label{Text="Only the matching facade subview is rendered; otherwise the first available view is used.\nOne item per line: Title | Additional text | Optional image relative to Media. Maximum 50 items.\nSample data is saved in the editor project and does not change skin XML.",Location=new Point(20,67),Size=new Size(890,76),Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top});
            lines.Dock=DockStyle.Fill;lines.Multiline=true;lines.AcceptsReturn=true;lines.ScrollBars=ScrollBars.Both;lines.WordWrap=false;lines.MaxLength=60000;lines.Font=new Font("Consolas",10);lines.Text=String.Join(Environment.NewLine,settings.Items.Select(i=>i.Title+" | "+i.Detail+" | "+i.Image));
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=55,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,10,12,8)};var accept=new Button{Text="Apply",Width=140,Height=32};accept.Click+=(s,e)=>{try{AcceptSettings();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Validate collection",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};var cancel=new Button{Text="Cancel",Width=130,Height=32,DialogResult=DialogResult.Cancel};bottom.Controls.Add(accept);bottom.Controls.Add(cancel);CancelButton=cancel;Controls.Add(lines);Controls.Add(bottom);Controls.Add(top);
        }
        CollectionSettings Collect()
        {
            var result=new CollectionSettings{Mode=CollectionSettings.Modes[Math.Max(0,mode.SelectedIndex)],Focus=(int)focus.Value-1};result.Items.Clear();
            foreach(string line in lines.Lines.Where(s=>s.Trim().Length>0)){var parts=line.Split('|');if(parts.Length>3)throw new InvalidOperationException("Use no more than three fields per item: Title | Details | Image.");result.Items.Add(new PreviewItem{Title=parts[0].Trim(),Detail=parts.Length>1?parts[1].Trim():"",Image=parts.Length>2?parts[2].Trim():""});}
            result.Validate();return result;
        }
        void AcceptSettings(){Result=Collect();DialogResult=DialogResult.OK;Close();}
        internal void ConfigureForTest(string name,int selection,string text){mode.SelectedIndex=Array.IndexOf(CollectionSettings.Modes,name);focus.Value=selection;lines.Text=text;}
        internal CollectionSettings CollectForTest(){return Collect();}
        internal void SubmitForTest(){AcceptSettings();}
        public void RenderTo(string path){Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,Size));image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
    }
    public sealed partial class StudioForm
    {
        void ShowCollectionSettings(){if(PendingProperties())throw new InvalidOperationException("Apply pending property edits first.");using(var dialog=new CollectionDialog(project.Editor.Collection))if(dialog.ShowDialog(this)==DialogResult.OK)ApplyCollectionSettings(dialog.Result);}
        void ApplyCollectionSettings(CollectionSettings settings)
        {project.Editor.Collection.SetFrom(settings);state.UpdateCollectionValues();UpdatePreviewTree();canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice("Collection preview applied. Save the editor project; skin XML is unchanged.");}
        public void PrepareCollectionPreview(string mode){LoadProject(DemoRoot(),"demo.collections.xml");var settings=project.Editor.Collection.Copy();settings.Mode=mode;ApplyCollectionSettings(settings);}
        public void RenderCollectionDialogTo(string path){using(var dialog=new CollectionDialog(project.Editor.Collection))dialog.RenderTo(path);}
    }
}
