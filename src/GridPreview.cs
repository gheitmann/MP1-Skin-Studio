using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed partial class StackPreview
    {
        internal sealed class GridRule {public int Columns=1,Rows,XGap,YGap;public bool Vertical;}
        readonly Dictionary<SkinControl,GridRule> grids=new Dictionary<SkinControl,GridRule>();
        internal static bool TryReadGrid(string text,out GridRule rule)
        {
            rule=null;var m=Regex.Match(text,@"^GridLayout\s*(?:\(([^()]*)\))?$",RegexOptions.IgnoreCase);if(!m.Success)return false;
            var args=Regex.Split(m.Groups[1].Value.Trim(),@"[,\s]+").Where(x=>x.Length>0).ToArray();if(args.Length>5)return false;
            var g=new GridRule();
            if(args.Length>0&&!Int32.TryParse(args[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out g.Columns))return false;
            if(args.Length>1&&!Int32.TryParse(args[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out g.Rows))return false;
            if(g.Columns<0||g.Rows<0||(g.Columns==0&&g.Rows==0))return false;
            int xGap=0,yGap=0;
            if(args.Length>2&&!Int32.TryParse(args[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out xGap))return false;
            if(args.Length>3&&!Int32.TryParse(args[3],NumberStyles.Integer,CultureInfo.InvariantCulture,out yGap))return false;
            // GUIControlFactory selects the two-argument constructor for three tokens.
            if(args.Length>3){g.XGap=xGap;g.YGap=yGap;}
            if(args.Length>4){string a=args[4].ToLowerInvariant();if(a=="vertical"||a=="1")g.Vertical=true;else if(a!="horizontal"&&a!="0")return false;}
            g.XGap=Math.Max(0,g.XGap);g.YGap=Math.Max(0,g.YGap);rule=g;return true;
        }
        static void GridDimensions(GridRule g,int count,out int cols,out int rows)
        {
            // MP1 gives a positive row count precedence over the supplied columns.
            rows=g.Rows;cols=g.Columns;
            if(rows>0)cols=(int)(((long)count+rows-1)/rows);else rows=(int)(((long)count+cols-1)/cols);
        }
        void MeasureGrid(SkinControl c,SkinControl[] list,GridRule rule)
        {
            int cols,rows;GridDimensions(rule,list.Length,out cols,out rows);
            float w=0,h=0;foreach(var child in list.Where(x=>!Collapsed(x))){var r=rectangles[child];w=Math.Max(w,r.Width);h=Math.Max(h,r.Height);}
            var t=Margin(c);var bounds=rectangles[c];
            bounds.Width=Math.Max(0,w*cols+(float)rule.XGap*(cols-1)+t[0]+t[2]);
            bounds.Height=Math.Max(0,h*rows+(float)rule.YGap*(rows-1)+t[1]+t[3]);rectangles[c]=bounds;
        }
        void ArrangeGrid(SkinControl c,SkinControl[] list,GridRule rule)
        {
            int cols,rows;GridDimensions(rule,list.Length,out cols,out rows);if(cols==0||rows==0)return;
            var g=rectangles[c];var t=Margin(c);float w=(g.Width-t[0]-t[2]-(cols-1)*(float)rule.XGap)/cols,h=(g.Height-t[1]-t[3]-(rows-1)*(float)rule.YGap)/rows;
            // Iterate actual children, not configured empty cells; huge dimensions stay bounded.
            var collapsedInRow=new Dictionary<int,int>();
            for(int i=0;i<list.Length;i++)
            {
                var child=list[i];int row=rule.Vertical?i%rows:i/cols,col=rule.Vertical?i/rows:i%cols,skipped;
                collapsedInRow.TryGetValue(row,out skipped);
                // Original GridLayout skips the horizontal advance for collapsed children.
                if(Collapsed(child)){collapsedInRow[row]=skipped+1;continue;}
                var r=rectangles[child];r.X=g.X+t[0]+(col-skipped)*(w+rule.XGap);r.Y=g.Y+t[1]+row*(h+rule.YGap);
                string ha=child.Resolved("HorizontalAlignment"),va=child.Resolved("VerticalAlignment");
                if(ha=="Center")r.X+=(w-r.Width)/2;else if(ha=="Right")r.X+=w-r.Width;else if(ha=="Stretch")r.Width=w;
                if(va=="Center")r.Y+=(h-r.Height)/2;else if(va=="Bottom")r.Y+=h-r.Height;else if(va=="Stretch")r.Height=h;
                if((child.Get("type")=="label"||child.Get("type")=="fadelabel")&&child.Resolved("align")=="right")r.X+=r.Width;
                rectangles[child]=r;
            }
        }
    }
}
