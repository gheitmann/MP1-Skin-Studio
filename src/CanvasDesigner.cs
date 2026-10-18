using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class SkinCanvas
    {
        public readonly HashSet<string> SelectedKeys=new HashSet<string>();
        public bool NavigationMode;
        public event Action<SkinControl,bool> SelectRequest;
        public event Action<List<ControlEdit>> CommitBatch;
        public event Action<SkinControl> ConnectRequest;
        public event Action<string> FollowRequest;
        readonly Dictionary<string,RectangleF> dragRects=new Dictionary<string,RectangleF>();
        readonly Dictionary<string,RectangleF> startRects=new Dictionary<string,RectangleF>();
        SkinControl[] dragControls=new SkinControl[0];
        Point dragStartClient;bool dragMoved;
        void DesignerMouseDown(MouseEventArgs e)
        {
            if(Project==null||e.Button!=MouseButtons.Left||panning)return;if(AllowGesture!=null&&!AllowGesture())return;Focus();PointF p=Logical(e.Location);
            if(NavigationMode)
            {
                var target=Project.Controls.LastOrDefault(c=>Drawn(c)&&SkinProject.Focusable(c)&&BoundsOf(c).Contains(p));
                if(target!=null&&ConnectRequest!=null)ConnectRequest(target);return;
            }
            resizing=SelectedKeys.Count<=1&&Selected!=null&&Drawn(Selected)&&!Project.IsLocked(Selected)&&Math.Abs(p.X-BoundsOf(Selected).Right)<12/scale&&Math.Abs(p.Y-BoundsOf(Selected).Bottom)<12/scale;
            var hit=resizing?Selected:Project.Controls.LastOrDefault(c=>Drawn(c)&&!Project.IsLocked(c)&&BoundsOf(c).Contains(p));
            if(hit==null)return;
            bool toggle=(ModifierKeys&Keys.Control)!=0;
            if(toggle||Selected==null||Selected.Key!=hit.Key)
            {
                if(SelectRequest!=null)SelectRequest(hit,toggle);
                else{Selected=hit;SelectedKeys.Clear();SelectedKeys.Add(hit.Key);if(Selection!=null)Selection(hit);}
            }
            if(toggle||Selected==null)return;
            if(!SelectedKeys.Contains(hit.Key)){SelectedKeys.Clear();SelectedKeys.Add(hit.Key);}
            dragControls=Project.Controls.Where(c=>SelectedKeys.Contains(c.Key)).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
            if(dragControls.Any(c=>Project.IsLocked(c)||Project.IsEditorHidden(c)))
            {if(Notice!=null)Notice("Gesperrte oder im Editor ausgeblendete Elemente zuerst im Baum freigeben. Die gesamte Auswahl bleibt unverändert.");return;}
            string[] keys=resizing?new[]{"width","height"}:new[]{"posX","posY"};
            if(!UnlockBindings&&dragControls.Any(c=>keys.Any(c.IsBound)))
            {if(Notice!=null)Notice("Mindestens ein Element enthält eine Bindung. Zum Ersetzen rechts ausdrücklich freigeben.");Invalidate();return;}
            try{foreach(var item in dragControls){LayoutTools.RequireManualGeometry(item);LayoutTools.RequireNumeric(item,keys);}}catch(Exception ex){if(Notice!=null)Notice(ex.Message);return;}
            oldX=Selected.Number("posX",0);oldY=Selected.Number("posY",0);oldW=BoundsOf(Selected).Width;oldH=BoundsOf(Selected).Height;
            mouseStart=p;startRects.Clear();dragRects.Clear();
            foreach(var c in dragControls){startRects[c.Key]=BoundsOf(c);dragRects[c.Key]=BoundsOf(c);}
            dragStartClient=e.Location;dragMoved=false;dragging=true;Capture=true;Invalidate();
        }
        void DesignerMouseMove(MouseEventArgs e)
        {
            if(!dragging||Selected==null)return;
            if(!dragMoved){if(Math.Abs(e.X-dragStartClient.X)<Math.Max(2,SystemInformation.DragSize.Width/2)&&Math.Abs(e.Y-dragStartClient.Y)<Math.Max(2,SystemInformation.DragSize.Height/2))return;dragMoved=true;}
            var p=Logical(e.Location);
            float dx=p.X-mouseStart.X,dy=p.Y-mouseStart.Y;
            float snappedX=Round(oldX+dx)-oldX,snappedY=Round(oldY+dy)-oldY;
            foreach(var c in dragControls)
            {
                var r=startRects[c.Key];
                if(resizing){r.Width=Math.Max(5,Round(oldW+dx));r.Height=Math.Max(5,Round(oldH+dy));}
                else{r.X+=snappedX;r.Y+=snappedY;}
                dragRects[c.Key]=r;
            }
            Cursor=resizing?Cursors.SizeNWSE:Cursors.SizeAll;Invalidate();
        }
        void DesignerMouseUp(MouseEventArgs e)
        {
            if(!dragging||Selected==null)return;
            var edits=new List<ControlEdit>();
            foreach(var c in dragControls)
            {
                var before=startRects[c.Key];var after=dragRects[c.Key];var values=new Dictionary<string,string>();
                if(resizing){if(Math.Abs(after.Width-before.Width)>.1)values["width"]=((int)after.Width).ToString();if(Math.Abs(after.Height-before.Height)>.1)values["height"]=((int)after.Height).ToString();}
                else{if(Math.Abs(after.X-before.X)>.1)values["posX"]=((int)(c.Number("posX",0)+after.X-before.X)).ToString();if(Math.Abs(after.Y-before.Y)>.1)values["posY"]=((int)(c.Number("posY",0)+after.Y-before.Y)).ToString();}
                if(values.Count>0)edits.Add(new ControlEdit(c,values));
            }
            dragging=false;Capture=false;Cursor=Cursors.Default;dragRects.Clear();
            if(edits.Count>0){if(CommitBatch!=null)CommitBatch(edits);else if(Commit!=null)foreach(var edit in edits)Commit(edit.Control,new Dictionary<string,string>(edit.Values));}
            Invalidate();
        }
        void DesignerKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Escape&&panning){Project.Editor.PanX=panStartX;Project.Editor.PanY=panStartY;panning=false;Capture=false;Cursor=Cursors.Default;UpdateTransform();NotifyView();e.Handled=true;}
            if(e.KeyCode==Keys.Escape&&dragging){dragging=false;Capture=false;Cursor=Cursors.Default;dragRects.Clear();Invalidate();e.Handled=true;}
            if(NavigationMode&&FollowRequest!=null)
            {
                string dir=e.KeyCode==Keys.Left?"onleft":e.KeyCode==Keys.Right?"onright":e.KeyCode==Keys.Up?"onup":e.KeyCode==Keys.Down?"ondown":null;
                if(dir!=null){FollowRequest(dir);e.Handled=true;}
            }
        }
        void DrawDesignerOverlay(Graphics g)
        {
            if(Selected==null)return;
            foreach(var c in Project.Controls.Where(c=>SelectedKeys.Contains(c.Key)).GroupBy(c=>c.Key).Select(x=>x.First()))
            {
                if(Project.IsEditorHidden(c))continue;var rect=EffectiveBounds(c);
                using(var pen=new Pen(Project.IsLocked(c)?Color.FromArgb(244,193,102):c.Key==Selected.Key?Color.FromArgb(85,186,255):Color.FromArgb(83,211,190),2/scale))g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);
                if(SelectedKeys.Count==1&&!Project.IsLocked(c)&&!c.AutomaticLayout&&!LayoutTools.IsContainer(c)){float size=9/scale;using(var b=new SolidBrush(Color.FromArgb(100,197,255)))g.FillRectangle(b,rect.Right-size/2,rect.Bottom-size/2,size,size);}
            }
            if(!NavigationMode||Project.IsEditorHidden(Selected))return;
            string[] directions={"onleft","onright","onup","ondown"},names={"Links","Rechts","Oben","Unten"};
            Color[] colors={Color.FromArgb(93,223,187),Color.FromArgb(98,191,255),Color.FromArgb(191,157,255),Color.FromArgb(255,192,101)};
            var source=BoundsOf(Selected);PointF from=new PointF(source.X+source.Width/2,source.Y+source.Height/2);
            for(int i=0;i<directions.Length;i++)
            {
                SkinControl target;try{target=Project.NavigationTarget(Selected,directions[i]);}catch{continue;}
                if(Project.IsEditorHidden(target))continue;var r=BoundsOf(target);PointF to=new PointF(r.X+r.Width/2,r.Y+r.Height/2);
                using(var pen=new Pen(colors[i],2.5f/scale))
                using(var cap=new AdjustableArrowCap(5,6))
                {
                    pen.CustomEndCap=cap;
                    if(target.Key==Selected.Key)g.DrawArc(pen,source.Right-35,source.Y-50,70,70,0,300);
                    else{float offset=(i-1.5f)*12/scale;from.Y+=offset;to.Y+=offset;g.DrawLine(pen,from,to);from.Y-=offset;}
                }
                using(var font=new Font("Segoe UI",12/scale,FontStyle.Bold,GraphicsUnit.Pixel))
                using(var brush=new SolidBrush(colors[i]))
                {float x=(from.X+to.X)/2,y=(from.Y+to.Y)/2+(i-2)*18/scale;g.DrawString(names[i]+" · "+target.Get("id"),font,brush,x,y);}
            }
        }
        internal void ExerciseFollow(Keys key){OnKeyDown(new KeyEventArgs(key));}
        internal void ExerciseConnection(SkinControl target)
        {UpdateTransform();var r=BoundsOf(target);var p=new Point((int)(origin.X+(r.X+r.Width/2)*scale),(int)(origin.Y+(r.Y+r.Height/2)*scale));OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
    }
}
