using System;
using System.Linq;

namespace MP1SkinStudio
{
    // Explicit sample state, never an inference about missing runtime properties.
    public static class VideoFolderPreview
    {
        public static void Apply(SkinProject project)
        {MediaFolderPreview.Apply(project,"video");}
    }
    public static class MediaFolderPreview
    {
        public static void Apply(SkinProject project,string kind)
        {
            if(!new[]{"video","music","pictures"}.Contains(kind))throw new InvalidOperationException("Unknown media folder sample.");
            var list=project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol"&&SkinProject.ControlId(c)>0)??project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol");
            if(list==null)
                throw new InvalidOperationException("Open a page with a list control before applying a media folder preview.");
            var preview=project.Editor.Preview.Copy();
            foreach(string key in new[]{"#title","#plot","#genre","#director","#year","#runtime","#imdbnumber","#rating","#iscollection","#isgroup","#Is3D","#IsHDR"})preview.Values[key]="";
            foreach(string key in new[]{"#itemcount","#selecteditem","#selecteditem2","#selectedindex","#selectedthumb"})preview.Values.Remove(key);
            string title=kind=="video"?"Videos":kind=="music"?"Music":"Pictures",image=kind=="video"?"DefaultFolderBig.png":kind=="music"?"DefaultFolderBigMusic.png":"defaultFolderBigPictures.png";
            preview.Values["#currentmodule"]=title;
            preview.Values["#hideinfo"]="False";preview.Values["#Titan.HiddenMenu"]="false";preview.Values[IdlePreview.Key]="0";
            if(kind=="video")
            {
                preview.Values["#watchedcount"]="0";preview.Values["#watchedpercent"]="0";
                preview.Values["#VideoFolderChanged"]="false";preview.Values["#importer.hasuncomplete"]="false";
                preview.Values["#fanarthandler.movie.animated.selected.thumb"]="";
            }
            if(kind=="music")preview.Values["#MusicFolderChanged"]="false";
            if(kind=="pictures")foreach(string key in new[]{"#PicturesFolderChanged","#pictures.IsVideo","#pictures.IsHDR","#pictures.Favorite","#pictures.exif.ishdr"})preview.Values[key]="false";
            preview.Values["#scrolling.up"]="no";preview.Values["#scrolling.down"]="no";
            PlaybackPreview.Stopped(preview);
            var collection=new CollectionSettings{Mode="list",Focus=0};
            collection.Items.Clear();collection.Items.Add(new PreviewItem{Title=title,Image=image});
            // Validate both copies before replacing either active state.
            preview.Validate();collection.Validate();
            if(SkinProject.ControlId(list)>0)project.Editor.ValidateFocus(project.Page,project.Theme,SkinProject.ControlId(list).Value);
            project.Editor.Preview.SetFrom(preview);project.Editor.Collection.SetFrom(collection);
            if(SkinProject.ControlId(list)>0)project.Editor.RememberFocus(project.Page,project.Theme,SkinProject.ControlId(list).Value);project.Editor.ShowUnresolved=false;
        }
    }
    public sealed partial class StudioForm
    {
        void ApplyVideoFolderPreview()
        {ApplyMediaFolderPreview("video");}
        void ApplyMediaFolderPreview(string kind)
        {
            RequireAppliedProperties();MediaFolderPreview.Apply(project,kind);
            var list=project.Controls.FirstOrDefault(c=>c.Get("type")=="listcontrol"&&SkinProject.ControlId(c)>0)??project.Controls.First(c=>c.Get("type")=="listcontrol");
            ApplyPreviewSettings(project.Editor.Preview);SelectControl(list);
            SetUnresolvedPreview(false);
            ShowNotice("Media folder sample applied: "+project.Editor.Collection.Items[0].Title+", information shown, hidden menu option off, active input and stopped playback. Other runtime values remain unverified. Save the editor project.");
        }
    }
}
