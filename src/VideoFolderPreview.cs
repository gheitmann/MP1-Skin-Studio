using System;
using System.Linq;

namespace MP1SkinStudio
{
    // Explicit sample state, never an inference about missing runtime properties.
    public static class VideoFolderPreview
    {
        public static void Apply(SkinProject project)
        {
            var list=project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol"&&SkinProject.ControlId(c)>0)??project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol");
            if(list==null)
                throw new InvalidOperationException("Open a page with a list control before applying the video folder preview.");
            var preview=project.Editor.Preview.Copy();
            foreach(string key in new[]{"#title","#plot","#genre","#director","#year","#runtime","#imdbnumber","#rating","#iscollection","#isgroup","#Is3D","#IsHDR"})preview.Values[key]="";
            foreach(string key in new[]{"#itemcount","#selecteditem","#selecteditem2","#selectedindex"})preview.Values.Remove(key);
            preview.Values["#currentmodule"]="Videos";
            preview.Values["#selectedthumb"]="DefaultFolderBig.png";
            preview.Values["#watchedcount"]="0";preview.Values["#watchedpercent"]="0";
            preview.Values["#scrolling.up"]="no";preview.Values["#scrolling.down"]="no";
            PlaybackPreview.Stopped(preview);
            var collection=new CollectionSettings{Mode="list",Focus=0};
            collection.Items.Clear();collection.Items.Add(new PreviewItem{Title="Videos",Image="DefaultFolderBig.png"});
            // Validate both copies before replacing either active state.
            preview.Validate();collection.Validate();
            project.Editor.Preview.SetFrom(preview);project.Editor.Collection.SetFrom(collection);
            if(SkinProject.ControlId(list)>0){project.Editor.PreviewFocusId=SkinProject.ControlId(list).Value;project.Editor.PreviewFocusPage=project.Page;}project.Editor.ShowUnresolved=false;
        }
    }
    public sealed partial class StudioForm
    {
        void ApplyVideoFolderPreview()
        {
            RequireAppliedProperties();VideoFolderPreview.Apply(project);
            var list=project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol"&&SkinProject.ControlId(c)>0)??project.Controls.First(c=>c.Get("type")=="listcontrol");
            ApplyPreviewSettings(project.Editor.Preview);SelectControl(list);
            SetUnresolvedPreview(false);
            ShowNotice("Video folder sample applied. Other unknown properties remain unresolved; skin XML is unchanged. Save the editor project.");
        }
    }
}
