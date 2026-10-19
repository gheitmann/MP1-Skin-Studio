using System;
using System.Linq;

namespace MP1SkinStudio
{
    // Explicit sample booleans, never a query of the real MediaPortal player.
    public static class PlaybackPreview
    {
        static readonly string[] conditions={"player.hasmedia","player.hasaudio","player.hasvideo","player.playing","player.paused"};
        public static string[] Keys {get{return conditions.Select(c=>"#preview."+c).ToArray();}}
        public static bool Supports(string expression){return conditions.Contains(expression,StringComparer.OrdinalIgnoreCase);}
        public static bool IsKey(string key){return key.StartsWith("#preview.",StringComparison.OrdinalIgnoreCase)&&Supports(key.Substring(9));}
        public static void Stopped(PreviewSettings settings){foreach(string key in Keys)settings.Values[key]="false";}
        public static void Unknown(PreviewSettings settings){foreach(string key in Keys)settings.Values.Remove(key);}
    }
    public sealed partial class StudioForm
    {
        void ApplyPlaybackPreview(bool stopped)
        {
            RequireAppliedProperties();var preview=project.Editor.Preview.Copy();
            if(stopped)PlaybackPreview.Stopped(preview);else PlaybackPreview.Unknown(preview);
            ApplyPreviewSettings(preview);
            ShowNotice(stopped?"Stopped playback sample applied. The real player is unchanged. Save the editor project.":"Playback sample removed. Player conditions remain unverified.");
        }
    }
}
