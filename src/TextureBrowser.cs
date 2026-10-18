using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class TextureGrid : ScrollableControl
    {
        const int TileWidth=152,TileHeight=126;
        public List<TextureAsset> Assets=new List<TextureAsset>();
        public TextureAsset[] VisibleAssets=new TextureAsset[0];
        public TextureAsset Selected;
        public event Action SelectionChanged,AcceptSelection;
        readonly Dictionary<string,Bitmap> cache=new Dictionary<string,Bitmap>(StringComparer.OrdinalIgnoreCase);
        public TextureGrid(){DoubleBuffered=true;AutoScroll=true;TabStop=true;BackColor=Color.FromArgb(18,24,33);ForeColor=Color.FromArgb(220,233,244);ResizeRedraw=true;}
        int Columns{get{return Math.Max(1,(ClientSize.Width-20)/TileWidth);}}
        public void Filter(string query)
        {
            VisibleAssets=Assets.Where(a=>a.RelativePath.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            if(Selected==null||!VisibleAssets.Contains(Selected))Selected=VisibleAssets.FirstOrDefault();AutoScrollPosition=Point.Empty;LayoutTiles();
            if(SelectionChanged!=null)SelectionChanged();Invalidate();
        }
        public void SelectRelative(string path)
        {Selected=VisibleAssets.FirstOrDefault(a=>String.Equals(a.RelativePath,path,StringComparison.OrdinalIgnoreCase));if(SelectionChanged!=null)SelectionChanged();Invalidate();}
        void LayoutTiles(){AutoScrollMinSize=new Size(0,((VisibleAssets.Length+Columns-1)/Columns)*TileHeight+14);}
        protected override void OnResize(EventArgs e){base.OnResize(e);LayoutTiles();}
        static void Checker(Graphics g,Rectangle area)
        {
            using(var a=new SolidBrush(Color.FromArgb(42,50,62)))using(var b=new SolidBrush(Color.FromArgb(53,63,76)))
                for(int y=area.Top;y<area.Bottom;y+=10)for(int x=area.Left;x<area.Right;x+=10)g.FillRectangle(((x-area.Left)/10+(y-area.Top)/10)%2==0?a:b,x,y,Math.Min(10,area.Right-x),Math.Min(10,area.Bottom-y));
        }
        public static void PaintChecker(Graphics g,Rectangle area){Checker(g,area);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;g.TranslateTransform(AutoScrollPosition.X,AutoScrollPosition.Y);
            for(int i=0;i<VisibleAssets.Length;i++)
            {
                var rect=new Rectangle(8+i%Columns*TileWidth,8+i/Columns*TileHeight,TileWidth-10,TileHeight-10);
                if(rect.Bottom+AutoScrollPosition.Y<0||rect.Top+AutoScrollPosition.Y>Height)continue;
                var asset=VisibleAssets[i];bool chosen=Selected==asset;
                using(var fill=new SolidBrush(chosen?Color.FromArgb(28,74,109):Color.FromArgb(28,36,48)))g.FillRectangle(fill,rect);
                var imageArea=new Rectangle(rect.X+7,rect.Y+6,rect.Width-14,73);Checker(g,imageArea);
                Bitmap thumbnail;
                if(!cache.TryGetValue(asset.PhysicalPath,out thumbnail))
                {
                    if(cache.Count>=128){string first=cache.Keys.First();if(cache[first]!=null)cache[first].Dispose();cache.Remove(first);}
                    string error;thumbnail=TextureCatalog.Thumbnail(asset,imageArea.Width,imageArea.Height,out error);cache[asset.PhysicalPath]=thumbnail;
                }
                if(thumbnail!=null)g.DrawImageUnscaled(thumbnail,imageArea.Location);
                else TextRenderer.DrawText(g,"No preview",Font,imageArea,Color.FromArgb(244,190,107),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                var label=new Rectangle(rect.X+7,rect.Y+84,rect.Width-14,27);TextRenderer.DrawText(g,asset.RelativePath,Font,label,ForeColor,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.WordBreak);
                using(var pen=new Pen(chosen?Color.FromArgb(85,185,255):Color.FromArgb(58,76,96),chosen?2:1))g.DrawRectangle(pen,rect);
            }
            if(VisibleAssets.Length==0)TextRenderer.DrawText(g,"No matching images in the Media directory.",Font,new Point(18,22),ForeColor);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);Focus();int x=e.X-AutoScrollPosition.X-8,y=e.Y-AutoScrollPosition.Y-8;if(x<0||y<0)return;
            int index=y/TileHeight*Columns+x/TileWidth;if(x/TileWidth>=Columns||index>=VisibleAssets.Length)return;
            Selected=VisibleAssets[index];if(SelectionChanged!=null)SelectionChanged();Invalidate();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(Selected!=null&&AcceptSelection!=null)AcceptSelection();}
        protected override bool IsInputKey(Keys keyData){return new[]{Keys.Left,Keys.Right,Keys.Up,Keys.Down}.Contains(keyData)||base.IsInputKey(keyData);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);int current=Array.IndexOf(VisibleAssets,Selected),next=current;
            if(e.KeyCode==Keys.Left)next--;else if(e.KeyCode==Keys.Right)next++;else if(e.KeyCode==Keys.Up)next-=Columns;else if(e.KeyCode==Keys.Down)next+=Columns;else if(e.KeyCode==Keys.Enter&&AcceptSelection!=null){AcceptSelection();e.Handled=true;return;}else return;
            if(VisibleAssets.Length>0){Selected=VisibleAssets[Math.Max(0,Math.Min(VisibleAssets.Length-1,next))];if(SelectionChanged!=null)SelectionChanged();Invalidate();}e.Handled=true;
        }
        protected override void Dispose(bool disposing){if(disposing)foreach(var bitmap in cache.Values)if(bitmap!=null)bitmap.Dispose();base.Dispose(disposing);}
    }
    public sealed class TextureBrowser : Form
    {
        TextureGrid grid=new TextureGrid();TextBox search=new TextBox();Label detail=new Label(),count=new Label();Panel preview=new Panel();Button choose=new Button();Bitmap large;
        public string SelectedPath{get{return grid.Selected==null?null:grid.Selected.RelativePath;}}
        public int MatchCount{get{return grid.VisibleAssets.Length;}}
        public TextureBrowser(SkinProject project,string current)
        {
            Text="Select Texture · MP1 Skin Studio";Size=new Size(1020,680);MinimumSize=new Size(800,540);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(224,235,245);
            var top=new Panel{Dock=DockStyle.Top,Height=87,Padding=new Padding(14)};
            top.Controls.Add(new Label{Text="TEXTURES FROM YOUR SKIN",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Location=new Point(14,12)});
            top.Controls.Add(new Label{Text="Search",AutoSize=true,Location=new Point(14,50)});search.SetBounds(78,45,665,27);search.BackColor=Color.FromArgb(16,23,32);search.ForeColor=ForeColor;top.Controls.Add(search);
            count.SetBounds(770,48,200,27);top.Controls.Add(count);
            top.Resize+=(s,e)=>{search.Width=Math.Max(120,top.Width-310);count.Left=Math.Max(215,top.Width-215);};
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,8)};
            choose.Text="Apply";choose.Width=128;choose.Height=32;choose.FlatStyle=FlatStyle.Flat;choose.BackColor=Color.FromArgb(25,112,170);choose.ForeColor=ForeColor;choose.Click+=(s,e)=>AcceptChoice();bottom.Controls.Add(choose);
            var cancel=new Button{Text="Cancel",Width=110,Height=32,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);CancelButton=cancel;
            var right=new Panel{Dock=DockStyle.Right,Width=253,Padding=new Padding(12)};preview.Dock=DockStyle.Top;preview.Height=224;preview.BackColor=Color.FromArgb(18,24,33);preview.Paint+=(s,e)=>{TextureGrid.PaintChecker(e.Graphics,preview.ClientRectangle);if(large!=null)e.Graphics.DrawImageUnscaled(large,(preview.Width-large.Width)/2,(preview.Height-large.Height)/2);};
            detail.Dock=DockStyle.Fill;detail.Padding=new Padding(0,14,0,0);detail.ForeColor=ForeColor;right.Controls.Add(detail);right.Controls.Add(preview);
            grid.Dock=DockStyle.Fill;Controls.Add(grid);Controls.Add(right);Controls.Add(top);Controls.Add(bottom);
            grid.Assets=TextureCatalog.List(project);grid.SelectionChanged+=UpdateSelection;grid.AcceptSelection+=AcceptChoice;
            search.TextChanged+=(s,e)=>{grid.Filter(search.Text);UpdateCount();};grid.Filter("");if(!String.IsNullOrWhiteSpace(current))grid.SelectRelative(current.Replace('\\','/'));UpdateCount();
        }
        void UpdateCount(){count.Text=MatchCount+" / "+grid.Assets.Count+" images";}
        void UpdateSelection()
        {
            if(large!=null){large.Dispose();large=null;}
            if(grid.Selected==null){choose.Enabled=false;detail.Text="Select an image. PNG, JPG, BMP and GIF are supported.";preview.Invalidate();return;}
            string error;var a=grid.Selected;large=TextureCatalog.Thumbnail(a,228,224,out error);choose.Enabled=large!=null;
            detail.Text=a.RelativePath+"\n\n"+(large==null?"No preview: "+error:a.Width+" × "+a.Height+" Pixel")+"\n\n"+(a.Size/1024.0).ToString("0.0")+" KB\n\nQuelle: "+(a.FromTheme?"Selected theme":"Base skin")+"\n\nTransparency is shown on a checkerboard.\n\nThe image path is applied. The image file remains in the skin directory.";preview.Invalidate();
        }
        void AcceptChoice(){if(grid.Selected!=null&&large!=null){DialogResult=DialogResult.OK;Close();}}
        public void Filter(string query){search.Text=query;}
        public void SelectRelative(string path){grid.SelectRelative(path);}
        public bool SelectionValid{get{return choose.Enabled;}}
        public void AcceptForTest(){choose.PerformClick();}
        public void RenderTo(string path)
        {Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,ImageFormat.Png);}Hide();}
        protected override void Dispose(bool disposing){if(disposing&&large!=null){large.Dispose();large=null;}base.Dispose(disposing);}
    }
}
