using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class SkinFontSpec
    {
        public string Family;public float Height;public FontStyle Style;
        // GUIFontManager truncates the 576-line scale to an integer, then
        // GUIFont creates a System.Drawing.Font in points. Normalize to 96 DPI
        // before the canvas transform, rather than applying display DPI twice.
        public float Pixels(int skinHeight){return Math.Max(1,Math.Min(512,(float)Math.Floor(Height*Math.Max(1,skinHeight)/576f)*96f/72f));}
    }
    public sealed partial class SkinProject
    {
        public readonly Dictionary<string,SkinFontSpec> FontSpecs=new Dictionary<string,SkinFontSpec>(StringComparer.Ordinal);
        void LoadFontSpecs(SkinSource source)
        {
            foreach(var f in source.Root.Children.Where(n=>n.Name=="font"))
            {
                float h;string name=f.Value("name");if(String.IsNullOrEmpty(name)||!Single.TryParse(f.Value("height"),NumberStyles.Float,CultureInfo.InvariantCulture,out h)||h<=0||Single.IsNaN(h)||Single.IsInfinity(h))continue;
                FontSpecs[name]=new SkinFontSpec{Family=f.Value("filename")??"Segoe UI",Height=h,Style=(f.Value("bold")=="yes"?FontStyle.Bold:FontStyle.Regular)|(f.Value("italic")=="yes"?FontStyle.Italic:FontStyle.Regular)};
            }
            var aliases=source.Root.Children.Where(n=>n.Name=="alias").Where(n=>!String.IsNullOrEmpty(n.Value("name"))).GroupBy(n=>n.Value("name")).ToDictionary(g=>g.Key,g=>g.Last().Value("fontname")??"");
            foreach(string alias in aliases.Keys)
            {string name=alias;var seen=new HashSet<string>();while(aliases.ContainsKey(name)&&seen.Add(name))name=aliases[name];SkinFontSpec spec;if(!aliases.ContainsKey(name)&&FontSpecs.TryGetValue(name,out spec)){FontSpecs[alias]=spec;FontSizes[alias]=spec.Height;if(!FontNames.Contains(alias))FontNames.Add(alias);}}
        }
    }
    public static class SurfaceGeometry
    {
        public static RectangleF LabelArea(SkinControl control,RectangleF bounds,SizeF text)
        {
            float width=control.Number("width",0),height=control.Number("height",0);
            if(control.AutomaticLayout){width=bounds.Width;height=bounds.Height;}
            string align=control.Resolved("align","left").ToLowerInvariant(),valign=control.Resolved("valign","top").ToLowerInvariant();
            float x=bounds.X,y=bounds.Y;
            // Designer bounds already extend left of the XML anchor for these alignments.
            if(align=="right")x+=bounds.Width;else if(align=="center")x+=bounds.Width/2;
            if(align=="center"){x+=width>0?Math.Max(0,(width-text.Width)/2):-text.Width/2;y+=(height-text.Height)/2;}
            else{if(align=="right")x-=text.Width;if(valign=="middle"||valign=="center")y+=(float)Math.Ceiling((height-text.Height)/2);else if(valign=="bottom")y+=height-text.Height;}
            return new RectangleF(x,y,width>0?Math.Min(width,text.Width+1):text.Width+1,text.Height+1);
        }
        public static RectangleF TextArea(SkinControl control,RectangleF bounds)
        {
            bool button=control.Get("type").Contains("button");
            float x=control.Number("textXOff",0),y=control.Number("textYOff",0);
            if(button){bounds.X+=x;bounds.Width=Math.Max(0,bounds.Width-2*x);bounds.Y+=y;bounds.Height=Math.Max(0,bounds.Height-y);}
            return bounds;
        }
        public static RectangleF Fit(RectangleF target,Size source,bool aspect)
        {if(!aspect||source.Width<1||source.Height<1)return target;float ratio=Math.Min(target.Width/source.Width,target.Height/source.Height);return new RectangleF(target.X+(target.Width-source.Width*ratio)/2,target.Y+(target.Height-source.Height*ratio)/2,source.Width*ratio,source.Height*ratio);}
    }
    public sealed partial class SkinCanvas
    {
        sealed class PendingSurfaceText
        {
            public Region Clip;
            public Action<Graphics> Paint;
        }
        List<PendingSurfaceText> pendingSurfaceText;
        void FlushSurfaceText(Graphics g)
        {
            var pending=pendingSurfaceText;pendingSurfaceText=null;
            foreach(var item in pending)
            {
                var saved=g.Save();
                try{g.SetClip(item.Clip,System.Drawing.Drawing2D.CombineMode.Replace);item.Paint(g);}
                finally{g.Restore(saved);}
            }
        }
        readonly Dictionary<string,FontFamily> fontFamilies=new Dictionary<string,FontFamily>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> fallbackFonts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string[] FallbackFonts {get{return fallbackFonts.OrderBy(s=>s).ToArray();}}
        void ResetFonts(){foreach(var family in fontFamilies.Values)family.Dispose();fontFamilies.Clear();fallbackFonts.Clear();}
        Font SurfaceFont(SkinControl c,string property)
        {
            string name=c.Resolved(property,c.Resolved("font","TitanLight16"));SkinFontSpec spec;
            if(!Project.FontSpecs.TryGetValue(name,out spec)){float size;spec=new SkinFontSpec{Family="Segoe UI",Height=Project.FontSizes.TryGetValue(name,out size)?size:18};fallbackFonts.Add(name);}
            FontFamily family;if(!fontFamilies.TryGetValue(spec.Family,out family))
            {try{family=new FontFamily(spec.Family);}catch{family=new FontFamily("Segoe UI");fallbackFonts.Add(spec.Family);}fontFamilies[spec.Family]=family;}
            FontStyle style=spec.Style;if(!family.IsStyleAvailable(style))style=FontStyle.Regular;
            return new Font(family,spec.Pixels(Project.Height),style,GraphicsUnit.Pixel);
        }
        void SurfaceText(Graphics g,SkinControl c,string text,RectangleF rect,bool focus,string fontProperty="font",string colorProperty="textcolor",string alignProperty="align",bool multiline=false)
        {
            if(String.IsNullOrEmpty(text)||rect.Width<1||rect.Height<1)return;
            if(pendingSurfaceText!=null)
            {
                // MP1 presents queued font textures after control textures.
                // Retain each collection/control clip when deferring its text.
                pendingSurfaceText.Add(new PendingSurfaceText{Clip=g.Clip,Paint=target=>SurfaceText(target,c,text,rect,focus,fontProperty,colorProperty,alignProperty,multiline)});
                return;
            }
            bool button=c.Get("type").Contains("button");
            string align=c.Resolved(button?"textalign":alignProperty,"left").ToLowerInvariant();string valign=c.Resolved(button?"textvalign":"valign","top").ToLowerInvariant();
            if(c.Get("type")=="checkmark")valign="top";
            Color color=ParseColor(c.Resolved(colorProperty,colorProperty=="disabledcolor"?"ff606060":"ffffffff"),Color.White);
            if(focus)color=ParseColor(c.Resolved(button?"textcolor":"selectedColor",c.Resolved("textcolorfocus",c.Resolved(colorProperty,"ffffffff"))),color);
            else if(button)color=ParseColor(c.Resolved("textcolorNoFocus",c.Resolved(colorProperty,"ffffffff")),color);
            using(var font=SurfaceFont(c,fontProperty))using(var format=new StringFormat(StringFormat.GenericTypographic))
            {
                format.Alignment=align=="right"?StringAlignment.Far:align=="center"?StringAlignment.Center:StringAlignment.Near;
                format.LineAlignment=valign=="bottom"?StringAlignment.Far:valign=="middle"||valign=="center"?StringAlignment.Center:StringAlignment.Near;
                format.Trimming=multiline?StringTrimming.EllipsisWord:StringTrimming.EllipsisCharacter;if(!multiline)format.FormatFlags=StringFormatFlags.NoWrap;
                if((c.Get("type")=="label"||c.Get("type")=="fadelabel")&&alignProperty=="align"&&!multiline)
                {
                    var extent=g.MeasureString(text,font,new SizeF(100000,10000),format);
                    rect=SurfaceGeometry.LabelArea(c,rect,extent);format.Alignment=StringAlignment.Near;format.LineAlignment=StringAlignment.Near;
                }
                var saved=g.Save();try{g.SetClip(rect,System.Drawing.Drawing2D.CombineMode.Intersect);
                    float distance=c.Number("shadowDistance",0);if(distance>0&&distance<500){float angle=c.Number("shadowAngle",0)*(float)Math.PI/180;var shadow=rect;shadow.Offset((float)Math.Cos(angle)*distance,(float)Math.Sin(angle)*distance);using(var brush=new SolidBrush(ParseColor(c.Resolved("shadowColor","ff000000"),Color.Black)))g.DrawString(text,font,brush,shadow,format);}
                    using(var brush=new SolidBrush(color))g.DrawString(text,font,brush,rect,format);
                }finally{g.Restore(saved);}
            }
        }
        void SurfaceImage(Graphics g,SkinControl c,Bitmap image,RectangleF rect)
        {
            var plan=ImagePresentation.Plan(c,rect,image.Size);if(!plan.Valid)return;var target=plan.Target;
            Color tint=ParseColor(State.Text(c,c.Resolved("colordiffuse","ffffffff")),Color.White);
            using(var attributes=new ImageAttributes())
            {var matrix=new ColorMatrix();matrix.Matrix00=tint.R/255f;matrix.Matrix11=tint.G/255f;matrix.Matrix22=tint.B/255f;matrix.Matrix33=tint.A/255f;attributes.SetColorMatrix(matrix);g.DrawImage(image,new[]{new PointF(target.Left,target.Top),new PointF(target.Right,target.Top),new PointF(target.Left,target.Bottom)},plan.Source,GraphicsUnit.Pixel,attributes);}
        }
    }
}
