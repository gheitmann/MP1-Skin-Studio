using System;
using System.Globalization;

namespace MP1SkinStudio
{
    public static class IdlePreview
    {
        public const string Key="#preview.system.idleseconds";
        public static bool TrySeconds(string text,out int seconds)
        {return Int32.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out seconds)&&seconds>=0&&seconds<=86400;}
    }
    public sealed partial class StudioForm
    {
        void ApplyIdlePreview(string seconds)
        {
            RequireAppliedProperties();var preview=project.Editor.Preview.Copy();
            if(seconds==null)preview.Values.Remove(IdlePreview.Key);else preview.Values[IdlePreview.Key]=seconds;
            ApplyPreviewSettings(preview);ShowNotice(seconds==null?"Idle-time sample removed; idle conditions remain unverified.":"Idle-time sample: "+seconds+" seconds. Save the editor project; real activity tracking is unchanged.");
        }
    }
}
