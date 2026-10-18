using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    // Derived rectangles belong to an occurrence, never to its shared source key.
    // No XML/property is changed by measurement, visibility changes or arrangement.
    public sealed class StackPreview
    {
        readonly Dictionary<SkinControl,RectangleF> rectangles=new Dictionary<SkinControl,RectangleF>();
        readonly Dictionary<SkinControl,SkinControl[]> children=new Dictionary<SkinControl,SkinControl[]>();
        readonly PreviewState state;
        public readonly List<string> Warnings=new List<string>();
        public StackPreview(SkinProject project,PreviewState preview)
        {
            state=preview;
            foreach(var c in project.Controls)rectangles[c]=Raw(c);
            foreach(var g in project.Controls.Where(c=>c.RuntimeParent!=null).GroupBy(c=>c.RuntimeParent))children[g.Key]=g.ToArray();
            foreach(var c in project.Controls.Where(c=>c.RuntimeParent==null))Measure(c);
            foreach(var c in project.Controls.Where(c=>c.RuntimeParent==null))Arrange(c);
        }
        static RectangleF Raw(SkinControl c)
        {var r=LayoutTools.Bounds(c);r.X=c.Number("posX",0);if(LayoutTools.IsContainer(c)){r.Width=c.Number("width",0);r.Height=c.Number("height",0);}return r;}
        public RectangleF Bounds(SkinControl c)
        {
            RectangleF r;if(!rectangles.TryGetValue(c,out r))return LayoutTools.Bounds(c);
            if(c.Get("type")=="label"||c.Get("type")=="fadelabel")
            {if(c.Resolved("align")=="right")r.X-=r.Width;else if(c.Resolved("align")=="center")r.X-=r.Width/2;}
            return r;
        }
        public sealed class Rule {public int Gap;public bool Horizontal,Collapse;}
        Rule Parse(SkinControl c)
        {
            string text=c.Resolved("layout").Trim();if(text.Length==0)return null;
            Rule rule;if(TryRead(text,out rule))return rule;
            Warnings.Add(c.Name+": Layout is unsupported or has invalid parameters: "+text);return null;
        }
        public static bool TryRead(string text,out Rule rule)
        {
            rule=null;
            var m=Regex.Match(text,@"^StackLayout\s*(?:\(([^()]*)\))?$",RegexOptions.IgnoreCase);
            if(!m.Success)return false;
            string[] args=Regex.Split(m.Groups[1].Value.Trim(),@"[,\s]+").Where(x=>x.Length>0).ToArray();
            int gap=0,orientation=1,collapse=0;
            bool valid=args.Length<=3;
            if(args.Length>0)valid&=Int32.TryParse(args[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out gap);
            if(args.Length>1){string token=args[1].ToLowerInvariant();if(token=="horizontal")orientation=0;else if(token=="vertical")orientation=1;else valid&=Int32.TryParse(token,out orientation)&&(orientation==0||orientation==1);}
            if(args.Length>2){string token=args[2].ToLowerInvariant();if(token=="true")collapse=1;else if(token=="false")collapse=0;else valid&=Int32.TryParse(token,out collapse);}
            if(!valid)return false;
            rule=new Rule{Gap=Math.Max(0,gap),Horizontal=orientation==0,Collapse=collapse==1};return true;
        }
        static float[] Margin(SkinControl c)
        {
            var a=Regex.Split(c.Resolved("Margin").Trim(),@"[,\s]+").Where(s=>s.Length>0).ToArray();var n=new List<float>();
            foreach(string s in a){float f;if(!Single.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out f)||Single.IsInfinity(f)||Single.IsNaN(f))return new float[4];n.Add(f);}
            if(n.Count==1)return new[]{n[0],n[0],n[0],n[0]};if(n.Count==2)return new[]{n[0],n[1],n[0],n[1]};return n.Count==4?n.ToArray():new float[4];
        }
        bool Collapsed(SkinControl c){return c.Resolved("Visibility").Equals("Collapsed",StringComparison.OrdinalIgnoreCase);}
        readonly Dictionary<SkinControl,Rule> rules=new Dictionary<SkinControl,Rule>();
        void Measure(SkinControl c)
        {
            SkinControl[] list;if(!children.TryGetValue(c,out list))return;
            foreach(var child in list)Measure(child);
            var rule=Parse(c);if(rule==null)return;rules[c]=rule;
            float w=0,h=0;foreach(var child in list.Where(x=>!Collapsed(x)))
            {var r=rectangles[child];w=rule.Horizontal?w+r.Width+rule.Gap:Math.Max(w,r.Width);h=rule.Horizontal?Math.Max(h,r.Height):h+r.Height+rule.Gap;}
            var original=rectangles[c];string align=c.Resolved("align");
            if(rule.Horizontal&&original.Width>0){if(align=="right")w=Math.Max(w,original.Width);if(align=="center")w=Math.Max(w,(original.Width+w)/2);}
            var t=Margin(c);original.Width=w+t[0]+t[2];original.Height=h+t[1]+t[3];rectangles[c]=original;
        }
        void Arrange(SkinControl c)
        {
            SkinControl[] list;if(!children.TryGetValue(c,out list))return;
            Rule rule;if(rules.TryGetValue(c,out rule))
            {
                var g=rectangles[c];var t=Margin(c);float x=g.X+t[0],y=g.Y+t[1];
                if(rule.Horizontal&&(c.Resolved("align")=="right"||c.Resolved("align")=="center"))x=Math.Max(0,x+g.Width-list.Where(a=>!Collapsed(a)).Sum(a=>rectangles[a].Width+rule.Gap));
                foreach(var child in list)
                {
                    if(Collapsed(child))continue;var r=rectangles[child];float w=rule.Horizontal?r.Width:Math.Max(0,g.Width-t[0]-t[2]),h=rule.Horizontal?Math.Max(0,g.Height-t[1]-t[3]):r.Height;
                    r.X=x;r.Y=y;string ha=child.Resolved("HorizontalAlignment"),va=child.Resolved("VerticalAlignment");
                    if(ha=="Center")r.X+=(w-r.Width)/2;else if(ha=="Right")r.X+=w-r.Width;else if(ha=="Stretch")r.Width=w;
                    if(va=="Center")r.Y+=(h-r.Height)/2;else if(va=="Bottom")r.Y+=h-r.Height;else if(va=="Stretch")r.Height=h;
                    if((child.Get("type")=="label"||child.Get("type")=="fadelabel")&&child.Resolved("align")=="right")r.X+=r.Width;
                    rectangles[child]=r;
                    // Unknown visibility reserves a slot. Editor-only hiding never changes runtime layout.
                    bool hidden=rule.Collapse&&state!=null&&state.Visible(child)==false;
                    if(!hidden){if(rule.Horizontal)x+=w+rule.Gap;else y+=h+rule.Gap;}
                }
            }
            foreach(var child in list)Arrange(child);
        }
    }
}
