using System;
using System.Globalization;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class PreviewState
    {
        public static readonly string[] FacadeFocusKeys={"#facadeview.focus.X","#facadeview.focus.Y","#facadeview.focus.Width","#facadeview.focus.Height"};
        void ResetFacadeFocusValues()
        {foreach(string key in FacadeFocusKeys){string value;if(Project.Editor.Preview.Values.TryGetValue(key,out value))Values[key]=value;else Values.Remove(key);}}
        void UpdateFacadeFocusValues()
        {
            // Static preview thumbnail bounds; animation/zoom/scroll offsets are not modeled.
            var facades=Project.LogicalControls().Where(c=>SkinProject.ControlId(c)==50).ToArray();if(facades.Length!=1||facades[0].Get("type")!="facadeview"||FocusId!=50)return;
            var control=CollectionSettings.Template(Project,facades[0],Project.Editor.Collection.Mode);if(control==null||control.Get("type")!="filmstrip")return;
            if(Visible(control)!=true||Project.IsEditorHidden(control))return;
            if(Project.Editor.Collection.SelectedIndex<0){foreach(string key in FacadeFocusKeys)if(!Project.Editor.Preview.Values.ContainsKey(key))Values[key]="";return;}
            for(var current=control;current!=null;current=current.RuntimeParent)
            {
                if(current.Get("layout").Trim().Length>0)return;
                foreach(string key in new[]{"posX","posY","width","height"}){PropertyValue value;if(current.Properties.TryGetValue(key,out value)&&(value.Expression.Length>0||value.Value.Contains("#")))return;}
            }
            var bounds=new StackPreview(Project,null).Bounds(control);var tile=CollectionGeometry.Tiles(control,bounds,Project.Editor.Collection).FirstOrDefault(t=>t.Selected);if(tile==null||!CollectionGeometry.ValidBounds(tile.Image))return;
            float[] coordinates={tile.Image.X,tile.Image.Y,tile.Image.Width,tile.Image.Height};
            for(int i=0;i<coordinates.Length;i++){if(coordinates[i]<-100000||coordinates[i]>100000)return;}
            for(int i=0;i<FacadeFocusKeys.Length;i++)if(!Project.Editor.Preview.Values.ContainsKey(FacadeFocusKeys[i]))Values[FacadeFocusKeys[i]]=((int)coordinates[i]).ToString(CultureInfo.InvariantCulture);
        }
        public bool TryImagePosition(SkinControl control,string key,out float value)
        {
            value=0;PropertyValue property;string text;int integer;
            if(control.Get("type")!="image"||(key!="posX"&&key!="posY")||!control.Properties.TryGetValue(key,out property)||property.Expression.Length==0||property.Expression.IndexOf('#')<0)return false;
            if(!TryText(control,property.Expression,out text)||!Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out integer)||integer<0||integer>100000)return false;
            value=integer;return true;
        }
    }
    public static class VideoPinPreview
    {
        public const string Key="#MyVideos.PinLocked";
        public static void Set(PreviewSettings settings,bool? locked)
        {var copy=settings.Copy();if(locked.HasValue)copy.Values[Key]=locked.Value?"True":"False";else copy.Values.Remove(Key);copy.Validate();settings.SetFrom(copy);}
    }
    public sealed partial class StudioForm
    {
        void ApplyVideoPinPreview(bool? locked)
        {RequireAppliedProperties();var preview=project.Editor.Preview.Copy();VideoPinPreview.Set(preview,locked);ApplyPreviewSettings(preview);ShowNotice("Video PIN preview applied. This changes only the simulated locked/unlocked state; real media access is untouched. Save the editor project.");}
    }
}
