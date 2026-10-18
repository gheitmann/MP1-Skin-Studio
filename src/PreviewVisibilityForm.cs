using System;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        public void SetUnresolvedPreview(bool show)
        {project.Editor.ShowUnresolved=show;canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice(show?"Unsupported conditions are shown with dotted markers.":"Unsupported conditions are hidden only in the preview; XML conditions remain unchanged.");}
        void AddVisibilityCommands(ContextMenuStrip menu)
        {
            menu.Items.Add("Collection Preview …",null,(s,e)=>Attempt(ShowCollectionSettings));
            menu.Items.Add(new ToolStripSeparator());var item=(ToolStripMenuItem)menu.Items.Add("Show unverified conditions",null,(s,e)=>SetUnresolvedPreview(!project.Editor.ShowUnresolved));item.Checked=project.Editor.ShowUnresolved;
        }
    }
}
