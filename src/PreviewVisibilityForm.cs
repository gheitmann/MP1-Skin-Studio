using System;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        public void SetUnresolvedPreview(bool show)
        {project.Editor.ShowUnresolved=show;canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice(show?"Unverified conditions and content are shown for inspection.":"Unverified conditions and content are hidden only in the preview; XML remains unchanged.");}
        void AddVisibilityCommands(ContextMenuStrip menu)
        {
            menu.Items.Add("Collection Preview …",null,(s,e)=>Attempt(ShowCollectionSettings));
            menu.Items.Add("Video folder preview",null,(s,e)=>Attempt(ApplyVideoFolderPreview));
            menu.Items.Add("BasicHome preview",null,(s,e)=>Attempt(ApplyBasicHomePreview));
            var playback=new ToolStripMenuItem("Playback preview");
            playback.DropDownItems.Add("Stopped",null,(s,e)=>Attempt(()=>ApplyPlaybackPreview(true)));
            playback.DropDownItems.Add("Unverified",null,(s,e)=>Attempt(()=>ApplyPlaybackPreview(false)));menu.Items.Add(playback);
            menu.Items.Add(new ToolStripSeparator());var item=(ToolStripMenuItem)menu.Items.Add("Show unverified content",null,(s,e)=>SetUnresolvedPreview(!project.Editor.ShowUnresolved));item.Checked=project.Editor.ShowUnresolved;
        }
    }
}
