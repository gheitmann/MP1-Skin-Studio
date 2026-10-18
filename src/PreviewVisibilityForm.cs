using System;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        public void SetUnresolvedPreview(bool show)
        {project.Editor.ShowUnresolved=show;canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice(show?"Nicht auswertbare Bedingungen werden gepunktet dargestellt.":"Nicht auswertbare Bedingungen nur in der Vorschau ausgeblendet; XML-Bedingungen bleiben unverändert.");}
        void AddVisibilityCommands(ContextMenuStrip menu)
        {
            menu.Items.Add("Sammlungs-Vorschau …",null,(s,e)=>Attempt(ShowCollectionSettings));
            menu.Items.Add(new ToolStripSeparator());var item=(ToolStripMenuItem)menu.Items.Add("Ungeprüfte Bedingungen anzeigen",null,(s,e)=>SetUnresolvedPreview(!project.Editor.ShowUnresolved));item.Checked=project.Editor.ShowUnresolved;
        }
    }
}
