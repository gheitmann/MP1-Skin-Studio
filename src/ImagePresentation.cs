using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class ImageSurfacePlan
    {public RectangleF Target,Source;public bool Valid,Supported=true;}
    public static class ImagePresentation
    {
        public static readonly string[] BooleanFields={"keepaspectratio","zoom","zoomfromtop"};
        static string Resolve(SkinControl control,string raw)
        {
            string value=raw??"",next;var seen=new HashSet<string>();
            while(control.Defines!=null&&control.Defines.TryGetValue(value,out next)&&seen.Add(value)){if(seen.Count>32)return "#unverified";value=next;}
            return value.Trim().ToLowerInvariant();
        }
        static bool Flag(string raw,out bool value)
        {value=raw=="yes"||raw=="true";return value||raw==""||raw=="no"||raw=="false";}
        static bool Settings(SkinControl control,out bool aspect,out bool zoom,out bool top,out string align,out string valign)
        {
            align="left";valign="top";bool known=Flag(Resolve(control,control.Get("keepaspectratio")),out aspect);known=Flag(Resolve(control,control.Get("zoom")),out zoom)&&known;known=Flag(Resolve(control,control.Get("zoomfromtop")),out top)&&known;
            foreach(var term in control.ImageAlignmentRules)
            {
                string value=Resolve(control,term.Value);
                if(term.Key=="align"){if(new[]{"left","center","right"}.Contains(value))align=value;else known=false;}
                else if(term.Key=="valign"){if(new[]{"top","middle","bottom"}.Contains(value))valign=value;else known=false;}
                else{bool centered;if(!Flag(value,out centered)){known=false;continue;}if(centered){align="center";valign="middle";}else if(align=="center"&&valign=="middle"){align="left";valign="top";}}
            }
            return known;
        }
        public static bool Supported(SkinControl control)
        {bool aspect,zoom,top;string align,valign;return Settings(control,out aspect,out zoom,out top,out align,out valign);}
        public static ImageSurfacePlan Plan(SkinControl control,RectangleF bounds,Size source)
        {
            var plan=new ImageSurfacePlan{Target=bounds,Source=new RectangleF(0,0,source.Width,source.Height)};
            if(!CollectionGeometry.ValidBounds(bounds)||source.Width<1||source.Height<1)return plan;
            plan.Valid=true;
            if(control.Get("type")!="image")
            {plan.Target=SurfaceGeometry.Fit(bounds,source,new[]{"yes","true"}.Contains(control.Resolved("keepaspectratio").Trim().ToLowerInvariant()));return plan;}
            bool aspect,zoom,top;string align,valign;plan.Supported=Settings(control,out aspect,out zoom,out top,out align,out valign);
            if(!plan.Supported){plan.Target=SurfaceGeometry.Fit(bounds,source,aspect);return plan;}
            if(!aspect)return plan;
            if(zoom||top)
            {
                int width=source.Width,height=source.Height;float ratio=bounds.Width/bounds.Height;
                if(source.Width/bounds.Width<source.Height/bounds.Height)height=(int)(width/ratio);else width=(int)(height*ratio);
                if(width<1||height<1){plan.Valid=false;return plan;}
                width=Math.Min(width,source.Width);height=Math.Min(height,source.Height);
                plan.Source=new RectangleF((source.Width-width)/2,top?0:(source.Height-height)/2,width,height);return plan;
            }
            float scale=Math.Min(bounds.Width/source.Width,bounds.Height/source.Height),w=source.Width*scale,h=source.Height*scale;
            float x=bounds.X+(align=="right"?bounds.Width-w:align=="center"?(bounds.Width-w)/2:0),y=bounds.Y+(valign=="bottom"?bounds.Height-h:valign=="middle"?(bounds.Height-h)/2:0);
            plan.Target=new RectangleF(x,y,w,h);return plan;
        }
    }
}
