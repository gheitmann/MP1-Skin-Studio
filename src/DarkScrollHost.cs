using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    // Cover native scrollbar chrome without changing the content viewport,
    // keyboard handling, native scroll ranges or selection/focus ownership.
    public sealed class DarkScrollHost : Panel
    {
        [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)]struct BarInfo
        {public int Size;public NativeRect Bounds;public int Button,ThumbStart,ThumbEnd,Reserved;[MarshalAs(UnmanagedType.ByValArray,SizeConst=6)]public int[] State;}
        [StructLayout(LayoutKind.Sequential)]struct ScrollInfo
        {public int Size,Mask,Minimum,Maximum;public uint Page;public int Position,TrackPosition;}
        [DllImport("user32.dll")]static extern bool GetScrollBarInfo(IntPtr window,int objectId,ref BarInfo info);
        [DllImport("user32.dll")]static extern bool GetScrollInfo(IntPtr window,int bar,ref ScrollInfo info);
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
        readonly Control content;readonly DarkScrollBar vertical,horizontal;readonly Panel corner;
        readonly ScrollObserver observer;bool pending,syncing;
        public Control Content {get{return content;}}
        public DarkScrollHost(Control control)
        {
            content=control;Dock=DockStyle.Fill;Margin=Padding.Empty;BackColor=control.BackColor;
            content.Dock=DockStyle.Fill;Controls.Add(content);
            vertical=new DarkScrollBar(this,true);horizontal=new DarkScrollBar(this,false);
            corner=new Panel{BackColor=Color.FromArgb(20,26,35),Visible=false,TabStop=false};
            Controls.Add(corner);Controls.Add(horizontal);Controls.Add(vertical);
            // Native child windows must sit behind the overlay siblings on the
            // desktop; DrawToBitmap alone does not expose an incorrect z-order.
            content.SendToBack();
            observer=new ScrollObserver(this);
            content.HandleCreated+=(s,e)=>{observer.Attach(content.Handle);ScheduleSync();};
            content.HandleDestroyed+=(s,e)=>observer.Release();
            content.SizeChanged+=(s,e)=>ScheduleSync();content.VisibleChanged+=(s,e)=>ScheduleSync();
            if(content.IsHandleCreated)observer.Attach(content.Handle);
        }
        sealed class ScrollObserver : NativeWindow
        {
            readonly DarkScrollHost owner;
            public ScrollObserver(DarkScrollHost host){owner=host;}
            public void Attach(IntPtr handle){Release();AssignHandle(handle);}
            public void Release(){if(Handle!=IntPtr.Zero)ReleaseHandle();}
            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);
                if(message.Msg==0x0005||message.Msg==0x000F||message.Msg==0x0085||message.Msg==0x007D||message.Msg==0x0114||message.Msg==0x0115||message.Msg==0x020A||message.Msg>=0x0400)owner.ScheduleSync();
            }
        }
        void ScheduleSync()
        {
            if(pending||syncing||IsDisposed||Disposing||!IsHandleCreated)return;pending=true;
            BeginInvoke(new Action(()=>{pending=false;if(!IsDisposed&&!Disposing)RefreshBars();}));
        }
        public void RefreshBars()
        {
            if(syncing||IsDisposed||Disposing||!IsHandleCreated||!content.IsHandleCreated)return;
            syncing=true;
            try
            {
                SyncBar(vertical,true);SyncBar(horizontal,false);
                corner.Visible=vertical.Visible&&horizontal.Visible;
                if(corner.Visible)corner.Bounds=new Rectangle(vertical.Left,horizontal.Top,vertical.Width,horizontal.Height);
            }
            finally{syncing=false;}
        }
        void SyncBar(DarkScrollBar bar,bool isVertical)
        {
            var info=new BarInfo{Size=Marshal.SizeOf(typeof(BarInfo)),State=new int[6]};
            bool visible=GetScrollBarInfo(content.Handle,isVertical?-5:-6,ref info)&&(info.State[0]&0x18000)==0&&info.Bounds.Right>info.Bounds.Left&&info.Bounds.Bottom>info.Bounds.Top;
            if(!visible){bar.Visible=false;return;}
            var bounds=RectangleToClient(Rectangle.FromLTRB(info.Bounds.Left,info.Bounds.Top,info.Bounds.Right,info.Bounds.Bottom));
            bounds.Intersect(ClientRectangle);if(bounds.Width<=0||bounds.Height<=0){bar.Visible=false;return;}
            var scroll=new ScrollInfo{Size=Marshal.SizeOf(typeof(ScrollInfo)),Mask=0x17};
            if(!GetScrollInfo(content.Handle,isVertical?1:0,ref scroll)){bar.Visible=false;return;}
            bar.Bounds=bounds;bar.Visible=true;
            int maximum=(int)Math.Max(scroll.Minimum,Math.Min(Int32.MaxValue,(long)scroll.Maximum-Math.Max(0,(long)scroll.Page-1)));
            // These classic horizontal messages carry only 16-bit positions.
            // Keep native handling for an exceptional wider range, rather than
            // truncating the requested position of a very long item.
            if(!isVertical&&maximum>UInt16.MaxValue&&(content is TreeView||content is ListBox)){bar.Visible=false;return;}
            bar.SetRange(scroll.Minimum,maximum,(int)Math.Min(Int32.MaxValue,(long)scroll.Page),scroll.Position);
        }
        internal void ScrollContent(bool isVertical,int code,int position=0)
        {
            if(!content.IsHandleCreated)return;
            var panel=content as ScrollableControl;var list=content as ListBox;var text=content as TextBoxBase;var tree=content as TreeView;
            if(code==4&&panel!=null)
            {var point=panel.AutoScrollPosition;panel.AutoScrollPosition=new Point(isVertical?-point.X:position,isVertical?position:-point.Y);}
            else if(code==4&&isVertical&&list!=null)
            {if(list.Items.Count>0)list.TopIndex=Math.Max(0,Math.Min(list.Items.Count-1,position));}
            else if(code==4&&isVertical&&tree!=null)
            {
                var node=tree.TopNode;int steps=position-vertical.ScrollPosition;
                while(node!=null&&steps>0&&node.NextVisibleNode!=null){node=node.NextVisibleNode;steps--;}
                while(node!=null&&steps<0&&node.PrevVisibleNode!=null){node=node.PrevVisibleNode;steps++;}
                if(node!=null)tree.TopNode=node;
            }
            else if(code==4&&isVertical&&text!=null)
            {int first=SendMessage(content.Handle,0x00CE,IntPtr.Zero,IntPtr.Zero).ToInt32();SendMessage(content.Handle,0x00B6,IntPtr.Zero,new IntPtr(position-first));}
            else SendMessage(content.Handle,isVertical?0x0115:0x0114,new IntPtr((code&0xffff)|((position&0xffff)<<16)),IntPtr.Zero);
            RefreshBars();
        }
        internal void ForwardWheel(MouseEventArgs e,Control source)
        {
            var point=source.PointToScreen(e.Location);
            SendMessage(content.Handle,0x020A,new IntPtr(e.Delta<<16),new IntPtr((point.X&0xffff)|((point.Y&0xffff)<<16)));RefreshBars();
        }
        protected override void Dispose(bool disposing)
        {if(disposing)observer.Release();base.Dispose(disposing);}
    }
    public sealed class DarkScrollBar : Control
    {
        readonly DarkScrollHost host;readonly bool vertical;readonly Timer repeat=new Timer();
        int minimum,maximum,page,position,dragAt,dragPosition,repeatCode;bool dragging,hover;
        public int ScrollPosition {get{return position;}}
        public int ScrollMaximum {get{return maximum;}}
        public DarkScrollBar(DarkScrollHost owner,bool isVertical)
        {
            host=owner;vertical=isVertical;Visible=false;TabStop=false;AccessibleRole=AccessibleRole.ScrollBar;
            AccessibleName=(isVertical?"Vertical":"Horizontal")+" content scrollbar";
            SetStyle(ControlStyles.Selectable,false);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            BackColor=Color.FromArgb(20,26,35);repeat.Tick+=(s,e)=>{repeat.Interval=60;host.ScrollContent(vertical,repeatCode);};
        }
        internal void SetRange(int min,int max,int visible,int value)
        {value=Math.Max(min,Math.Min(max,value));if(minimum==min&&maximum==max&&page==visible&&position==value)return;minimum=min;maximum=max;page=visible;position=value;Invalidate();}
        int Length {get{return vertical?Height:Width;}}int Breadth {get{return vertical?Width:Height;}}
        int EndSize {get{return Math.Min(Breadth,Length/2);}}
        Rectangle Thumb
        {
            get
            {
                int track=Math.Max(0,Length-2*EndSize);long total=(long)maximum-minimum+Math.Max(1,page);
                int size=maximum<=minimum?track:Math.Min(track,Math.Max(18,(int)(track*(double)Math.Max(1,page)/Math.Max(1,total))));
                int start=EndSize+(maximum<=minimum?0:(int)Math.Round((track-size)*(double)(position-minimum)/(maximum-minimum)));
                return vertical?new Rectangle(3,start,Math.Max(1,Width-6),size):new Rectangle(start,3,size,Math.Max(1,Height-6));
            }
        }
        int Axis(Point point){return vertical?point.Y:point.X;}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);bool enabled=maximum>minimum;var thumb=Thumb;
            using(var brush=new SolidBrush(!enabled?Color.FromArgb(40,49,63):dragging?Color.FromArgb(85,158,207):hover?Color.FromArgb(92,116,143):Color.FromArgb(63,78,96)))if(thumb.Width>0&&thumb.Height>0)e.Graphics.FillRectangle(brush,thumb);
            using(var brush=new SolidBrush(enabled?Color.FromArgb(149,168,189):Color.FromArgb(63,78,96)))
            {
                int middle=Breadth/2,offset=Math.Max(3,EndSize/2);
                if(vertical){e.Graphics.FillPolygon(brush,new[]{new Point(middle-3,offset+2),new Point(middle+3,offset+2),new Point(middle,offset-2)});e.Graphics.FillPolygon(brush,new[]{new Point(middle-3,Height-offset-2),new Point(middle+3,Height-offset-2),new Point(middle,Height-offset+2)});}
                else{e.Graphics.FillPolygon(brush,new[]{new Point(offset+2,middle-3),new Point(offset+2,middle+3),new Point(offset-2,middle)});e.Graphics.FillPolygon(brush,new[]{new Point(Width-offset-2,middle-3),new Point(Width-offset-2,middle+3),new Point(Width-offset+2,middle)});}
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||maximum<=minimum)return;Capture=true;var thumb=Thumb;
            if(thumb.Contains(e.Location)){dragging=true;dragAt=Axis(e.Location);dragPosition=position;Invalidate();return;}
            int at=Axis(e.Location);repeatCode=at<EndSize?0:at>=Length-EndSize?1:at<(vertical?thumb.Top:thumb.Left)?2:3;
            host.ScrollContent(vertical,repeatCode);repeat.Interval=400;repeat.Start();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!dragging)return;var thumb=Thumb;int travel=Length-2*EndSize-(vertical?thumb.Height:thumb.Width);if(travel<=0)return;
            int target=(int)Math.Round(dragPosition+(Axis(e.Location)-dragAt)*(double)(maximum-minimum)/travel);
            host.ScrollContent(vertical,4,Math.Max(minimum,Math.Min(maximum,target)));
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {base.OnMouseUp(e);StopScrolling();host.ScrollContent(vertical,8);}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)StopScrolling();}
        void StopScrolling(){repeat.Stop();dragging=false;Capture=false;Invalidate();}
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
        protected override void OnMouseWheel(MouseEventArgs e){host.ForwardWheel(e,this);}
        protected override void Dispose(bool disposing){if(disposing)repeat.Dispose();base.Dispose(disposing);}
    }
}
