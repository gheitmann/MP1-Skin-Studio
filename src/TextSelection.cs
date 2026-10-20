using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MP1SkinStudio
{
    public sealed partial class SkinCanvas
    {
        Graphics selectionGraphics;
        internal static bool TextSelection(SkinControl control)
        {return control.Get("type")=="label"||control.Get("type")=="fadelabel";}
        RectangleF SelectionBounds(Graphics graphics,SkinControl control)
        {
            var bounds=EffectiveBounds(control);if(!TextSelection(control))return bounds;
            string text=ReadablePreview.Caption(State,control,Project.Editor.PreviewMode=="all"&&!IsolateSelection);
            using(var font=SurfaceFont(control,"font"))using(var format=new StringFormat(StringFormat.GenericTypographic))
            {
                format.FormatFlags=StringFormatFlags.NoWrap;format.Trimming=StringTrimming.EllipsisCharacter;
                var extent=graphics.MeasureString(String.IsNullOrEmpty(text)?" ":text,font,new SizeF(100000,10000),format);
                // Match the same alignment/measurement path as SurfaceText.
                var area=SurfaceGeometry.LabelArea(control,bounds,extent);
                if(String.IsNullOrWhiteSpace(text)||text.Length>8192)return area;
                // MeasureString includes line leading. Ink bounds avoid outlining
                // the next row when a skin places single-line labels close together.
                using(var path=new GraphicsPath())
                {
                    path.AddString(text,font.FontFamily,(int)font.Style,font.Size,new PointF(area.X,area.Y),format);
                    var ink=path.GetBounds();ink.Intersect(area);
                    if(ink.Width<=0||ink.Height<=0)return area;
                    ink.Inflate(1,1);return ink;
                }
            }
        }
        bool SelectionContains(SkinControl control,PointF point)
        {
            if(!TextSelection(control))return EffectiveBounds(control).Contains(point);
            if(selectionGraphics!=null)return SelectionBounds(selectionGraphics,control).Contains(point);
            using(var graphics=CreateGraphics())return SelectionBounds(graphics,control).Contains(point);
        }
    }
}
