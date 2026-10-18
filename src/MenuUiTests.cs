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
            verify(project.Page=="common.time.xml"&&!pageMenu.IsDisposed,"Seitenauswahl über echten Menü-Tastaturpfad schließt Dropdown ohne Dispose-Absturz");
            var again=page.OpenChoicesForTest();again.ChooseForTest(page.FindStringExact("demo.xml"));Application.DoEvents();
            verify(Object.ReferenceEquals(pageMenu,again)&&project.Page=="demo.xml","Wiederholte Seitenauswahl verwendet dieselbe begrenzte Menüinstanz");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));
            var picker=fields["font"].Parent.Controls.OfType<Button>().First(b=>b.Tag==null);picker.PerformClick();var fontMenu=StudioMenus.PeekForTest(this,"property-picker");fontMenu.ChooseForTest(0);Application.DoEvents();
            verify(fields["font"].Text==project.FontNames[0]&&!fontMenu.IsDisposed,"Schriftmenü führt Auswahl und normales Dropdown-Schließen aus");
            SelectControl(selected);var resetAnchor=fields["width"].Parent.Controls.OfType<Button>().First(b=>Convert.ToString(b.Tag)=="reset:width");resetAnchor.PerformClick();var resetMenu=StudioMenus.PeekForTest(this,"property-reset");resetMenu.ChooseForTest(resetMenu.Items.Count-1);Application.DoEvents();
            verify(selected.Node.Child("width")==null&&resetAnchor.IsDisposed&&!resetMenu.IsDisposed,"Reset-Menü überlebt das Neuerstellen seiner eigenen Anker-Schaltfläche");undo.PerformClick();
            int before=project.Controls.Count;ShowInsertMenu(canvas);var insertMenu=StudioMenus.PeekForTest(this,"insert");insertMenu.ChooseForTest(1);Application.DoEvents();
            verify(project.Controls.Count==before+1&&selected.Get("type")=="label"&&!insertMenu.IsDisposed,"Einfügen über Menü verarbeitet Klick nach dem Schließen vollständig");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"),true,false);ShowAlignMenu(canvas);var alignMenu=StudioMenus.PeekForTest(this,"align");alignMenu.ChooseForTest(0);Application.DoEvents();
            verify(SelectedControls().All(c=>c.Get("posX")=="140")&&!alignMenu.IsDisposed,"Ausrichten über Menü bleibt trotz Eigenschafts-Neuaufbau stabil");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));var protection=(StudioMenu)ProtectionMenu();protection.Show(tree,new Point(12,12));protection.ChooseForTest(0);Application.DoEvents();bool locked=project.IsLocked(selected);
            protection=(StudioMenu)ProtectionMenu();protection.Show(tree,new Point(12,12));protection.ChooseForTest(0);Application.DoEvents();
            verify(locked&&!project.IsLocked(selected)&&!protection.IsDisposed,"Sperren und Entsperren per Kontextmenü können direkt nacheinander erfolgen");
            using(var dialog=new PreviewSettingsDialog(project))
            {
                dialog.Show();Application.DoEvents();var presetMenu=dialog.OpenPresetMenuForTest();presetMenu.ChooseForTest(2);Application.DoEvents();
                bool longTitle=dialog.CollectForTest().Values["#title"].Length>80;var second=dialog.OpenPresetMenuForTest();second.ChooseForTest(4);Application.DoEvents();
                verify(longTitle&&dialog.CollectForTest().Values["#myNotifier.Messages.Count.Unread"]=="12"&&Object.ReferenceEquals(presetMenu,second),"Testdaten-Vorlagen über echten Dropdown-Pfad mehrfach ohne Absturz wechseln");dialog.Close();
            }
            StudioMenu retained=null;
            using(var owner=new Form())
            {
                var anchor=new Button();owner.Controls.Add(anchor);owner.Show();Application.DoEvents();retained=StudioMenus.For(anchor,"regression");bool completed=false,aliveDuringNestedLoop=false;
                retained.Items.Add("Neu aufbauen",null,(s,e)=>{anchor.Dispose();Application.DoEvents();aliveDuringNestedLoop=!retained.IsDisposed;completed=true;});retained.Show(anchor,new Point(0,anchor.Height));retained.ChooseForTest(0);Application.DoEvents();
                verify(completed&&aliveDuringNestedLoop&&!retained.IsDisposed,"Menü bleibt auch bei zerstörtem Anker und verschachtelter Ereignisschleife gültig");
                var replacement=new Button();owner.Controls.Add(replacement);var reused=StudioMenus.For(replacement,"regression");int calls=0;reused.Items.Add("Einmal",null,(s,e)=>calls++);reused.Show(replacement,new Point(0,replacement.Height));reused.ChooseForTest(0);Application.DoEvents();
                verify(Object.ReferenceEquals(retained,reused)&&calls==1&&reused.Items.Count==1,"Menü-Wiederverwendung entfernt alte Einträge und Callback-Referenzen");
                reused=StudioMenus.For(replacement,"regression");reused.Items.Add("Nicht ausführen",null,(s,e)=>calls++);reused.Show(replacement,new Point(0,replacement.Height));reused.EscapeForTest();Application.DoEvents();verify(calls==1&&!reused.Visible&&!reused.IsDisposed,"Escape schließt Menü ohne Aktion oder vorzeitige Freigabe");
            }
            verify(retained.IsDisposed,"Menüs werden beim Freigeben ihres Besitzerfensters aufgeräumt");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
