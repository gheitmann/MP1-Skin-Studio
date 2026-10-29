using System;
using System.Drawing;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class SkinCanvas
    {
        bool panning;Point panStart;double panStartX,panStartY;
        public event Action ViewChanged;
        float FitScale{get{return Project==null?1:Math.Max(.02f,Math.Min((ClientSize.Width-54f)/Project.Width,(ClientSize.Height-70f)/Project.Height));}}
        void UpdateTransform()
        {
            if(Project==null)return;
            scale=FitScale*(float)Project.Editor.Zoom;
            origin=new PointF((ClientSize.Width-Project.Width*scale)/2+(float)Project.Editor.PanX*scale,(ClientSize.Height-Project.Height*scale)/2+(float)Project.Editor.PanY*scale);
        }
        void NotifyView(){Invalidate();if(ViewChanged!=null)ViewChanged();}
        public void FitView()
        {if(Project==null)return;Project.Editor.Zoom=1;Project.Editor.PanX=Project.Editor.PanY=0;UpdateTransform();NotifyView();}
        public void ZoomView(double factor,Point anchor)
        {
            if(Project==null||dragging||panning||Double.IsNaN(factor)||Double.IsInfinity(factor)||factor<=0)return;
            UpdateTransform();var before=Logical(anchor);Project.Editor.Zoom=Math.Max(.25,Math.Min(16,Project.Editor.Zoom*factor));UpdateTransform();var after=Logical(anchor);
            Project.Editor.PanX=Math.Max(-1000000,Math.Min(1000000,Project.Editor.PanX+after.X-before.X));Project.Editor.PanY=Math.Max(-1000000,Math.Min(1000000,Project.Editor.PanY+after.Y-before.Y));UpdateTransform();NotifyView();
        }
        public void NativeView(){ZoomView(1/(FitScale*Project.Editor.Zoom),new Point(Width/2,Height/2));}
        public void StepZoom(bool increase){ZoomView(increase?1.25:.8,new Point(Width/2,Height/2));}
        public string ZoomText{get{return Math.Round(FitScale*(Project==null?1:Project.Editor.Zoom)*100)+" %";}}
        protected override void OnMouseWheel(MouseEventArgs e)
        {base.OnMouseWheel(e);Focus();ZoomView(Math.Pow(1.25,e.Delta/120.0),e.Location);}
        bool ViewMouseDown(MouseEventArgs e)
        {
            if(Project==null||e.Button!=MouseButtons.Middle)return false;
            if(dragging)return true;
            Focus();UpdateTransform();panning=true;panStart=e.Location;panStartX=Project.Editor.PanX;panStartY=Project.Editor.PanY;Capture=true;Cursor=Cursors.Hand;return true;
        }
        bool ViewMouseMove(MouseEventArgs e)
        {
            if(!panning)return false;
            Project.Editor.PanX=Math.Max(-1000000,Math.Min(1000000,panStartX+(e.X-panStart.X)/scale));Project.Editor.PanY=Math.Max(-1000000,Math.Min(1000000,panStartY+(e.Y-panStart.Y)/scale));UpdateTransform();Invalidate();return true;
        }
        bool ViewMouseUp(MouseEventArgs e)
        {if(!panning||e.Button!=MouseButtons.Middle)return false;panning=false;Capture=false;Cursor=Cursors.Default;NotifyView();return true;}
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);if(Capture)return;
            if(panning){panning=false;Cursor=Cursors.Default;NotifyView();}
            if(dragging){dragging=false;dragRects.Clear();Cursor=Cursors.Default;if(LiveGesturePreview!=null)LiveGesturePreview(null);Invalidate();}
        }
        internal PointF ExerciseZoom(Point anchor,int delta)
        {UpdateTransform();var before=Logical(anchor);OnMouseWheel(new MouseEventArgs(MouseButtons.None,0,anchor.X,anchor.Y,delta));var after=Logical(anchor);return new PointF(after.X-before.X,after.Y-before.Y);}
        internal void ExercisePan(int dx,int dy)
        {OnMouseDown(new MouseEventArgs(MouseButtons.Middle,1,100,100,0));OnMouseMove(new MouseEventArgs(MouseButtons.Middle,0,100+dx,100+dy,0));OnMouseUp(new MouseEventArgs(MouseButtons.Middle,1,100+dx,100+dy,0));}
        internal bool ExerciseDrawn(SkinControl control){return Drawn(control);}
    }
}
