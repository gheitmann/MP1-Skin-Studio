using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinCanvas
    {
        void DrawCollection(Graphics g,SkinControl c,RectangleF rect)
        {
            if(c.Get("type")=="facadeview"&&CollectionSettings.Template(Project,c,Project.Editor.Collection.Mode)!=null)return;
            var settings=Project.Editor.Collection;var saved=g.Save();g.SetClip(rect,CombineMode.Intersect);
            try
            {
                var tiles=CollectionGeometry.Tiles(c,rect,settings);string kind=CollectionSettings.Kind(c,settings.Mode);
                foreach(var tile in tiles.OrderBy(t=>t.Selected?1:0))
                {
                    var item=settings.Items[tile.Index];bool selected=FocusPreview&&tile.Selected;
                    string frame=kind=="list"||kind=="album"||kind=="playlist"?c.Get(selected?"textureFocus":"textureNoFocus"):c.Get(selected?"imageFolderFocus":"imageFolder");
                    var background=Texture(c,frame);if(background!=null)SurfaceImage(g,c,background,tile.Bounds);
                    else if(frame.Trim()!="-")using(var brush=new SolidBrush(selected?Color.FromArgb(205,24,112,172):Color.FromArgb(95,26,47,68)))g.FillRectangle(brush,tile.Bounds);
                    if(tile.Image.Width>2&&tile.Image.Height>2)
                    {
                        Bitmap image=null;
                        try{if(item.Image.Length>0&&File.Exists(Project.Resolve("Media/"+item.Image)))image=Texture(c,item.Image);}catch{}
                        var target=tile.Image;
                        if(image!=null){target=SurfaceGeometry.Fit(target,image.Size,true);SurfaceImage(g,c,image,target);}
                        else{using(var brush=new SolidBrush(Color.FromArgb(255,44+(tile.Index*27)%70,74+(tile.Index*19)%80,105+(tile.Index*31)%90)))g.FillRectangle(brush,target);if(target.Width>45&&target.Height>45)DrawText(g,(tile.Index+1).ToString(),target,Math.Min(80,target.Height*.3f),Color.FromArgb(150,255,255,255),"center");}
                        if(selected)using(var pen=new Pen(Color.FromArgb(100,205,255),3/scale))g.DrawRectangle(pen,tile.Image.X,tile.Image.Y,tile.Image.Width,tile.Image.Height);
                    }
                    SurfaceText(g,c,item.Title,tile.Title,selected,"font","textcolor","textalign");
                    if(tile.Detail.Width>1)SurfaceText(g,c,item.Detail,tile.Detail,selected,"font2","textcolor2","textalign2");
                }
                if(settings.Items.Count==0)DrawText(g,"Empty collection",rect,24,Color.FromArgb(130,160,185),"center");
                bool? visible=State.Visible(c);if(visible!=true)using(var pen=new Pen(Color.FromArgb(190,232,174,78),1/scale)){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);}
            }finally{g.Restore(saved);}
        }
    }
}
