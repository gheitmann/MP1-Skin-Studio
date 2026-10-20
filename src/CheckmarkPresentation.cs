using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
namespace MP1SkinStudio
{
    public static class CheckmarkPresentation
    {
        public static string Key(string name)
        {return String.Equals(name,"markWidth",StringComparison.OrdinalIgnoreCase)?"markWidth":String.Equals(name,"markHeight",StringComparison.OrdinalIgnoreCase)?"markHeight":name;}
        public static readonly string[] Fields={"selected","markWidth","markHeight","textureCheckmark","textureCheckmarkNoFocus","disabledcolor"};
        public static readonly string[] Labels={"Selected expression · true / false","Checkmark width","Checkmark height","Selected checkmark image","Unselected checkmark image","Unfocused text color · AARRGGBB"};
        public static bool? Selected(SkinControl c,PreviewState state)
        {if(c.Get("selected").Length==0)return false;bool value;return Boolean.TryParse(state.Text(c,c.Get("selected")).Trim(),out value)?(bool?)value:null;}
        public static bool Layout(SkinControl c,out int width,out int height)
        {
            width=height=0;
            foreach(string field in new[]{"markWidth","markHeight","align"}){PropertyValue p;if(c.Properties.TryGetValue(field,out p)&&p.Expression.Length>0)return false;}
            string w=c.Resolved("markWidth","0"),h=c.Resolved("markHeight","0"),a=c.Resolved("align","right").Trim().ToLowerInvariant();
            return Int32.TryParse(w,NumberStyles.Integer,CultureInfo.InvariantCulture,out width)&&Int32.TryParse(h,NumberStyles.Integer,CultureInfo.InvariantCulture,out height)&&width>=0&&width<=8192&&height>=0&&height<=8192&&new[]{"left","right"}.Contains(a);
        }
        public static RectangleF MarkArea(SkinControl c,RectangleF bounds,float textWidth,int width,int height)
        {return new RectangleF(bounds.X+(c.Resolved("align","right").Trim().Equals("left",StringComparison.OrdinalIgnoreCase)?textWidth+5:0),bounds.Y,width,height);}
    }
    public sealed partial class SkinCanvas
    {
        void DrawCheckmark(Graphics g,SkinControl c,RectangleF bounds,bool focused)
        {
            int width,height;bool valid=CheckmarkPresentation.Layout(c,out width,out height);bool? selected=CheckmarkPresentation.Selected(c,State);
            bool allLayers=Project.Editor.PreviewMode=="all"&&!IsolateSelection;string label=ReadablePreview.Caption(State,c,allLayers);float textWidth=0;
            if(label.Length>8192)return;
            bool hasFont=c.Resolved("font").Length>0&&c.Resolved("font")!="-";
            if(hasFont)using(var font=SurfaceFont(c,"font"))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())textWidth=g.MeasureString(label,font,Int32.MaxValue,format).Width;
            var mark=CheckmarkPresentation.MarkArea(c,bounds,textWidth,Math.Max(0,width),Math.Max(0,height));
            if(valid&&selected.HasValue&&width>0&&height>0)
            {var image=Texture(c,selected.Value?c.Get("textureCheckmark"):c.Get("textureCheckmarkNoFocus"));if(image!=null)SurfaceImage(g,c,image,mark);}
            if(valid&&hasFont)
            {
                var text=bounds;if(!c.Resolved("align","right").Trim().Equals("left",StringComparison.OrdinalIgnoreCase)){text.X+=width+5;text.Width=Math.Max(0,text.Width-width-5);}
                SurfaceText(g,c,label,text,false,"font",focused?"textcolor":"disabledcolor","checkmarkTextAlignment",false);
            }
        }
        internal void CheckmarkForTest(Graphics g,SkinControl c,RectangleF bounds,bool focused){DrawCheckmark(g,c,bounds,focused);}
    }
}
