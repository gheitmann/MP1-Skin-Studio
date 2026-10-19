using System;
using System.Linq;

namespace MP1SkinStudio
{
    public static class BasicHomePreview
    {
        public static void Apply(SkinProject project)
        {
            if(project.WindowId!=35||!String.Equals(System.IO.Path.GetFileName(project.Page),"BasicHome.xml",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Open BasicHome.xml before applying its sample state.");
            var preview=project.Editor.Preview.Copy();
            preview.Values["#skin.basichome.background"]="FanartHandler";
            preview.Values["#Titan.ShowUpdateInd"]="false";PlaybackPreview.Stopped(preview);preview.Validate();
            bool focus=project.DefaultControlId>0&&project.Controls.Any(c=>SkinProject.Focusable(c)&&SkinProject.ControlId(c)==project.DefaultControlId);
            if(focus)project.Editor.ValidateFocus(project.Page,project.Theme,project.DefaultControlId);
            project.Editor.Preview.SetFrom(preview);project.Editor.ShowUnresolved=false;
            if(focus)
            {project.Editor.RememberFocus(project.Page,project.Theme,project.DefaultControlId);}
        }
    }
    public sealed partial class StudioForm
    {
        void ApplyBasicHomePreview()
        {
            RequireAppliedProperties();BasicHomePreview.Apply(project);ApplyPreviewSettings(project.Editor.Preview);state.RestoreFocus();RefreshProject(null);
            ShowNotice("BasicHome sample applied: stopped playback, FanartHandler mode, update indicator off. Runtime fanart and latest items remain unverified. Save the editor project.");
        }
    }
}
