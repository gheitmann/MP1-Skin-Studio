using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        // Native GUIControl.Navigate uses rounded top-left bearings and distances,
        // preserving document order and replacing only when BOTH scores improve.
        internal static double NavigationBearing(Point from,Point to)
        {
            double bearing=Math.Round(90-Math.Atan2((double)to.Y-from.Y,(double)to.X-from.X)/Math.PI*180+360)%360;
            bearing=bearing>180?((bearing+180)%360)-180:bearing<-180?((bearing-180)%360)+180:bearing;
            return bearing>=0?bearing-180:180-bearing;
        }
        internal static bool NavigationCone(double bearing,string direction)
        {
            return direction=="onleft"?bearing>=215&&bearing<=325:
                direction=="onright"?bearing>=-145&&bearing<=-35:
                direction=="onup"?bearing>=-45&&bearing<=45:bearing<=-135||bearing>=135;
        }
        static bool NativeNavigationExcluded(SkinControl c)
        {for(var p=c;p!=null;p=p.RuntimeParent)if(ControlId(p)==1)return true;return false;}
        static void NavigationGeometry(SkinControl c)
        {
            LayoutTools.RequireNumeric(c,"posX","posY","width","height");
            foreach(string key in new[]{"posX","posY","width","height"})
            {
                double number;string value=c.Resolved(key);
                if(value.Length==0&&(key=="posX"||key=="posY"))continue;
                if(!Double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number)||Double.IsNaN(number)||Double.IsInfinity(number)||Math.Abs(number)>1000000||((key=="width"||key=="height")&&number<=0))
                    throw new InvalidOperationException("Grid navigation requires known finite geometry: "+c.Name+" · "+key+".");
            }
        }
        static void NavigationLayoutProperties(SkinControl c)
        {
            string h=c.Resolved("HorizontalAlignment"),v=c.Resolved("VerticalAlignment");
            if(!new[]{"","Left","Center","Right","Stretch"}.Contains(h)||!new[]{"","Top","Center","Bottom","Stretch"}.Contains(v)||!new[]{"","Visible","Hidden","Collapsed"}.Contains(c.Resolved("Visibility")))
                throw new InvalidOperationException("Grid navigation requires known layout alignment.");
            string margin=c.Resolved("Margin").Trim();if(margin.Length==0)return;
            var parts=Regex.Split(margin,@"[,\s]+");
            if(!new[]{1,2,4}.Contains(parts.Length)||parts.Any(p=>{double n;return !Double.TryParse(p,NumberStyles.Float,CultureInfo.InvariantCulture,out n)||Double.IsNaN(n)||Double.IsInfinity(n)||Math.Abs(n)>1000000;}))
                throw new InvalidOperationException("Grid navigation requires a known finite margin.");
        }
        static Point NavigationPoint(RectangleF bounds)
        {
            if(Single.IsNaN(bounds.X)||Single.IsNaN(bounds.Y)||Single.IsInfinity(bounds.X)||Single.IsInfinity(bounds.Y)||Math.Abs(bounds.X)>1000000||Math.Abs(bounds.Y)>1000000)
                throw new InvalidOperationException("Grid navigation geometry exceeds the supported range.");
            return new Point((int)bounds.X,(int)bounds.Y);
        }
        static bool NavigationLayout(SkinControl group)
        {
            PropertyValue property;if(group.Properties.TryGetValue("layout",out property)&&property.Expression.Length>0)return false;
            StackPreview.Rule stack;StackPreview.GridRule grid;
            string layout=group.Resolved("layout").Trim();
            if(layout.Length==0&&group.Get("type")=="group")
            {
                // Plain groups retain absolute child coordinates. Do not infer
                // translation when an automatic ancestor repositions the wrapper.
                for(var ancestor=group.RuntimeParent;ancestor!=null;ancestor=ancestor.RuntimeParent)if(ancestor.Resolved("layout").Trim().Length>0)return false;
                return true;
            }
            return StackPreview.TryRead(layout,out stack)||StackPreview.TryReadGrid(layout,out grid);
        }
        bool GeometricNavigationSource(SkinControl source)
        {
            var parent=source.RuntimeParent;if(parent==null)return source.Get("type").Contains("button");
            StackPreview.GridRule grid;
            return parent.Resolved("layout").Trim().Length==0||parent.RuntimeParent!=null||StackPreview.TryReadGrid(parent.Resolved("layout").Trim(),out grid)||Controls.Any(c=>c.RuntimeParent==parent&&LayoutTools.IsContainer(c));
        }
        void NavigationGroup(SkinControl group,Dictionary<SkinControl,SkinControl[]> children,int depth)
        {
            if(depth>16||!NavigationLayout(group))throw new InvalidOperationException("Automatic navigation requires supported groups within 16 levels; plain groups inside automatic layouts remain unsupported.");
            LayoutTools.RequireNumeric(group,"posX","posY","width","height");
            foreach(string property in new[]{"posX","posY","width","height"})
            {
                string value=group.Resolved(property);double n;
                if(value.Length>0&&(!Double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out n)||Double.IsNaN(n)||Double.IsInfinity(n)||Math.Abs(n)>1000000))
                    throw new InvalidOperationException("Automatic layout navigation requires finite group geometry.");
            }
            NavigationLayoutProperties(group);
            if(!new[]{"","left","center","right"}.Contains(group.Resolved("align")))throw new InvalidOperationException("Automatic layout navigation requires known group alignment.");
            SkinControl[] list;if(!children.TryGetValue(group,out list))return;
            foreach(var child in list)
            {
                if(LayoutTools.IsContainer(child))NavigationGroup(child,children,depth+1);
                else if(group.Resolved("layout").Trim().Length>0||Focusable(child)){NavigationGeometry(child);NavigationLayoutProperties(child);}
            }
        }
        SkinControl GridNavigationTarget(PreviewState state,SkinControl source,string direction)
        {
            var parent=source.RuntimeParent;
            if((parent!=null&&!NavigationLayout(parent))||!source.Get("type").Contains("button")||NativeNavigationExcluded(source))
                throw new InvalidOperationException("Automatic navigation requires a static button in a supported hierarchy.");
            var logical=LogicalControls().ToArray();
            if(logical.Length>2048)throw new InvalidOperationException("This page exceeds the automatic grid navigation limit.");
            var candidates=new List<SkinControl>();var groups=new HashSet<SkinControl>();
            foreach(var c in logical.Where(Focusable))
            {
                if(NativeNavigationExcluded(c)||IsEditorHidden(c))continue;
                bool hiddenAncestor=false;
                for(var ancestor=c.RuntimeParent;ancestor!=null;ancestor=ancestor.RuntimeParent)if(new[]{"Hidden","Collapsed"}.Contains(ancestor.Resolved("Visibility")))hiddenAncestor=true;
                if(hiddenAncestor)continue;
                int before=state.FocusId;bool? visible;
                try{state.FocusId=ControlId(c)??before;visible=state.Visible(c);}finally{state.FocusId=before;}
                if(visible==false)continue;
                if(visible==null)throw new InvalidOperationException("Grid navigation encounters unverified visibility. Apply suitable sample data first.");
                int? id=ControlId(c);
                if(!id.HasValue||id<=0||logical.Count(x=>ControlId(x)==id)!=1||!c.Get("type").Contains("button"))
                    throw new InvalidOperationException("Grid navigation encounters an unsupported or ambiguous focusable control.");
                // Runtime enable/hidden-focus rules are not inferred from appearance.
                if(c.Get("enable").Length>0||c.Get("allowhiddenfocus").Length>0)
                    throw new InvalidOperationException("Grid navigation cannot verify runtime enable or hidden-focus rules.");
                NavigationGeometry(c);NavigationLayoutProperties(c);
                var group=c.RuntimeParent;
                if(group!=null)
                {
                    int depth=0;
                    for(var ancestor=group;ancestor!=null;ancestor=ancestor.RuntimeParent)
                    {
                        if(++depth>16||!NavigationLayout(ancestor))throw new InvalidOperationException("Automatic layout navigation requires supported stack/grid ancestors within 16 levels.");
                        group=ancestor;
                    }
                    groups.Add(group);
                }
                candidates.Add(c);
            }
            if(!candidates.Contains(source))throw new InvalidOperationException("The grid source is unavailable in this preview.");
            var children=Controls.Where(c=>c.RuntimeParent!=null).GroupBy(c=>c.RuntimeParent).ToDictionary(g=>g.Key,g=>g.ToArray());
            foreach(var group in groups)NavigationGroup(group,children,1);
            var layout=new StackPreview(this,state);if(layout.Warnings.Count>0)throw new InvalidOperationException("Grid navigation requires supported static layouts.");
            Point origin=NavigationPoint(layout.Bounds(source));double distanceMin=10000,bearingMin=10000;var target=source;
            foreach(var c in candidates)
            {
                if(c==source)continue;
                Point point=NavigationPoint(layout.Bounds(c));double bearing=NavigationBearing(origin,point);
                if(!NavigationCone(bearing,direction))continue;
                double dx=(double)point.X-origin.X,dy=(double)point.Y-origin.Y,distance=Math.Round(Math.Sqrt(dx*dx+dy*dy));
                if(distance<=distanceMin&&bearing<=bearingMin){target=c;distanceMin=distance;bearingMin=bearing;}
            }
            return target;
        }
    }
}
