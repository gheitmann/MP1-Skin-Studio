using System;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RequireAppliedProperties()
        {
            if(PendingProperties())throw new InvalidOperationException("Property values have not been applied. Choose Apply Properties or restore their previous values first; the current inputs are preserved.");
        }
        void OpenSelectedPage()
        {
            try
            {
                RequireAppliedProperties();project.OpenPage(page.SelectedItem.ToString());
                state.FocusId=project.DefaultControlId;RefreshProject(null);
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
            project.Theme=theme.SelectedIndex<=0?"":theme.SelectedItem.ToString();ReloadPages(project.Page);
        }
    }
}
