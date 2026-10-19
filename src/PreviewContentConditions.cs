using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MP1SkinStudio
{
    public sealed partial class PreviewState
    {
        // Model only the static content known to this preview, not plugin/cache state.
        bool? KnownControlText(SkinControl control)
        {
            string type=control.Get("type");
            if(type!="label"&&type!="fadelabel"&&type!="textbox"&&type!="textboxscrollup")return false;
            string raw=control.Get("label"),text,origin;int id;
            if(raw=="-")return false;
            if(TryLabelId(control,raw,out id))
            {
                if(!Project.Editor.Language.TryGet(id,out text,out origin))return null;
                raw=text;
            }
            if(!TryText(control,raw,out text))return null;
            if(text.Length==0)return false;
            // Width/markup details require the native font renderer.
            if(String.IsNullOrWhiteSpace(text)||text.Contains("["))return null;
            return true;
        }
        sealed class ThumbObservation
        {public long Length,Ticks;public bool? Available;}
        readonly Dictionary<string,ThumbObservation> thumbObservations=new Dictionary<string,ThumbObservation>(StringComparer.OrdinalIgnoreCase);
        bool? KnownControlThumb(SkinControl control)
        {
            if(control.Get("type")!="image")return false;
            string texture;if(!TryText(control,control.Get("texture"),out texture))return null;
            texture=texture.Trim();if(texture.Length==0||texture=="-")return false;
            if(texture.Contains("#")||Path.IsPathRooted(texture)||texture.Replace('\\','/').Split('/').Length>64)return null;
            foreach(string part in texture.Replace('\\','/').Split('/'))if(part=="..")return null;
            try
            {
                string path=Project.Resolve("Media/"+texture);var info=new FileInfo(path);
                if(!info.Exists)return false;
                if(info.Length>32L*1024*1024)return null;
                ThumbObservation found;
                if(thumbObservations.TryGetValue(path,out found)&&found.Length==info.Length&&found.Ticks==info.LastWriteTimeUtc.Ticks)return found.Available;
                bool? available;
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                using(var image=Image.FromStream(stream,false,false))
                    available=image.Width>0&&image.Height>0&&image.Width<=8192&&image.Height<=8192&&(long)image.Width*image.Height<=32000000?(bool?)true:null;
                if(thumbObservations.Count>=512)thumbObservations.Clear();
                thumbObservations[path]=new ThumbObservation{Length=info.Length,Ticks=info.LastWriteTimeUtc.Ticks,Available=available};return available;
            }
            catch{return null;}
        }
    }
}
