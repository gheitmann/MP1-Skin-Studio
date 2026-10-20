using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    // Editor presentation only. Neither source values nor runtime conditions are changed.
    public static class ReadablePreview
    {
        public static readonly string[] Modes={"layout","all"};
        public static readonly string[] Scenes={"music","video","pictures","folder","radio"};
        public static void Validate(EditorState editor)
        {if(!Modes.Contains(editor.PreviewMode)||!Scenes.Contains(editor.MediaTipScene))throw new InvalidDataException("Invalid readable preview mode or MediaTip example.");}
        public static bool SupportsMediaTip(SkinProject project)
        {
            if(project==null||project.WindowId!=7979||!String.Equals(Path.GetFileName(project.Page),"MediaTipOverlay.xml",StringComparison.OrdinalIgnoreCase))return false;
            var ids=project.LogicalControls().Where(c=>SkinProject.ControlId(c)>0).GroupBy(c=>SkinProject.ControlId(c).Value).ToDictionary(g=>g.Key,g=>g.ToArray());
            SkinControl[] headline;if(!ids.TryGetValue(10,out headline)||headline.Length!=1||headline[0].Get("type")!="label")return false;
            foreach(int start in new[]{20,30,40,50,60})for(int line=0;line<5;line++)
            {SkinControl[] controls;if(!ids.TryGetValue(start+line,out controls)||controls.Length!=1||!new[]{"label","fadelabel"}.Contains(controls[0].Get("type")))return false;}
            return true;
        }
        static int Group(SkinControl c)
        {
            int id=SkinProject.ControlId(c)??-1;
            if(id>=20&&id<=66&&id%10<=6)return id/10-2;
            if(id==0&&new[]{"AudioCodec","AudioType","VideoCodec"}.Contains(c.Get("description")))return 1;
            return -1;
        }
        public static bool InScene(SkinProject project,SkinControl control)
        {if(!SupportsMediaTip(project))return true;int group=Group(control);return group<0||Scenes[group]==project.Editor.MediaTipScene;}
        public static bool InSelection(SkinControl control,SkinControl selected)
        {
            if(selected==null)return false;if(control.Key==selected.Key)return true;
            for(var parent=control.RuntimeParent;parent!=null;parent=parent.RuntimeParent)if(parent.Key==selected.Key)return true;
            return control.Source==selected.Source&&control.Node.Start>selected.Node.Start&&control.Node.End<selected.Node.End;
        }
        static string Sample(string name,string scene)
        {
            switch(name.ToLowerInvariant())
            {
                case "headline":return scene=="music"?"Music information":scene=="video"?"Video information":scene=="pictures"?"Photo information":scene=="folder"?"Folder information":"Radio information";
                case "filename":return scene=="music"?"Example song.flac":scene=="video"?"Example film.mkv":"Holiday photo.jpg";
                case "filesize":return scene=="music"?"8 MB":scene=="video"?"1.4 GB":"4 MB";
                case "descriptiontrack":return "Track";case "track":return "03";case "descriptiontitle":return "Title";
                case "title":return scene=="music"?"A quiet evening":scene=="video"?"Example film":"Morning programme";
                case "descriptionalbum":return "Album";case "album":return "Evening sessions";
                case "descriptionartist":return "Artist";case "artist":return "Example artist";
                case "descriptioncomposer":return "Composer";case "composer":return "Example composer";
                case "descriptionyear":return "Year";case "year":return "2026";case "descriptiongenre":return "Genre";case "genre":return "Ambient";
                case "audiocodec":return scene=="music"?"FLAC":"AAC";case "audiotype":return "Stereo";
                case "audiobitrate":return scene=="music"?"900 kb/s":"192 kb/s";case "audiosamplingrate":return "48 kHz";
                case "videocodec":return "H.264";case "videobitrate":return "8 Mb/s";case "framerate":return "25 fps";case "resolution":return "1920 x 1080";
                case "descriptiongeometry":return "Format";case "geometry":return "16:9";case "dimension":return scene=="pictures"?"4000 x 3000":"1920 x 1080";case "aspectratio":return "16:9";
                case "date":return "20 October 2026";case "descriptiondimension":return "Dimensions";case "descriptionorientation":return "Orientation";case "orientation":return "Landscape";
                case "descriptioncameramodel":return "Camera";case "cameramodel":return "Example camera";case "cameraequipment":return "35 mm lens";
                case "descriptionfstop":return "Aperture";case "fstop":return "f/4";case "descriptionshutterspeed":return "Shutter";case "shutterspeed":return "1/125 s";
                case "descriptionmeteringmode":return "Metering";case "meteringmode":return "Pattern";case "descriptionflashmode":return "Flash";case "flashmode":return "Off";
                case "descriptionexposuretime":return "Exposure";case "exposuretime":return "1/125 s";case "descriptionexposurecompensation":return "Compensation";case "exposurecompensation":return "0 EV";
                case "directoryname":return "Holiday photos";case "descriptiondirectorysize":return "Folder size";case "directorysize":return "240 MB";
                case "descriptiondirectoryfilecount":return "Files";case "directoryfilecount":return "60";case "station":return "Example radio";
                default:return null;
            }
        }
        public static string Caption(PreviewState state,SkinControl control,bool allLayers=false)
        {
            string raw=control.Get("label"),text=state.Label(control,raw);
            if(allLayers||state.ResolvedLabel(control,raw)||raw=="-"||raw.Length==0)return text;
            if(SupportsMediaTip(state.Project))
            {
                int group=Group(control);string scene=group<0?state.Project.Editor.MediaTipScene:Scenes[group];
                text=Regex.Replace(text,@"#MediaTip\.([A-Za-z][A-Za-z0-9]*)",m=>Sample(m.Groups[1].Value,scene)??m.Value,RegexOptions.IgnoreCase);
                if(!text.Contains("#"))return text;
            }
            string name=control.Get("description").Trim();
            if(name.Length==0||name.Contains("#"))return "Example text";
            return (name.Length>60?name.Substring(0,60):name)+" (example)";
        }
    }
    public sealed partial class SkinCanvas
    {public bool IsolateSelection;}
}
