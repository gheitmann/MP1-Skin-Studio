using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    [Serializable]public sealed class ElementDrag
    {public string Kind;public string ProjectRoot;}
    public sealed partial class SkinCanvas
    {
        public event Func<IDataObject,PointF,bool> InsertDrop;
        public Func<IDataObject,bool> AcceptDrop;
        bool DropInside(Point client,out PointF logical)
        {UpdateTransform();logical=Logical(client);return Project!=null&&!NavigationMode&&!dragging&&!panning&&logical.X>=0&&logical.Y>=0&&logical.X<Project.Width&&logical.Y<Project.Height;}
        protected override void OnDragEnter(DragEventArgs e){base.OnDragEnter(e);UpdateDropEffect(e);}
        protected override void OnDragOver(DragEventArgs e){base.OnDragOver(e);UpdateDropEffect(e);}
        void UpdateDropEffect(DragEventArgs e)
        {PointF p;e.Effect=(e.AllowedEffect&DragDropEffects.Copy)!=0&&DropInside(PointToClient(new Point(e.X,e.Y)),out p)&&AcceptDrop!=null&&AcceptDrop(e.Data)?DragDropEffects.Copy:DragDropEffects.None;}
        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);PointF p;e.Effect=DragDropEffects.None;
            if((e.AllowedEffect&DragDropEffects.Copy)==0||!DropInside(PointToClient(new Point(e.X,e.Y)),out p)||AcceptDrop==null||!AcceptDrop(e.Data))return;
            if(InsertDrop!=null&&InsertDrop(e.Data,p))e.Effect=DragDropEffects.Copy;
        }
        internal DragDropEffects ExerciseDrop(IDataObject data,PointF logical)
        {UpdateTransform();var screen=PointToScreen(new Point((int)Math.Round(origin.X+logical.X*scale),(int)Math.Round(origin.Y+logical.Y*scale)));var args=new DragEventArgs(data,0,screen.X,screen.Y,DragDropEffects.Copy,DragDropEffects.None);OnDragDrop(args);return args.Effect;}
    }
    public sealed partial class StudioForm
    {
        Panel dropPalette;TextureGrid dropTextures;TextBox dropSearch;Label dropHint;bool dropAssetsStale=true;
        Panel BuildDropPalette()
        {
            dropPalette=new Panel{Dock=DockStyle.Left,Width=220,Visible=false,BackColor=panel,Padding=new Padding(8)};
            var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=76,WrapContents=true};
            foreach(string kind in new[]{"label","button","image"})
            {
                string template=kind;var button=ButtonFor(kind=="label"?"Text +":kind=="button"?"Button +":"Image +",()=>InsertDropTemplate(template));button.Width=83;Point start=Point.Empty;bool armed=false;
                button.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){start=e.Location;armed=true;}};
                button.MouseUp+=(s,e)=>armed=false;
                button.MouseMove+=(s,e)=>{if(!armed||e.Button!=MouseButtons.Left||Math.Abs(e.X-start.X)<SystemInformation.DragSize.Width/2&&Math.Abs(e.Y-start.Y)<SystemInformation.DragSize.Height/2)return;armed=false;if(project!=null&&!PendingProperties())button.DoDragDrop(new ElementDrag{Kind=template,ProjectRoot=project.Root},DragDropEffects.Copy);};
                top.Controls.Add(button);tips.SetToolTip(button,"Drag onto the preview to position a new element, or click to insert at the default position. Target: the current page XML.");
            }
            var close=ButtonFor("Close",()=>dropPalette.Visible=false);close.Width=65;top.Controls.Add(close);
            dropHint=new Label{Dock=DockStyle.Top,Height=48,ForeColor=muted,Text="Drag a tool or Media image onto the preview. Target: current page XML."};
            dropSearch=new TextBox{Dock=DockStyle.Top,BackColor=Color.FromArgb(20,26,35),ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Filter toolbox images"};dropSearch.TextChanged+=(s,e)=>{if(dropTextures!=null)dropTextures.Filter(dropSearch.Text);};
            dropTextures=new TextureGrid{Dock=DockStyle.Fill,DragImages=true};
            dropPalette.Controls.Add(new DarkScrollHost(dropTextures){Dock=DockStyle.Fill});dropPalette.Controls.Add(dropSearch);dropPalette.Controls.Add(dropHint);dropPalette.Controls.Add(top);
            canvas.AllowDrop=true;canvas.AcceptDrop=CanAcceptInsertDrop;canvas.InsertDrop+=(data,point)=>{try{InsertDropped(data,point);return true;}catch(Exception ex){ShowNotice("Drop not applied: "+ex.Message);return false;}};return dropPalette;
        }
        void ToggleDropPalette()
        {dropPalette.Visible=!dropPalette.Visible;if(dropPalette.Visible){LoadDropAssets();dropSearch.Focus();}}
        void RefreshDropAssets()
        {dropAssetsStale=true;if(dropTextures!=null){dropTextures.Assets.Clear();dropTextures.Filter("");}if(dropPalette!=null&&dropPalette.Visible)LoadDropAssets();}
        void LoadDropAssets()
        {if(project==null||!dropAssetsStale)return;dropTextures.Assets=TextureCatalog.List(project);dropTextures.Filter(dropSearch.Text);dropHint.Text="Drag a tool or image · Target: "+project.Page;dropAssetsStale=false;}
        bool CanAcceptInsertDrop(IDataObject data)
        {
            if(project==null||PendingProperties()||canvas.NavigationMode||data==null)return false;
            var tool=data.GetData(typeof(ElementDrag)) as ElementDrag;
            if(tool!=null)return String.Equals(tool.ProjectRoot,project.Root,StringComparison.OrdinalIgnoreCase)&&new[]{"label","button","image"}.Contains(tool.Kind);
            var files=data.GetData(DataFormats.FileDrop) as string[];string relative;
            return files!=null&&files.Length==1&&TryMediaDrop(files[0],out relative);
        }
        bool TryMediaDrop(string file,out string relative)
        {
            relative=null;try
            {
                if(!Path.IsPathRooted(file)||!new[]{".png",".jpg",".jpeg",".bmp",".gif"}.Contains(Path.GetExtension(file).ToLowerInvariant()))return false;
                string full=Path.GetFullPath(file);
                foreach(string root in new[]{project.Theme.Length>0?project.SafePath("Themes/"+project.Theme+"/Media"):null,project.SafePath("Media")}.Where(r=>r!=null))
                {
                    string prefix=root.TrimEnd('\\','/')+Path.DirectorySeparatorChar;if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))continue;
                    string path=full;while(path!=null&&path.Length>=root.Length){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)return false;path=Path.GetDirectoryName(path);}
                    string candidate=full.Substring(prefix.Length).Replace('\\','/');if(!String.Equals(project.Resolve("Media/"+candidate),full,StringComparison.OrdinalIgnoreCase))return false;relative=candidate;return true;
                }
            }
            catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}return false;
        }
        void InsertDropTemplate(string kind)
        {RequireAppliedProperties();InsertDropped(new DataObject(typeof(ElementDrag).FullName,new ElementDrag{Kind=kind,ProjectRoot=project.Root}),new PointF(Math.Min(300,project.Width-1),Math.Min(300,project.Height-1)));}
        void InsertDropped(IDataObject data,PointF point)
        {
            if(Single.IsNaN(point.X)||Single.IsNaN(point.Y)||Single.IsInfinity(point.X)||Single.IsInfinity(point.Y)||point.X<0||point.Y<0||project==null||point.X>=project.Width||point.Y>=project.Height)throw new InvalidOperationException("Drop inside the skin preview.");
            RequireAppliedProperties();if(!CanAcceptInsertDrop(data))throw new InvalidOperationException("Drag a tool or one image from this skin's active Media directory. External images must first be placed in Media.");
            var tool=data.GetData(typeof(ElementDrag)) as ElementDrag;string kind=tool==null?"image":tool.Kind,texture="";int width=kind=="image"?300:420,height=kind=="image"?300:kind=="button"?110:70;
            if(tool==null)
            {
                var files=(string[])data.GetData(DataFormats.FileDrop);if(!TryMediaDrop(files[0],out texture))throw new InvalidOperationException("The Media image is no longer available.");
                var asset=new TextureAsset{PhysicalPath=files[0]};string error;using(var thumbnail=TextureCatalog.Thumbnail(asset,32,32,out error)){if(thumbnail==null)throw new InvalidOperationException("Image cannot be previewed: "+error);width=asset.Width;height=asset.Height;}
            }
            int step=project.Editor.SnapEnabled?project.Editor.SnapSpacing:1;
            int x=Math.Max(0,Math.Min(project.Width-1,(int)Math.Round(point.X/step)*step)),y=Math.Max(0,Math.Min(project.Height-1,(int)Math.Round(point.Y/step)*step));
            string source=project.Relative(project.Resolve(project.Page));CreateDesignerElement(()=>project.InsertControlAt(source,kind,x,y,texture,width,height));
            ShowNotice("Inserted "+kind+" in "+source+". One Undo removes the complete insertion; Media files remain unchanged.");
        }
    }
}
