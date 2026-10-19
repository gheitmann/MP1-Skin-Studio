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
            if(PendingProperties()){ShowNotice("Apply Properties or Revert Inputs before jumping to a finding. Current inputs and selection are preserved.");return;}
            var control=project.Controls.FirstOrDefault(c=>c.Key==key);if(control==null){ShowNotice("The control is no longer present on the open page. Check the page again.");return;}
            SelectControl(control);
            foreach(TreeNode leaf in TreeControlNodes())
            {var item=leaf.Tag as SkinControl;if(item!=null&&item.Key==key){loading=true;tree.SelectedNode=leaf;leaf.EnsureVisible();loading=false;}}
            TextBox field;if(fields.TryGetValue(property,out field)){field.Focus();field.SelectAll();}
            ShowNotice("Audit finding selected: "+control.Name+" · "+property+" · "+control.Source.RelativePath+(project.IsEditorHidden(control)?" · hidden in editor":"")+(project.IsLocked(control)?" · locked":""));
        }
        public void RenderAuditTo(string path)
        {using(var dialog=new SkinProblemsDialog(SkinAudit.Run(project)))dialog.RenderTo(path);}
        void RunAuditUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");
            var film=project.Controls.First(c=>c.Get("description")=="Movies");string key=film.Key;
            project.Edit(film,new Dictionary<string,string>{{"onright","987654"},{"textureNoFocus","Studio/missing.png"}},"Audit cases");RefreshProject(key);
            var report=SkinAudit.Run(project);
            using(var dialog=new SkinProblemsDialog(report))
            {
                dialog.Show();Application.DoEvents();verify(dialog.VisibleCount>0,"The problem list shows findings for the open page");
                dialog.FilterForTest("NAV_MISSING",false);verify(dialog.VisibleCount==1&&dialog.CanJump,"Searching by audit code isolates the missing navigation reference");
                dialog.SelectCodeForTest("NAV_MISSING");verify(dialog.DetailForTest.Contains("987654")&&dialog.DetailForTest.Contains("demo.menu.xml"),"Audit details show the value and actual include source");
                dialog.JumpForTest();verify(dialog.DialogResult==DialogResult.OK&&dialog.TargetKey==key&&dialog.TargetProperty=="onright","Findings return the control key and property for navigation");
                JumpToIssue(dialog.TargetKey,dialog.TargetProperty);verify(selected.Key==key&&fields["onright"].SelectedText=="987654","Jumping selects the correct control and highlights its property");
            }
            using(var dialog=new SkinProblemsDialog(report))
            {
                dialog.Show();Application.DoEvents();dialog.FilterForTest("no-results-839204",true);verify(dialog.VisibleCount==0&&!dialog.CanJump,"Empty filters disable jumping to elements");
                dialog.FilterForTest("TEXTURE_UNCHECKED",true);verify(dialog.VisibleCount>0,"Dynamic image paths appear separately as unverified findings");
                dialog.FilterForTest("TEXTURE_UNCHECKED",false);verify(dialog.VisibleCount==0,"The information toggle hides unverified values");dialog.Close();
            }
            undo.PerformClick();var clean=SkinAudit.Run(project);verify(!clean.Issues.Any(i=>i.Code=="NAV_MISSING"&&i.ControlKey==key)&&!project.Dirty,"Rechecking after undo removes resolved navigation findings");
        }
    }
}
