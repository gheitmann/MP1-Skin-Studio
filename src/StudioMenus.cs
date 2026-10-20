using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    // WinForms still reparents a dropdown and dispatches its item callback after
    // Closed. Menus must survive that event, even when the callback rebuilds the
    // property controls or enters a nested message loop. Keep one reusable menu
    // per purpose, owned by the form rather than its short-lived anchor control.
    public sealed class StudioMenu : ContextMenuStrip
    {
        public StudioMenu()
        {
            if(SystemInformation.HighContrast){RenderMode=ToolStripRenderMode.System;return;}
            BackColor=Color.FromArgb(20,26,35);ForeColor=Color.FromArgb(225,234,244);
            Renderer=new DarkMenuRenderer();
        }
        internal bool ChooseForTest(int index)
        {Items[index].Select();return base.ProcessDialogKey(Keys.Enter);}
        internal bool EscapeForTest(){return base.ProcessDialogKey(Keys.Escape);}
    }
    sealed class DarkMenuColors : ProfessionalColorTable
    {
        static readonly Color background=Color.FromArgb(20,26,35),thumb=Color.FromArgb(63,78,96),hover=Color.FromArgb(92,116,143);
        public DarkMenuColors(){UseSystemColors=false;}
        public override Color ToolStripDropDownBackground {get{return background;}}
        public override Color ImageMarginGradientBegin {get{return background;}}
        public override Color ImageMarginGradientMiddle {get{return background;}}
        public override Color ImageMarginGradientEnd {get{return background;}}
        public override Color MenuBorder {get{return thumb;}}
        public override Color MenuItemSelected {get{return thumb;}}
        public override Color MenuItemBorder {get{return hover;}}
        public override Color MenuItemSelectedGradientBegin {get{return thumb;}}
        public override Color MenuItemSelectedGradientEnd {get{return thumb;}}
        public override Color MenuItemPressedGradientBegin {get{return hover;}}
        public override Color MenuItemPressedGradientMiddle {get{return hover;}}
        public override Color MenuItemPressedGradientEnd {get{return hover;}}
        public override Color CheckBackground {get{return thumb;}}
        public override Color CheckSelectedBackground {get{return hover;}}
        public override Color CheckPressedBackground {get{return hover;}}
        public override Color SeparatorDark {get{return thumb;}}
        public override Color SeparatorLight {get{return thumb;}}
    }
    sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer():base(new DarkMenuColors()){RoundedEdges=false;}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {e.ArrowColor=e.Item.Enabled?Color.FromArgb(225,234,244):Color.FromArgb(149,168,189);base.OnRenderArrow(e);}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {e.TextColor=e.Item.Enabled?Color.FromArgb(225,234,244):Color.FromArgb(149,168,189);base.OnRenderItemText(e);}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var r=e.ImageRectangle;float x=r.X+r.Width/2f,y=r.Y+r.Height/2f;
            using(var pen=new Pen(Color.FromArgb(225,234,244),1.8f))e.Graphics.DrawLines(pen,new[]{new PointF(x-4,y),new PointF(x-1,y+3),new PointF(x+5,y-4)});
        }
    }
    public static class StudioMenus
    {
        sealed class OwnerMenus
        {
            readonly Dictionary<string,StudioMenu> menus=new Dictionary<string,StudioMenu>();
            public OwnerMenus(Form owner){owner.Disposed+=(s,e)=>{foreach(var menu in menus.Values)menu.Dispose();menus.Clear();};}
            public StudioMenu Peek(string purpose){return menus[purpose];}
            public StudioMenu Prepare(string purpose)
            {
                StudioMenu menu;if(!menus.TryGetValue(purpose,out menu)||menu.IsDisposed){menu=new StudioMenu();menus[purpose]=menu;}
                if(menu.Visible)menu.Close();
                // Clearing a large page selector otherwise lays out every
                // remaining item after each removal on the UI thread.
                menu.SuspendLayout();
                try
                {
                    foreach(var item in menu.Items.Cast<ToolStripItem>().ToArray())item.Dispose();
                    menu.Items.Clear();menu.ShowCheckMargin=true;menu.ShowImageMargin=false;
                }
                finally{menu.ResumeLayout(false);}
                return menu;
            }
        }
        static readonly ConditionalWeakTable<Form,OwnerMenus> owners=new ConditionalWeakTable<Form,OwnerMenus>();
        internal static StudioMenu PeekForTest(Form owner,string purpose){return owners.GetValue(owner,key=>new OwnerMenus(key)).Peek(purpose);}
        public static StudioMenu For(Control anchor,string purpose)
        {
            if(anchor==null)throw new ArgumentNullException("anchor");
            var owner=anchor as Form??anchor.FindForm();if(owner==null||owner.IsDisposed||owner.Disposing)throw new InvalidOperationException("The menu requires an open editor window.");
            return owners.GetValue(owner,key=>new OwnerMenus(key)).Prepare(purpose);
        }
    }
    public static class StudioErrors
    {
        public static string Record(Exception error)
        {
            string path=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log");
            try{System.IO.File.WriteAllText(path,DateTime.UtcNow.ToString("O")+Environment.NewLine+error);return path;}
            catch
            {
                try{path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"MP1SkinStudio-error.log");System.IO.File.WriteAllText(path,DateTime.UtcNow.ToString("O")+Environment.NewLine+error);return path;}
                catch{return null;}
            }
        }
    }
}
