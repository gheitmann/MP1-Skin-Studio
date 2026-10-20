using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        CheckBox gridToggle,snapToggle;ChoiceBox snapSpacing=new ChoiceBox();bool syncingGrid;
        void AddGridTools(FlowLayoutPanel bar)
        {
            gridToggle=Check("Grid",true,v=>{if(syncingGrid||project==null)return;canvas.Grid=v;UpdateProjectCaption();});bar.Controls.Add(gridToggle);
            snapToggle=Check("Snap",true,v=>{if(syncingGrid||project==null)return;canvas.Snap=v;UpdateProjectCaption();});bar.Controls.Add(snapToggle);
            snapSpacing.Width=82;snapSpacing.Height=26;snapSpacing.Margin=new Padding(0,3,8,0);snapSpacing.AccessibleName="Snap spacing in skin pixels";
            snapSpacing.Items.AddRange(new object[]{1,2,4,5,8,10,16,20,25,50,100,200});snapSpacing.SelectedIndex=5;
            snapSpacing.SelectedIndexChanged+=(s,e)=>{if(syncingGrid||project==null||snapSpacing.SelectedItem==null)return;project.Editor.SnapSpacing=(int)snapSpacing.SelectedItem;canvas.Invalidate();UpdateProjectCaption();};
            bar.Controls.Add(snapSpacing);tips.SetToolTip(snapSpacing,"Grid and snapping distance in skin pixels. At small zoom levels, the grid shows a readable multiple of this distance. Saved in the editor project.");
        }
        void SyncGridTools()
        {
            if(project==null||gridToggle==null)return;syncingGrid=true;
            try{gridToggle.Checked=project.Editor.GridVisible;snapToggle.Checked=project.Editor.SnapEnabled;if(!snapSpacing.Items.Contains(project.Editor.SnapSpacing))snapSpacing.Items.Add(project.Editor.SnapSpacing);snapSpacing.SelectedIndex=snapSpacing.Items.IndexOf(project.Editor.SnapSpacing);}
            finally{syncingGrid=false;}
        }
    }
}
