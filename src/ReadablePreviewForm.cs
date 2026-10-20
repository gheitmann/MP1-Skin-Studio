using System;
using System.Drawing;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        ChoiceBox previewMode,mediaTipScene;Label previewModeHint,sceneLabel;bool updatingPreviewMode;
        Control BuildPreviewModeBar()
        {
            var panel=new Panel{Dock=DockStyle.Top,Height=59};
            var row=new FlowLayoutPanel{Dock=DockStyle.Top,Height=34,WrapContents=false,Padding=new Padding(8,2,0,0)};
            row.Controls.Add(new Label{Text="View",ForeColor=muted,Width=38,Height=27,TextAlign=ContentAlignment.MiddleLeft});
            previewMode=new ChoiceBox{Width=164,Height=26,AccessibleName="Preview editing view"};
            previewMode.Items.AddRange(new object[]{"Edit layout","Isolate selection","All layers"});row.Controls.Add(previewMode);
            sceneLabel=new Label{Text="Example",ForeColor=muted,Width=60,Height=27,TextAlign=ContentAlignment.MiddleLeft};row.Controls.Add(sceneLabel);
            mediaTipScene=new ChoiceBox{Width=127,Height=26,AccessibleName="MediaTip example variant"};
            mediaTipScene.Items.AddRange(new object[]{"Music","Video","Pictures","Folder","Radio"});row.Controls.Add(mediaTipScene);
            previewModeHint=new Label{Dock=DockStyle.Bottom,Height=23,Padding=new Padding(10,0,4,0),ForeColor=muted,AutoEllipsis=true};
            panel.Controls.Add(row);panel.Controls.Add(previewModeHint);
            previewMode.SelectedIndexChanged+=(s,e)=>
            {
                if(updatingPreviewMode||project==null||previewMode.SelectedIndex<0)return;
                canvas.IsolateSelection=previewMode.SelectedIndex==1;project.Editor.PreviewMode=previewMode.SelectedIndex==2?"all":"layout";project.Editor.ShowUnresolved=previewMode.SelectedIndex==2;
                UpdatePreviewModeTools();canvas.Invalidate();UpdateProjectCaption();
            };
            mediaTipScene.SelectedIndexChanged+=(s,e)=>
            {
                if(updatingPreviewMode||project==null||mediaTipScene.SelectedIndex<0)return;
                project.Editor.MediaTipScene=ReadablePreview.Scenes[mediaTipScene.SelectedIndex];UpdatePreviewModeTools();canvas.Invalidate();UpdateProjectCaption();
            };
            tips.SetToolTip(previewMode,"Edit layout hides unresolved visibility and shortens unresolved captions. Isolate shows only the selected element/group. All layers is a diagnostic view. XML and pending inputs are unchanged.");
            tips.SetToolTip(mediaTipScene,"Authored MediaTip example only, not live plugin state. Examples and view mode are saved in the editor project. Isolate selection is temporary.");
            return panel;
        }
        void UpdatePreviewModeTools()
        {
            if(previewMode==null||project==null)return;
            updatingPreviewMode=true;
            try
            {
                previewMode.SelectedIndex=canvas.IsolateSelection?1:project.Editor.PreviewMode=="all"?2:0;
                bool supported=ReadablePreview.SupportsMediaTip(project);sceneLabel.Visible=mediaTipScene.Visible=supported;
                mediaTipScene.SelectedIndex=Array.IndexOf(ReadablePreview.Scenes,project.Editor.MediaTipScene);
                mediaTipScene.Enabled=supported&&!canvas.IsolateSelection&&project.Editor.PreviewMode!="all";
                string hint=canvas.IsolateSelection?"Selected element/group only. Choose another element in the tree. Isolation is temporary.":project.Editor.PreviewMode=="all"?"Diagnostic view: authored layers and raw unresolved bindings may overlap.":supported?"MediaTip example: one variant with sample text. Not live plugin state.":"Editing example: known visibility and short captions. All layers reveals unresolved content.";
                if(!canvas.IsolateSelection&&project.Editor.PreviewMode!="all"&&selected!=null&&supported&&!ReadablePreview.InScene(project,selected))hint="Selection belongs to another variant. Change Example or use Isolate selection.";
                previewModeHint.Text=hint;tips.SetToolTip(previewModeHint,hint);
            }
            finally{updatingPreviewMode=false;}
        }
    }
}
