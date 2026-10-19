using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    // A small selector drawn by the application also works with DrawToBitmap.
    // Native ComboBox child windows omit their text in off-screen renders.
    public sealed class ChoiceBox : Control
    {
        public readonly List<object> Items=new List<object>();
        int index=-1;
        readonly string menuKey="choice:"+Guid.NewGuid().ToString("N");
        public event EventHandler SelectedIndexChanged;
        public object SelectedItem {get{return index>=0&&index<Items.Count?Items[index]:null;}}
        public int SelectedIndex
        {
            get{return index;}
            set{index=value>=0&&value<Items.Count?value:-1;Invalidate();if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}
        }
        public ChoiceBox(){DoubleBuffered=true;TabStop=true;AccessibleRole=AccessibleRole.ComboBox;Cursor=Cursors.Hand;}
        public int FindStringExact(string text){return Items.FindIndex(item=>String.Equals(item.ToString(),text,StringComparison.OrdinalIgnoreCase));}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);e.Graphics.Clear(BackColor);
            using(var pen=new Pen(Focused?Color.FromArgb(92,185,245):Color.FromArgb(65,82,101)))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
            if(SelectedItem!=null)TextRenderer.DrawText(e.Graphics,SelectedItem.ToString(),Font,new Rectangle(7,1,Width-31,Height-2),ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            using(var brush=new SolidBrush(ForeColor))e.Graphics.FillPolygon(brush,new[]{new Point(Width-17,Height/2-2),new Point(Width-9,Height/2-2),new Point(Width-13,Height/2+2)});
        }
        StudioMenu OpenChoices()
        {
            Focus();if(Items.Count==0)return null;var menu=StudioMenus.For(this,menuKey);
            for(int i=0;i<Items.Count;i++){int chosen=i;var item=(ToolStripMenuItem)menu.Items.Add(Items[i].ToString().Replace("&","&&"),null,(s,e)=>SelectedIndex=chosen);item.Checked=i==index;}
            menu.Show(this,new Point(0,Height));return menu;
        }
        internal StudioMenu OpenChoicesForTest(){return OpenChoices();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)OpenChoices();}
        protected override bool IsInputKey(Keys keyData){return keyData==Keys.Up||keyData==Keys.Down||base.IsInputKey(keyData);}
        protected override void OnKeyDown(KeyEventArgs e)
        {base.OnKeyDown(e);if(e.KeyCode==Keys.Down){SelectedIndex=Math.Min(Items.Count-1,index+1);e.Handled=true;}else if(e.KeyCode==Keys.Up){SelectedIndex=Math.Max(0,index-1);e.Handled=true;}else if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OpenChoices();e.Handled=true;}}
        protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
        protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    }

    public sealed partial class SkinCanvas : Control
    {
        public SkinProject Project;
        public PreviewState State;
        public SkinControl Selected;
        public bool ShowHidden, FocusPreview = true, Grid = true, Snap = true, UnlockBindings;
        public event Action<SkinControl> Selection;
        public event Action<SkinControl, Dictionary<string,string>> Commit;
        public event Action<string> Notice;
        readonly Dictionary<string,Bitmap> images = new Dictionary<string,Bitmap>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        PointF origin; float scale = 1;
        bool dragging, resizing; PointF mouseStart; float oldX,oldY,oldW,oldH;
        public SkinCanvas()
        {
            DoubleBuffered=true; ResizeRedraw=true; BackColor=Color.FromArgb(17,21,29); Dock=DockStyle.Fill; TabStop=true;
            SetStyle(ControlStyles.Selectable,true);
            SetStyle(ControlStyles.StandardClick|ControlStyles.StandardDoubleClick,true);
        }
        public void ResetAssets() { foreach(var b in images.Values)b.Dispose(); images.Clear();missing.Clear();ResetFonts(); }
        protected override void Dispose(bool disposing) { if(disposing) ResetAssets();base.Dispose(disposing); }
        public string[] Missing { get { return missing.ToArray(); } }
        public RectangleF BoundsOf(SkinControl c)
        {
            if(Project==null)return LayoutTools.Bounds(c);
            if(c.Get("type")=="facadeview"){var template=CollectionSettings.Template(Project,c,Project.Editor.Collection.Mode);if(template!=null)c=template;}
            return (paintingLayout??new StackPreview(Project,State)).Bounds(c);
        }
        StackPreview paintingLayout;
        RectangleF EffectiveBounds(SkinControl c) {RectangleF r;return dragging&&dragRects.TryGetValue(c.Key,out r)?r:BoundsOf(c);}
        bool Drawn(SkinControl c)
        {
            if(Project.IsEditorHidden(c))return false;
            if(!CollectionSettings.Active(Project,c))return false;
            bool? visible=State.Visible(c);if(!ShowHidden&&visible==false)return false;if(!Project.Editor.ShowUnresolved&&visible==null)return false;
            if(!ShowHidden&&c.Resolved("Visibility")=="Collapsed")return false;
            var r=EffectiveBounds(c);
            return r.Width>2&&r.Height>2&&r.Right>0&&r.Bottom>0&&r.X<Project.Width&&r.Y<Project.Height;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            if(Project==null){TextRenderer.DrawText(g,"Open a skin directory or load the demo",Font,ClientRectangle,Color.Gray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
            paintingLayout=new StackPreview(Project,State);
            var textPass=new List<PendingSurfaceText>();pendingSurfaceText=textPass;
            try {
            UpdateTransform();
            using(var shadow=new SolidBrush(Color.FromArgb(9,12,18)))g.FillRectangle(shadow,origin.X+8,origin.Y+12,Project.Width*scale,Project.Height*scale);
            g.TranslateTransform(origin.X,origin.Y);g.ScaleTransform(scale,scale);
            g.SetClip(new RectangleF(0,0,Project.Width,Project.Height));
            using(var bg=new LinearGradientBrush(new Rectangle(0,0,Project.Width,Project.Height),Color.FromArgb(22,44,68),Color.FromArgb(8,15,25),60f))g.FillRectangle(bg,0,0,Project.Width,Project.Height);
            if(Grid)using(var pen=new Pen(Color.FromArgb(30,120,150,180),1/scale))
            {for(int x=0;x<Project.Width;x+=100)g.DrawLine(pen,x,0,x,Project.Height);for(int y=0;y<Project.Height;y+=100)g.DrawLine(pen,0,y,Project.Width,y);}
            foreach(var c in Project.Controls)
            {
                if(!Drawn(c))continue;
                DrawControl(g,c,EffectiveBounds(c));
            }
            FlushSurfaceText(g);
            DrawDesignerOverlay(g);
            g.ResetClip();g.ResetTransform();
            string footer=Project.Width+" × "+Project.Height+"  ·  "+Math.Round(scale*100)+" %  ·  Approximate layout";
            TextRenderer.DrawText(g,footer,Font,new Point(12,ClientSize.Height-25),Color.FromArgb(143,162,183));
            } finally {paintingLayout=null;pendingSurfaceText=null;foreach(var text in textPass)text.Clip.Dispose();}
        }
        Color ParseColor(string text, Color fallback)
        {
            text=text.Trim().TrimStart('#');uint n;
            if(UInt32.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out n))
            {if(text.Length<=6)n|=0xff000000;return Color.FromArgb(unchecked((int)n));}
            var c=Color.FromName(text);return c.IsKnownColor?c:fallback;
        }
        Bitmap Texture(SkinControl c,string name)
        {
            name=State.Text(c,name).Trim();if(name.Length==0||name=="-"||name.Contains("#"))return null;
            try
            {
                string path=Project.Resolve("Media/"+name);Bitmap image;
                if(images.TryGetValue(path,out image))return image;
                if(!File.Exists(path)){missing.Add(name);return null;}
                using(var stream=File.OpenRead(path))using(var original=Image.FromStream(stream))
                {if(original.Width>10000||original.Height>10000)return null; image=new Bitmap(original);}
                images[path]=image;return image;
            }
            catch { missing.Add(name);return null; }
        }
        void DrawControl(Graphics g,SkinControl c,RectangleF rect)
        {
            string type=c.Get("type");bool focused=FocusPreview&&SkinProject.Focusable(c)&&SkinProject.ControlId(c)>0&&SkinProject.ControlId(c)==State.FocusId;
            bool? visible=State.Visible(c);
            if(CollectionSettings.IsCollection(c)){DrawCollection(g,c,rect);return;}
            string texture=c.Get("texture");
            if(type.Contains("button"))texture=focused?c.Get("textureFocus"):c.Get("textureNoFocus");
            Bitmap image=Texture(c,texture);
            if(image!=null)
            {
                SurfaceImage(g,c,image,rect);
            }
            else if(ShowTexturePlaceholder(c,texture)&&(type.Contains("button")||type=="listcontrol"||type=="facadeview"||type=="image"||type=="videowindow"||type=="filmstrip"||type=="coverflow"))
            {
                if(rect.Width*rect.Height<Project.Width*Project.Height*.8f)
                {
                    Color fill=type.Contains("button")&&focused?Color.FromArgb(245,31,118,181):Color.FromArgb(150,32,54,77);
                    using(var brush=new SolidBrush(fill))g.FillRectangle(brush,rect);
                    using(var pen=new Pen(Color.FromArgb(110,106,144,177),1/scale)){if(type=="image")pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);}
                    if(type=="image"||type=="videowindow")DrawText(g,type=="videowindow"?"VIDEO PREVIEW":System.IO.Path.GetFileName(texture),rect,20,Color.FromArgb(169,190,209),"center");
                }
            }
            string label=State.Label(c,c.Get("label"));
            if((Project.Editor.ShowUnresolved||State.ResolvedLabel(c,c.Get("label")))&&(type=="label"||type=="fadelabel"||type=="textbox"||type=="textboxscrollup"||type.Contains("button")))
            {
                SurfaceText(g,c,label,SurfaceGeometry.TextArea(c,rect),focused,"font","textcolor","align",type=="textbox"||type=="textboxscrollup");
            }
            if(visible==null||visible==false)
            {
                using(var pen=new Pen(visible==false?Color.FromArgb(150,130,150,175):Color.FromArgb(170,232,174,78),1/scale))
                {pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);}
            }
        }
        bool ShowTexturePlaceholder(SkinControl c,string texture)
        {
            string resolved;bool known=State.TryText(c,texture,out resolved);
            if(known&&(resolved.Trim()=="-"||(c.Get("type")=="image"&&resolved.Trim().Length==0)))return false;
            if(c.Get("type")=="image"&&!Project.Editor.ShowUnresolved)return false;
            return known||Project.Editor.ShowUnresolved;
        }
        void DrawText(Graphics g,string text,RectangleF rect,float size,Color color,string align)
        {
            if(String.IsNullOrEmpty(text)||rect.Width<3||rect.Height<3)return;
            using(var font=new Font("Segoe UI",Math.Min(120,size),FontStyle.Regular,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(color))
            using(var format=new StringFormat(StringFormat.GenericTypographic))
            {format.Alignment=align=="right"?StringAlignment.Far:align=="center"?StringAlignment.Center:StringAlignment.Near;format.LineAlignment=StringAlignment.Near;format.Trimming=StringTrimming.EllipsisCharacter;format.FormatFlags=StringFormatFlags.NoWrap;g.DrawString(text,font,brush,rect,format);}
        }
        PointF Logical(Point p){return new PointF((p.X-origin.X)/scale,(p.Y-origin.Y)/scale);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);UpdateTransform();if(!ViewMouseDown(e)&&!PickMouseDown(e))DesignerMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!ViewMouseMove(e))DesignerMouseMove(e);
        }
        float Round(float n){return Snap?(float)Math.Round(n/10)*10:(float)Math.Round(n);}
        internal void ExerciseDrag(SkinControl c,int dx,int dy,bool resize)
        {
            Selected=c;Invalidate();Update();UpdateTransform();
            var r=BoundsOf(c);
            var start=new Point((int)Math.Round(origin.X+(resize?r.Right:r.X+r.Width/2)*scale),(int)Math.Round(origin.Y+(resize?r.Bottom:r.Y+r.Height/2)*scale));
            var end=new Point(start.X+(int)Math.Round(dx*scale),start.Y+(int)Math.Round(dy*scale));
            OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,start.X,start.Y,0));
            OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,end.X,end.Y,0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,end.X,end.Y,0));
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(!ViewMouseUp(e))DesignerMouseUp(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {base.OnKeyDown(e);DesignerKeyDown(e);}
        protected override bool IsInputKey(Keys keyData){return NavigationMode&&new[]{Keys.Left,Keys.Right,Keys.Up,Keys.Down}.Contains(keyData)||base.IsInputKey(keyData);}
    }

    public sealed partial class StudioForm : Form
    {
        readonly Color panel=Color.FromArgb(29,35,46), ink=Color.FromArgb(225,234,244), muted=Color.FromArgb(149,168,189);
        SkinProject project;PreviewState state;SkinControl selected;
        SkinCanvas canvas=new SkinCanvas();TreeView tree=new TreeView();ChoiceBox page=new ChoiceBox(),theme=new ChoiceBox();
        Label selectionName=new Label(),selectionInfo=new Label(),status=new Label(),folderName=new Label();TextBox diagnostics=new TextBox();
        FlowLayoutPanel properties=new FlowLayoutPanel();CheckBox unlock=new CheckBox();
        Button undo,redo,export;ToolTip tips=new ToolTip();
        Dictionary<string,TextBox> fields=new Dictionary<string,TextBox>();Dictionary<string,string> originals=new Dictionary<string,string>();
        HashSet<string> touchedFields=new HashSet<string>(),mixedFields=new HashSet<string>();
        bool loading;
        readonly string[] editable=new[]{"posX","posY","width","height","label","font","textcolor","texture","textureFocus","textureNoFocus","align","onleft","onright","onup","ondown","hyperlink","textXOff","textYOff","textalign","textvalign","valign","colordiffuse","visible"}.Concat(CollectionProperties.Fields.Select(f=>f.Key)).ToArray();
        readonly string[] labels=new[]{"Position X","Position Y","Width","Height","Text / data binding","Font name","Text color · AARRGGBB","Image / texture","Focused image","Unfocused image","Alignment","Navigate left → ID","Navigate right → ID","Navigate up → ID","Navigate down → ID","Target page → Window ID","Text offset X","Text offset Y","Horizontal inner text alignment","Vertical inner text alignment","Vertical text field alignment","Image tint · AARRGGBB","Visibility condition"}.Concat(CollectionProperties.Fields.Select(f=>f.Label)).ToArray();
        public StudioForm(string root)
        {
            Text="MP1 Skin Studio · Prototype 0.9";StartPosition=FormStartPosition.CenterScreen;Size=new Size(1520,960);MinimumSize=new Size(1180,780);
            AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(21,26,35);ForeColor=ink;KeyPreview=true;
            BuildUi();
            canvas.Selection+=SelectControl;
            canvas.PickRequest+=PickOverlapping;canvas.AllowGesture=AllowCanvasGesture;canvas.TextRequest+=c=>Attempt(()=>EditElementText(c));
            canvas.SelectRequest+=(c,toggle)=>SelectControl(c,toggle,!toggle&&canvas.SelectedKeys.Contains(c.Key));
            canvas.CommitBatch+=edits=>Attempt(()=>{string key=selected==null?null:selected.Key;project.EditMany(edits,"Move / resize selection");RefreshProject(key);});canvas.Notice+=ShowNotice;
            canvas.ConnectRequest+=target=>Attempt(()=>ConnectTo(target));canvas.FollowRequest+=direction=>Attempt(()=>FollowNavigation(direction));
            tree.AfterSelect+=(s,e)=>{var c=e.Node.Tag as SkinControl;if(!loading&&c!=null)SelectFromTree(c);};
            page.SelectedIndexChanged+=(s,e)=>{if(!loading&&project!=null&&page.SelectedItem!=null)Attempt(OpenSelectedPage);};
            theme.SelectedIndexChanged+=(s,e)=>{if(!loading&&project!=null)Attempt(ChangeSelectedTheme);};
            FormClosing+=(s,e)=>{if(SessionNeedsSaving()&&MessageBox.Show("Unapplied property values or other session changes have not been saved.\n\nClose and discard unsaved changes?","Editor changes",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)e.Cancel=true;};
            if(Directory.Exists(root))LoadProject(root,"demo.xml");
        }
        Label LabelFor(string text,int height=23) {return new Label{Text=text,AutoSize=false,Height=height,ForeColor=muted,Dock=DockStyle.Top,TextAlign=ContentAlignment.MiddleLeft};}
        Button ButtonFor(string text,Action action,bool accent=false)
        {
            var b=new Button{Text=text,AutoSize=false,Width=145,Height=34,FlatStyle=FlatStyle.Flat,BackColor=accent?Color.FromArgb(30,113,170):panel,ForeColor=ink,Margin=new Padding(0,0,8,0)};
            b.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);b.Click+=(s,e)=>Attempt(action);return b;
        }
        CheckBox Check(string text,bool value,Action<bool> changed)
        {var c=new CheckBox{Text=text,Checked=value,AutoSize=true,ForeColor=ink,Margin=new Padding(0,6,20,0)};c.CheckedChanged+=(s,e)=>{changed(c.Checked);canvas.Invalidate();UpdateDiagnostics();};return c;}
        void BuildUi()
        {
            var header=new Panel{Dock=DockStyle.Top,Height=72,Padding=new Padding(22,12,20,8),BackColor=panel};
            header.Controls.Add(new Label{Text="MP1  /  SKIN STUDIO",Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true,Location=new Point(22,10),ForeColor=ink});
            header.Controls.Add(new Label{Text="Design visually. Preserve existing XML.",AutoSize=true,Location=new Point(24,43),ForeColor=muted});
            var badge=new Label{Text="LAYOUT PROTOTYPE   ·   WITHOUT MP1 RENDERER",AutoSize=true,ForeColor=Color.FromArgb(244,193,102),Anchor=AnchorStyles.Right|AnchorStyles.Top,Location=new Point(1010,27)};header.Controls.Add(badge);header.Resize+=(s,e)=>badge.Left=header.Width-badge.Width-24;
            var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=56,Padding=new Padding(20,11,12,10),WrapContents=false};
            toolbar.Controls.Add(ButtonFor("Open skin directory",OpenFolder,true));var openXml=ButtonFor("Open XML",OpenXml);openXml.Width=110;toolbar.Controls.Add(openXml);var demo=ButtonFor("Titan Demo",()=>{if(CanSwitch())LoadProject(DemoRoot(),"demo.xml");});demo.Width=110;toolbar.Controls.Add(demo);
            undo=ButtonFor("↶ Undo",()=>{if(project!=null){UndoEditor(false);}});undo.Width=125;toolbar.Controls.Add(undo);
            redo=ButtonFor("↷ Redo",()=>{if(project!=null){UndoEditor(true);}});redo.Width=130;toolbar.Controls.Add(redo);
            export=ButtonFor("Export Changes as ZIP",Export,true);export.Width=160;toolbar.Controls.Add(export);
            var openProject=ButtonFor("Open Project",OpenEditorProject);openProject.Width=115;toolbar.Controls.Add(openProject);
            var saveProject=ButtonFor("Save Project",()=>SaveEditorProject(false),true);saveProject.Width=120;toolbar.Controls.Add(saveProject);
            var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Padding=new Padding(16,0,16,10)};
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,245));main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,298));main.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var left=new Panel{Dock=DockStyle.Fill,BackColor=panel,Padding=new Padding(12),Margin=new Padding(0,0,10,0)};
            var leftTop=new Panel{Dock=DockStyle.Top,Height=148};
            folderName=LabelFor("No skin loaded",28);folderName.Dock=DockStyle.None;folderName.SetBounds(0,0,208,26);leftTop.Controls.Add(folderName);
            var tl=LabelFor("THEME",20);tl.Dock=DockStyle.None;tl.SetBounds(0,32,208,20);leftTop.Controls.Add(tl);
            theme.BackColor=Color.FromArgb(20,26,35);theme.ForeColor=ink;theme.SetBounds(0,54,208,26);leftTop.Controls.Add(theme);
            var pl=LabelFor("PAGE / XML",20);pl.Dock=DockStyle.None;pl.SetBounds(0,88,208,20);leftTop.Controls.Add(pl);
            page.BackColor=Color.FromArgb(20,26,35);page.ForeColor=ink;page.SetBounds(0,110,208,26);leftTop.Controls.Add(page);
            tree.Dock=DockStyle.Fill;tree.BackColor=panel;tree.ForeColor=ink;tree.BorderStyle=BorderStyle.None;tree.HideSelection=false;tree.FullRowSelect=true;tree.ShowLines=false;tree.ItemHeight=25;
            left.Controls.Add(tree);left.Controls.Add(BuildElementSearch());left.Controls.Add(leftTop);main.Controls.Add(left,0,0);
            var center=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0,0,10,0)};
            var previewToolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false,Padding=new Padding(8,0,0,0)};
            previewToolbar.Controls.Add(Check("Grid",true,v=>canvas.Grid=v));previewToolbar.Controls.Add(Check("Snap to 10 px",true,v=>canvas.Snap=v));
            previewToolbar.Controls.Add(Check("Focus",true,v=>canvas.FocusPreview=v));previewToolbar.Controls.Add(Check("Show hidden",false,v=>canvas.ShowHidden=v));
            var testData=ButtonFor("Sample Data",ShowPreviewSettings);testData.Width=91;testData.Height=28;testData.Margin=new Padding(0,2,0,0);previewToolbar.Controls.Add(testData);tips.SetToolTip(testData,"Configure sample data and simulated plugin states; presets include long titles, weather and news.");
            var languageButton=ButtonFor("Language",ShowLanguageSettings);languageButton.Width=82;languageButton.Height=28;languageButton.Margin=new Padding(8,2,0,0);previewToolbar.Controls.Add(languageButton);tips.SetToolTip(languageButton,"Load language files for numeric labels; save primary and fallback languages in the editor project.");
            var collectionButton=ButtonFor("Collection",()=>Attempt(ShowCollectionSettings));collectionButton.Width=95;collectionButton.Height=28;collectionButton.Margin=new Padding(8,2,0,0);previewToolbar.Controls.Add(collectionButton);
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=139,Padding=new Padding(10,7,8,5),BackColor=panel};
            bottom.Controls.Add(LabelFor("PREVIEW & INFORMATION",22));
            diagnostics.Dock=DockStyle.Fill;diagnostics.Multiline=true;diagnostics.ReadOnly=true;diagnostics.ScrollBars=ScrollBars.Vertical;diagnostics.BorderStyle=BorderStyle.None;diagnostics.BackColor=panel;diagnostics.ForeColor=muted;diagnostics.Font=new Font("Segoe UI",9);bottom.Controls.Add(diagnostics);diagnostics.BringToFront();
            center.Controls.Add(canvas);center.Controls.Add(BuildViewBar());center.Controls.Add(previewToolbar);center.Controls.Add(BuildDesignerBar());center.Controls.Add(bottom);main.Controls.Add(center,1,0);
            var right=new Panel{Dock=DockStyle.Fill,BackColor=panel,Padding=new Padding(12),Margin=new Padding(0)};
            var propHeader=new Panel{Dock=DockStyle.Top,Height=130};
            selectionName=new Label{Text="Select an element",ForeColor=ink,Font=new Font("Segoe UI",12,FontStyle.Bold),AutoEllipsis=true};selectionName.SetBounds(0,0,270,27);propHeader.Controls.Add(selectionName);
            selectionInfo=new Label{Text="Click in the preview or element tree.",ForeColor=muted};selectionInfo.SetBounds(0,31,270,63);propHeader.Controls.Add(selectionInfo);
            unlock=new CheckBox{Text="Replace bindings locally",AutoSize=true,ForeColor=Color.FromArgb(243,191,102)};unlock.SetBounds(0,101,267,25);unlock.CheckedChanged+=(s,e)=>{canvas.UnlockBindings=unlock.Checked;UpdateResetTools();};propHeader.Controls.Add(unlock);
            properties.Dock=DockStyle.Fill;properties.FlowDirection=FlowDirection.TopDown;properties.WrapContents=false;properties.AutoScroll=true;
            var propertyActions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38,WrapContents=false,Margin=Padding.Empty};
            var apply=ButtonFor("Apply Properties",Apply,true);apply.Width=150;apply.Height=38;apply.Margin=Padding.Empty;propertyActions.Controls.Add(apply);
            var revert=ButtonFor("Revert Inputs",RevertPropertyInputs);revert.Width=105;revert.Height=38;revert.Margin=new Padding(6,0,0,0);propertyActions.Controls.Add(revert);
            tips.SetToolTip(revert,"Restore the property fields from the current model. Applied XML edits and undo history remain unchanged.");
            right.Controls.Add(properties);right.Controls.Add(propHeader);right.Controls.Add(propertyActions);main.Controls.Add(right,2,0);
            status=new Label{Dock=DockStyle.Bottom,Height=31,Padding=new Padding(20,5,0,0),BackColor=panel,ForeColor=muted,Text="Ready"};
            Controls.Add(main);Controls.Add(toolbar);Controls.Add(header);Controls.Add(status);
            tips.SetToolTip(unlock,"Enable only when intentionally replacing an existing #Property or #Define with a local value.");
        }
        public static string DemoRoot(){return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Samples","Titan-Sample");}
        bool CanSwitch(){if(PendingProperties()){ShowNotice("Apply pending property edits or restore their previous values before switching sessions.");return false;}return !SessionNeedsSaving()||MessageBox.Show("The latest changes have not been saved as an editor project or exported as ZIP.\n\nSwitch sessions anyway?","Switch skin",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;}
        void OpenFolder()
        {using(var d=new FolderBrowserDialog{Description="Choose a skin directory containing references.xml, fonts.xml and Media",ShowNewFolderButton=false})if(d.ShowDialog()==DialogResult.OK&&CanSwitch())LoadProject(d.SelectedPath,null);}
        void OpenXml()
        {using(var d=new OpenFileDialog{Filter="MediaPortal Skin-XML|*.xml",Title="Choose an XML file within the skin directory"})if(d.ShowDialog()==DialogResult.OK&&CanSwitch())LoadProject(System.IO.Path.GetDirectoryName(d.FileName),System.IO.Path.GetFileName(d.FileName));}
        public void LoadProject(string root,string requested)
        {InstallProject(new SkinProject(root),requested,null);}
        void InstallProject(SkinProject next,string requested,string editorFile)
        {
            var prepared=PreparedSkinSession.Create(next,requested);
            project=next;currentProjectFile=editorFile;savedProjectSignature=editorFile==null?null:ProjectFile.ContentSignature(project);initialEditorSignature=ProjectFile.EditorSignature(project);savedEditorSignature=initialEditorSignature;exportedSignature=null;state=prepared.Preview;canvas.Project=project;canvas.State=state;canvas.ResetAssets();
            bool prior=loading;loading=true;
            try
            {
                elementSearch.Clear();theme.Items.Clear();theme.Items.Add("Default skin");theme.Items.AddRange(prepared.Themes.Cast<object>());
                theme.SelectedIndex=project.Theme.Length==0?0:theme.FindStringExact(project.Theme);
                PopulatePageChoices(prepared.Pages);
            }
            finally{loading=prior;}
            folderName.Text=System.IO.Path.GetFileName(project.Root.TrimEnd('\\'));tips.SetToolTip(folderName,project.Root);RefreshProject(null);
        }
        void PopulatePageChoices(string[] pages)
        {
            bool prior=loading;loading=true;
            try{page.Items.Clear();page.Items.AddRange(pages.Cast<object>());page.SelectedIndex=page.FindStringExact(project.Page);}
            finally{loading=prior;}
        }
        void RefreshProject(string selectedKey)
        {
            state.UpdateCollectionValues();
            var keep=selectedKey==null?new string[0]:canvas.SelectedKeys.ToArray();
            RebuildElementTree();
            var next=project.Controls.FirstOrDefault(c=>c.Key==selectedKey)??project.Controls.FirstOrDefault(c=>SkinProject.Focusable(c)&&SkinProject.ControlId(c)==state.FocusId)??project.Controls.FirstOrDefault(c=>c.Get("description")=="Movies")??project.Controls.FirstOrDefault(c=>c.Get("description")=="Time")??project.Controls.FirstOrDefault();
            canvas.SelectedKeys.Clear();foreach(var key in keep)if(project.Controls.Any(c=>c.Key==key))canvas.SelectedKeys.Add(key);
            SelectControl(next,false,canvas.SelectedKeys.Count>0);undo.Enabled=project.Undo.Count>0;redo.Enabled=project.Redo.Count>0;export.Enabled=project.Dirty;
            status.Text=project.Controls.Count+" elements  · "+project.Sources.Values.Count(s=>s.Changed)+" modified files  ·  CTRL+click: multiple selection  ·  Originals remain intact";
            canvas.Invalidate();UpdateDiagnostics();
            UpdateProjectCaption();UpdateViewTools();
        }
        void SelectControl(SkinControl c)
        {SelectControl(c,false,false);}
        void SelectControl(SkinControl c,bool toggle,bool retain)
        {
            bool preserveUnlock=retain&&unlock.Checked;
            if(!retain&&!toggle)canvas.SelectedKeys.Clear();
            if(toggle&&c!=null&&canvas.SelectedKeys.Contains(c.Key))
            {canvas.SelectedKeys.Remove(c.Key);c=project.Controls.FirstOrDefault(item=>canvas.SelectedKeys.Contains(item.Key));}
            if(c!=null)canvas.SelectedKeys.Add(c.Key);
            selected=c;canvas.Selected=c;unlock.Checked=preserveUnlock;canvas.UnlockBindings=preserveUnlock;
            fields.Clear();originals.Clear();touchedFields.Clear();mixedFields.Clear();properties.SuspendLayout();foreach(Control old in properties.Controls.Cast<Control>().ToArray())old.Dispose();properties.Controls.Clear();
            UpdateSelectionTools();
            if(c==null){selectionName.Text="No controls";selectionInfo.Text="This file contains no supported controls.";properties.ResumeLayout();canvas.Invalidate();return;}
            selectionName.Text=(canvas.SelectedKeys.Count>1?canvas.SelectedKeys.Count+" items · ":"")+DisplayName(c);selectionInfo.Text=c.Get("type")+"  ·  ID "+c.Get("id","—")+" (protected)\n"+c.Source.RelativePath+"\n"+(canvas.SelectedKeys.Count>1?"Changes apply to the entire selection":LayoutTools.IsContainer(c)?"Group: select contents through the Element menu":c.AutomaticLayout?"Position controlled by group layout":c.IncludeCondition.Length>0?"Conditional include":"XML source mapped");
            tips.SetToolTip(selectionInfo,c.Source.Path+"\n"+c.IncludeCondition);
            for(int i=0;i<editable.Length;i++)
            {
                string key=editable[i];var collectionProperty=CollectionProperties.Find(key);
                if(collectionProperty!=null&&SelectedControls().Any(item=>!collectionProperty.AppliesTo(item)))continue;
                var row=new Panel{Width=251,Height=72,Margin=new Padding(0,0,0,3)};
                var label=new Label{Text=labels[i],ForeColor=muted,AutoSize=true,Location=new Point(0,0)};row.Controls.Add(label);
                bool mixed=SelectedControls().Any(item=>item.Get(key)!=c.Get(key));
                if(mixed)mixedFields.Add(key);
                var box=new TextBox{Text=mixed?"":c.Get(key),Width=247,Location=new Point(0,24),BackColor=Color.FromArgb(20,26,35),ForeColor=ink,BorderStyle=BorderStyle.FixedSingle};row.Controls.Add(box);
                box.TextChanged+=(sender,args)=>{touchedFields.Add(key);UpdateResetTools();if(project!=null&&!loading){UpdateProjectCaption();export.Enabled=project.Dirty||PendingProperties();}};
                AddPropertyResetButton(row,key);
                if(new[]{"textcolor","colordiffuse","font","align","textalign","textvalign","valign","texture","textureFocus","textureNoFocus"}.Contains(key))
                {
                    box.Width=217;var picker=new Button{Text="…",Width=26,Height=25,Location=new Point(221,23),FlatStyle=FlatStyle.Flat,BackColor=panel,ForeColor=ink};picker.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);row.Controls.Add(picker);
                    string propertyKey=key;TextBox target=box;
                    picker.Click+=(sender,args)=>
                    {
                        if(propertyKey.StartsWith("texture",StringComparison.Ordinal))
                        {
                            Attempt(()=>{using(var browser=new TextureBrowser(project,target.Text))if(browser.ShowDialog(this)==DialogResult.OK)target.Text=browser.SelectedPath;});
                        }
                        else if(propertyKey=="textcolor"||propertyKey=="colordiffuse")
                        {
                            using(var dialog=new ColorDialog{FullOpen=true})
                            {
                                string raw=target.Text.Trim().TrimStart('#');uint rgb;
                                if(UInt32.TryParse(raw,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb))dialog.Color=Color.FromArgb((int)(rgb&0xffffff)|unchecked((int)0xff000000));
                                if(dialog.ShowDialog(this)==DialogResult.OK){string alpha=raw.Length==8?raw.Substring(0,2):"ff";target.Text=alpha+dialog.Color.R.ToString("x2")+dialog.Color.G.ToString("x2")+dialog.Color.B.ToString("x2");}
                            }
                        }
                        else
                        {
                            var menu=StudioMenus.For(this,"property-picker");var options=propertyKey=="font"?project.FontNames.ToArray():propertyKey=="valign"||propertyKey=="textvalign"?new[]{"top","middle","bottom"}:new[]{"left","center","right"};
                            foreach(var option in options){string optionLabel=option;menu.Items.Add(optionLabel,null,(s2,e2)=>target.Text=optionLabel);}
                            if(menu.Items.Count==0)menu.Items.Add("No fonts.xml loaded").Enabled=false;
                            menu.Show(picker,new Point(0,picker.Height));
                        }
                    };
                    tips.SetToolTip(picker,key.StartsWith("texture",StringComparison.Ordinal)?"Select an image from the skin Media directory with a preview.":key=="textcolor"?"Choose a color; existing transparency is preserved.":key=="font"?"Select a font from fonts.xml.":"Select alignment.");
                }
                PropertyValue value;string origin=c.Properties.TryGetValue(key,out value)?(value.Local?"Local":value.Origin):"Not set · preview uses a fallback value";
                if(mixed)origin="Mixed values · set for all";
                var source=new Label{Text=origin,ForeColor=value!=null&&value.Local?Color.FromArgb(110,176,205):Color.FromArgb(124,141,160),AutoEllipsis=true,Width=247,Height=17,Location=new Point(0,50),Font=new Font("Segoe UI",8)};row.Controls.Add(source);
                fields[key]=box;originals[key]=box.Text;properties.Controls.Add(row);
                tips.SetToolTip(box,key+"\n"+origin+(key=="visible"?"\nAn empty condition adds no restriction; parent and include conditions still apply. Unsupported runtime expressions remain unresolved in the preview and are preserved in XML.":"")+(c.IsBound(key)?"\n#Bindings are replaced only with explicit approval.":""));
            }
            properties.ResumeLayout();UpdateProtectionTools();int id;if(SkinProject.Focusable(c)&&Int32.TryParse(c.Get("id"),out id)&&id>0)state.SetFocus(id);
            UpdatePreviewTree();SyncElementTreeSelection();
            canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();
        }
        void Apply()
        {
            if(selected==null)return;var values=new Dictionary<string,string>();var targets=SelectedControls();
            foreach(var key in editable)
            {
                if(!fields.ContainsKey(key))continue;
                string text=fields[key].Text;if(text==originals[key]&&!(mixedFields.Contains(key)&&touchedFields.Contains(key)))continue;
                if(targets.Any(c=>c.IsBound(key))&&!unlock.Checked)throw new InvalidOperationException("„"+labels[Array.IndexOf(editable,key)]+"” contains a binding in the selection. Explicitly enable “Replace bindings locally”.");
                if(new[]{"posX","posY","width","height","textXOff","textYOff"}.Contains(key))
                {int n;if(!Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out n)||(key=="width"||key=="height")&&n<=0)throw new InvalidOperationException("For "+key+" enter a valid integer; sizes must be positive.");}
                if((key=="align"||key=="textalign")&&!new[]{"left","right","center"}.Contains(text))throw new InvalidOperationException("Alignment: left, right or center.");
                if((key=="valign"||key=="textvalign")&&!new[]{"top","middle","bottom"}.Contains(text))throw new InvalidOperationException("Vertical alignment: top, middle or bottom.");
                var collectionProperty=CollectionProperties.Find(key);if(collectionProperty!=null)collectionProperty.Validate(text);
                values[key]=text;
            }
            if(values.Count==0)return;string identity=selected.Key;project.EditMany(targets.Select(c=>new ControlEdit(c,values)),"Change selection properties");RefreshProject(identity);
        }
        void Export()
        {
            RequireAppliedProperties();
            if(project==null||!project.Dirty){ShowNotice("No changes yet.");return;}
            using(var d=new SaveFileDialog{Filter="ZIP containing modified skin files|*.zip",FileName="MP1-Skin-Changes.zip",Title="Export changes"})
                if(d.ShowDialog()==DialogResult.OK){project.Export(d.FileName);exportedSignature=ProjectFile.XmlSignature(project);UpdateProjectCaption();ShowNotice("Exported: "+d.FileName+". Also save an editor project to continue editing later.");}
        }
        void UpdateDiagnostics()
        {
            if(project==null)return;
            var lines=new List<string>{"Static GDI+ preview: font families and 576-line scaling from fonts.xml; unavailable families are substituted. Animations and masks remain unverified."};
            if(project.Controls.Any(CollectionSettings.IsCollection))lines.Add("Collection: "+CollectionSettings.EffectiveMode(project)+", "+project.Editor.Collection.Items.Count+" sample items. Coverflow is a static approximation without a 3D engine.");
            int unknown=project.Controls.Count(c=>state.Visible(c)==null),hidden=project.Controls.Count(c=>state.Visible(c)==false);
            lines.Add("Conditions: "+hidden+" hidden, "+unknown+" unresolved. Unknown conditions remain unchanged in XML.");
            lines.Add("Sample data: "+project.Editor.Preview.Values.Count+" custom sample values; "+state.Plugins.Count+" plugin states active. Adjust through Sample Data. Images come from Media; missing images appear only with Show unverified content.");
            if(selected!=null&&!String.IsNullOrEmpty(selected.Get("visible")))lines.Add("Selection · visible: "+selected.Get("visible"));
            if(project.Editor.Language.Active)lines.Add("Language preview: "+(project.Editor.Language.Primary==null?"no primary language":project.Editor.Language.Primary.Name)+(project.Editor.Language.Fallback==null?"":" · Fallback: "+project.Editor.Language.Fallback.Name)+" · Original IDs are preserved.");
            lines.AddRange(project.Warnings.Distinct());
            if(canvas.Missing.Length>0)lines.Add("Missing images: "+String.Join(", ",canvas.Missing.Take(6))+(canvas.Missing.Length>6?" …":""));
            diagnostics.Text=String.Join(Environment.NewLine,lines);
        }
        void ShowNotice(string text){status.Text=text;}
        void Attempt(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"MP1 Skin Studio",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            if(keyData==(Keys.Control|Keys.Z)&&!(ActiveControl is TextBox)){undo.PerformClick();return true;}
            if(keyData==(Keys.Control|Keys.Y)&&!(ActiveControl is TextBox)){redo.PerformClick();return true;}
            if(keyData==(Keys.Control|Keys.S)){Attempt(()=>SaveEditorProject(false));return true;}
            if(keyData==(Keys.Control|Keys.Shift|Keys.S)){Attempt(()=>SaveEditorProject(true));return true;}
            if(keyData==(Keys.Control|Keys.O)){Attempt(OpenEditorProject);return true;}
            if(keyData==(Keys.Control|Keys.E)){export.PerformClick();return true;}
            if(keyData==(Keys.Control|Keys.F)){elementSearch.Focus();elementSearch.SelectAll();return true;}
            if(keyData==Keys.F2&&(canvas.Focused||(tree.Focused&&tree.SelectedNode!=null&&tree.SelectedNode.Tag is SkinControl))&&SelectedControls().Length==1){Attempt(()=>EditElementText(selected));return true;}
            if(keyData==Keys.F8){Attempt(ShowAudit);return true;}
            if(keyData==Keys.Delete&&(canvas.Focused||(tree.Focused&&tree.SelectedNode!=null&&tree.SelectedNode.Tag is SkinControl))){Attempt(DeleteSelection);return true;}
            return base.ProcessCmdKey(ref msg,keyData);
        }
        public void RenderTo(string path)
        {
            Show();Application.DoEvents();
            using(var bitmap=new Bitmap(Width,Height)) {DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,ImageFormat.Png);}
            Hide();
        }
        public void RunUiSmoke(string directory)
        {
            // A hidden process startup can consume the first native ShowWindow call.
            Directory.CreateDirectory(directory);Show();Hide();Show();Activate();Application.DoEvents();
            var results=new List<string>();
            Action<bool,string> verify=(ok,name)=>{results.Add((ok?"PASS  ":"FAIL  ")+name);File.WriteAllLines(Path.Combine(directory,"ui-test-progress.txt"),results,new System.Text.UTF8Encoding(false));if(!ok)throw new InvalidOperationException("UI test: "+name);};
            verify(page.SelectedItem.ToString()=="demo.xml"&&theme.SelectedIndex==0,"Demo and theme are selected");
            var film=project.Controls.First(c=>c.Get("description")=="Movies");SelectControl(film);
            fields["posX"].Text="220";Apply();
            verify(selected.Get("posX")=="220"&&selected.Source.RelativePath=="demo.menu.xml","Applying properties edits the associated include file");
            undo.PerformClick();verify(selected.Get("posX")=="140"&&!project.Dirty,"Undo restores the original");
            redo.PerformClick();verify(selected.Get("posX")=="220"&&project.Dirty,"Redo restores the change");undo.PerformClick();
            var time=project.Controls.First(c=>c.Get("description")=="Time");SelectControl(time);fields["label"].Text="New clock";
            bool blocked=false;try{Apply();}catch(InvalidOperationException){blocked=true;}
            verify(blocked&&!project.Dirty,"Data bindings are protected against accidental replacement");
            unlock.Checked=true;Apply();verify(selected.Get("label")=="New clock","Approved data bindings can be replaced locally");undo.PerformClick();
            film=project.Controls.First(c=>c.Get("description")=="Movies");SelectControl(film);canvas.Update();canvas.ExerciseDrag(film,60,40,false);
            verify(selected.Get("posX")=="200"&&selected.Get("posY")=="620","Canvas mouse events move controls with grid snapping");undo.PerformClick();
            film=project.Controls.First(c=>c.Get("description")=="Movies");SelectControl(film);canvas.Update();canvas.ExerciseDrag(film,50,20,true);
            verify(selected.Get("width")=="520"&&selected.Get("height")=="150","Canvas resize handles change width and height");undo.PerformClick();
            page.SelectedIndex=page.FindStringExact("common.time.xml");verify(project.Page=="common.time.xml"&&project.Controls.Count>0,"Page selection loads another XML file");
            page.SelectedIndex=page.FindStringExact("demo.xml");verify(project.Page=="demo.xml"&&!project.Dirty,"Returning to the demo preserves original files");
            RunDesignerUiSmoke(verify);
            RunProjectUiSmoke(directory,verify);
            RunTextureUiSmoke(directory,verify);
            RunViewportUiSmoke(directory,verify);
            RunAuditUiSmoke(verify);
            RunPropertyResetUiSmoke(verify);
            RunPreviewSettingsUiSmoke(directory,verify);
            RunMenuUiSmoke(verify);
            RunStructuralUiSmoke(verify);
            RunSearchUiSmoke(verify);
            RunGroupUiSmoke(verify);
            RunGroupCreationUiSmoke(verify);
            RunGroupRemovalUiSmoke(verify);
            RunTextEditUiSmoke(verify);
            RunLanguageUiSmoke(directory,verify);
            RunStackUiSmoke(directory,verify);RunGridUiSmoke(directory,verify);
            RunCollectionUiSmoke(directory,verify);
            File.WriteAllLines(System.IO.Path.Combine(directory,"ui-test-results.txt"),results.Concat(new[]{"",results.Count+" passed, 0 failed."}),new System.Text.UTF8Encoding(false));
            Hide();
        }
    }
}
