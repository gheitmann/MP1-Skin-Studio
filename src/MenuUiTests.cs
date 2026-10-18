using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RunMenuUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");
            var pageMenu=page.OpenChoicesForTest();pageMenu.ChooseForTest(page.FindStringExact("common.time.xml"));Application.DoEvents();
            verify(project.Page=="common.time.xml"&&!pageMenu.IsDisposed,"Page selection through real keyboard menu input closes without disposal errors");
            var again=page.OpenChoicesForTest();again.ChooseForTest(page.FindStringExact("demo.xml"));Application.DoEvents();
            verify(Object.ReferenceEquals(pageMenu,again)&&project.Page=="demo.xml","Repeated page selection reuses the same bounded menu instance");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));
            var picker=fields["font"].Parent.Controls.OfType<Button>().First(b=>b.Tag==null);picker.PerformClick();var fontMenu=StudioMenus.PeekForTest(this,"property-picker");fontMenu.ChooseForTest(0);Application.DoEvents();
            verify(fields["font"].Text==project.FontNames[0]&&!fontMenu.IsDisposed,"Font menus select entries and close normally");
            SelectControl(selected);var resetAnchor=fields["width"].Parent.Controls.OfType<Button>().First(b=>Convert.ToString(b.Tag)=="reset:width");resetAnchor.PerformClick();var resetMenu=StudioMenus.PeekForTest(this,"property-reset");resetMenu.ChooseForTest(resetMenu.Items.Count-1);Application.DoEvents();
            verify(selected.Node.Child("width")==null&&resetAnchor.IsDisposed&&!resetMenu.IsDisposed,"Reset menus survive rebuilding their own anchor button");undo.PerformClick();
            int before=project.Controls.Count;ShowInsertMenu(canvas);var insertMenu=StudioMenus.PeekForTest(this,"insert");insertMenu.ChooseForTest(1);Application.DoEvents();
            verify(project.Controls.Count==before+1&&selected.Get("type")=="label"&&!insertMenu.IsDisposed,"Menu insertion processes clicks completely after closing");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);ShowAlignMenu(canvas);var alignMenu=StudioMenus.PeekForTest(this,"align");alignMenu.ChooseForTest(0);Application.DoEvents();
            verify(SelectedControls().All(c=>c.Get("posX")=="140")&&!alignMenu.IsDisposed,"Menu alignment remains stable while rebuilding property controls");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));var protection=(StudioMenu)ProtectionMenu();protection.Show(tree,new Point(12,12));protection.ChooseForTest(0);Application.DoEvents();bool locked=project.IsLocked(selected);
            protection=(StudioMenu)ProtectionMenu();protection.Show(tree,new Point(12,12));protection.ChooseForTest(0);Application.DoEvents();
            verify(locked&&!project.IsLocked(selected)&&!protection.IsDisposed,"Context menus support consecutive locking and unlocking");
            using(var dialog=new PreviewSettingsDialog(project))
            {
                dialog.Show();Application.DoEvents();var presetMenu=dialog.OpenPresetMenuForTest();presetMenu.ChooseForTest(2);Application.DoEvents();
                bool longTitle=dialog.CollectForTest().Values["#title"].Length>80;var second=dialog.OpenPresetMenuForTest();second.ChooseForTest(4);Application.DoEvents();
                verify(longTitle&&dialog.CollectForTest().Values["#myNotifier.Messages.Count.Unread"]=="12"&&Object.ReferenceEquals(presetMenu,second),"Sample presets can repeatedly change through the real dropdown without crashing");dialog.Close();
            }
            StudioMenu retained=null;
            using(var owner=new Form())
            {
                var anchor=new Button();owner.Controls.Add(anchor);owner.Show();Application.DoEvents();retained=StudioMenus.For(anchor,"regression");bool completed=false,aliveDuringNestedLoop=false;
                retained.Items.Add("Rebuild",null,(s,e)=>{anchor.Dispose();Application.DoEvents();aliveDuringNestedLoop=!retained.IsDisposed;completed=true;});retained.Show(anchor,new Point(0,anchor.Height));retained.ChooseForTest(0);Application.DoEvents();
                verify(completed&&aliveDuringNestedLoop&&!retained.IsDisposed,"Menus remain valid after anchor disposal and nested event loops");
                var replacement=new Button();owner.Controls.Add(replacement);var reused=StudioMenus.For(replacement,"regression");int calls=0;reused.Items.Add("Once",null,(s,e)=>calls++);reused.Show(replacement,new Point(0,replacement.Height));reused.ChooseForTest(0);Application.DoEvents();
                verify(Object.ReferenceEquals(retained,reused)&&calls==1&&reused.Items.Count==1,"Menu reuse removes previous items and callback references");
                reused=StudioMenus.For(replacement,"regression");reused.Items.Add("Do not execute",null,(s,e)=>calls++);reused.Show(replacement,new Point(0,replacement.Height));reused.EscapeForTest();Application.DoEvents();verify(calls==1&&!reused.Visible&&!reused.IsDisposed,"Escape closes menus without actions or premature disposal");
            }
            verify(retained.IsDisposed,"Menus are cleaned up when their owner window is disposed");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
