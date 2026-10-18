using System;
using System.Collections.Generic;
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
        internal bool ChooseForTest(int index)
        {Items[index].Select();return base.ProcessDialogKey(Keys.Enter);}
        internal bool EscapeForTest(){return base.ProcessDialogKey(Keys.Escape);}
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
                foreach(var item in menu.Items.Cast<ToolStripItem>().ToArray())item.Dispose();
                menu.Items.Clear();menu.ShowCheckMargin=true;menu.ShowImageMargin=false;return menu;
            }
        }
        static readonly ConditionalWeakTable<Form,OwnerMenus> owners=new ConditionalWeakTable<Form,OwnerMenus>();
        internal static StudioMenu PeekForTest(Form owner,string purpose){return owners.GetValue(owner,key=>new OwnerMenus(key)).Peek(purpose);}
        public static StudioMenu For(Control anchor,string purpose)
        {
            if(anchor==null)throw new ArgumentNullException("anchor");
            var owner=anchor as Form??anchor.FindForm();if(owner==null||owner.IsDisposed||owner.Disposing)throw new InvalidOperationException("Das Menü benötigt ein geöffnetes Editorfenster.");
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
