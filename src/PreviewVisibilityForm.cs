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
            menu.Items.Add("Sample Artwork …",null,(s,e)=>Attempt(ShowArtworkPreview));
            menu.Items.Add("Video folder preview",null,(s,e)=>Attempt(ApplyVideoFolderPreview));
            menu.Items.Add("Music folder preview",null,(s,e)=>Attempt(()=>ApplyMediaFolderPreview("music")));
            menu.Items.Add("Pictures folder preview",null,(s,e)=>Attempt(()=>ApplyMediaFolderPreview("pictures")));
            menu.Items.Add("BasicHome preview",null,(s,e)=>Attempt(ApplyBasicHomePreview));
            var pin=new ToolStripMenuItem("Video PIN preview");pin.DropDownItems.Add("Locked",null,(s,e)=>Attempt(()=>ApplyVideoPinPreview(true)));pin.DropDownItems.Add("Unlocked",null,(s,e)=>Attempt(()=>ApplyVideoPinPreview(false)));pin.DropDownItems.Add("Unverified",null,(s,e)=>Attempt(()=>ApplyVideoPinPreview(null)));menu.Items.Add(pin);
            var playback=new ToolStripMenuItem("Playback preview");
            playback.DropDownItems.Add("Stopped",null,(s,e)=>Attempt(()=>ApplyPlaybackPreview(true)));
            playback.DropDownItems.Add("Unverified",null,(s,e)=>Attempt(()=>ApplyPlaybackPreview(false)));menu.Items.Add(playback);
            var idle=new ToolStripMenuItem("Idle-time preview");
            idle.DropDownItems.Add("Active input (0 seconds)",null,(s,e)=>Attempt(()=>ApplyIdlePreview("0")));
            idle.DropDownItems.Add("Idle (60 seconds)",null,(s,e)=>Attempt(()=>ApplyIdlePreview("60")));
            idle.DropDownItems.Add("Unverified",null,(s,e)=>Attempt(()=>ApplyIdlePreview(null)));menu.Items.Add(idle);
            menu.Items.Add("Review XML changes … (Ctrl+Shift+E)",null,(s,e)=>Attempt(ShowChangeReview));
            menu.Items.Add("Inspect unverified preview …",null,(s,e)=>Attempt(ShowPreviewProblems));
            menu.Items.Add(new ToolStripSeparator());var item=(ToolStripMenuItem)menu.Items.Add("Show unverified content",null,(s,e)=>SetUnresolvedPreview(!project.Editor.ShowUnresolved));item.Checked=project.Editor.ShowUnresolved;
        }
    }
}
