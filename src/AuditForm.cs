using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void ShowAudit()
        {
            if(project==null)return;
            using(var dialog=new SkinProblemsDialog(SkinAudit.Run(project)))
                if(dialog.ShowDialog(this)==DialogResult.OK)JumpToIssue(dialog.TargetKey,dialog.TargetProperty);
        }
        void JumpToIssue(string key,string property)
        {
            var control=project.Controls.FirstOrDefault(c=>c.Key==key);if(control==null){ShowNotice("Das Control ist in der geöffneten Seite nicht mehr vorhanden. Seite erneut prüfen.");return;}
            SelectControl(control);
            foreach(TreeNode leaf in TreeControlNodes())
            {var item=leaf.Tag as SkinControl;if(item!=null&&item.Key==key){loading=true;tree.SelectedNode=leaf;leaf.EnsureVisible();loading=false;}}
            TextBox field;if(fields.TryGetValue(property,out field)){field.Focus();field.SelectAll();}
            ShowNotice("Prüffund ausgewählt: "+control.Name+" · "+property+" · "+control.Source.RelativePath+(project.IsEditorHidden(control)?" · im Editor ausgeblendet":"")+(project.IsLocked(control)?" · gesperrt":""));
        }
        public void RenderAuditTo(string path)
        {using(var dialog=new SkinProblemsDialog(SkinAudit.Run(project)))dialog.RenderTo(path);}
        void RunAuditUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");
            var film=project.Controls.First(c=>c.Get("description")=="Filme");string key=film.Key;
            project.Edit(film,new Dictionary<string,string>{{"onright","987654"},{"textureNoFocus","Studio/fehlt.png"}},"Prüffälle");RefreshProject(key);
            var report=SkinAudit.Run(project);
            using(var dialog=new SkinProblemsDialog(report))
            {
                dialog.Show();Application.DoEvents();verify(dialog.VisibleCount>0,"Problemliste zeigt Fundstellen der geöffneten Seite");
                dialog.FilterForTest("NAV_MISSING",false);verify(dialog.VisibleCount==1&&dialog.CanJump,"Suche nach Prüfcode filtert exakt den fehlenden Navigationsverweis");
                dialog.SelectCodeForTest("NAV_MISSING");verify(dialog.DetailForTest.Contains("987654")&&dialog.DetailForTest.Contains("demo.menu.xml"),"Prüfdetails zeigen Wert und tatsächliche Include-Quelle");
                dialog.JumpForTest();verify(dialog.DialogResult==DialogResult.OK&&dialog.TargetKey==key&&dialog.TargetProperty=="onright","Fundstelle gibt Control-Schlüssel und Eigenschaft zum Anspringen zurück");
                JumpToIssue(dialog.TargetKey,dialog.TargetProperty);verify(selected.Key==key&&fields["onright"].SelectedText=="987654","Sprung wählt richtiges Control und markiert die betroffene Eigenschaft");
            }
            using(var dialog=new SkinProblemsDialog(report))
            {
                dialog.Show();Application.DoEvents();dialog.FilterForTest("kein-ergebnis-839204",true);verify(dialog.VisibleCount==0&&!dialog.CanJump,"Leerer Filter deaktiviert Sprung zum Element");
                dialog.FilterForTest("TEXTURE_UNCHECKED",true);verify(dialog.VisibleCount>0,"Dynamische Bildpfade erscheinen separat als ungeprüfte Hinweise");
                dialog.FilterForTest("TEXTURE_UNCHECKED",false);verify(dialog.VisibleCount==0,"Hinweisschalter blendet ungeprüfte Werte aus");dialog.Close();
            }
            undo.PerformClick();var clean=SkinAudit.Run(project);verify(!clean.Issues.Any(i=>i.Code=="NAV_MISSING"&&i.ControlKey==key)&&!project.Dirty,"Erneute Prüfung nach Undo verwirft behobenen Navigationsfund");
        }
    }
}
