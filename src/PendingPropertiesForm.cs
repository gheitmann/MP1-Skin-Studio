using System;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RevertPropertyInputs()
        {
            if(selected==null||!PendingProperties())return;
            SelectControl(selected,false,true);UpdateProjectCaption();export.Enabled=project.Dirty;
            ShowNotice("Unapplied property inputs reverted. Applied XML changes and undo history are unchanged.");
        }
        void RequireAppliedProperties()
        {
            if(PendingProperties())throw new InvalidOperationException("Property values have not been applied. Choose Apply Properties or Revert Inputs first; the current inputs are preserved.");
        }
        void OpenSelectedPage()
        {
            try
            {
                RequireAppliedProperties();project.OpenPage(page.SelectedItem.ToString());
                state.RestoreFocus();RefreshProject(null);
            }
            catch
            {
                bool prior=loading;loading=true;
                try{page.SelectedIndex=page.FindStringExact(project.Page);}finally{loading=prior;}
                throw;
            }
        }
        void ChangeSelectedTheme()
        {
            if(PendingProperties())
            {
                bool prior=loading;loading=true;
                try{theme.SelectedIndex=project.Theme.Length==0?0:theme.FindStringExact(project.Theme);}finally{loading=prior;}
                RequireAppliedProperties();
            }
            try
            {
                var prepared=project.PrepareTheme(theme.SelectedIndex<=0?"":theme.SelectedItem.ToString());
                state=prepared.Preview;canvas.State=state;canvas.ResetAssets();PopulatePageChoices(prepared.Pages);RefreshProject(null);
            }
            catch
            {
                bool prior=loading;loading=true;
                try{theme.SelectedIndex=project.Theme.Length==0?0:theme.FindStringExact(project.Theme);}finally{loading=prior;}
                throw;
            }
        }
    }
}
