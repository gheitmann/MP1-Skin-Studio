using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class PreviewSettings
    {
        public readonly Dictionary<string,string> Values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Plugins=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string,string> Artwork=new Dictionary<string,string>(StringComparer.Ordinal);
        public static Dictionary<string,string> BaseValues()
        {return new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"#time","18:42"},{"#date","Saturday, October 17"},{"#header.label","My MediaPortal"},{"#WorldWeather.TodayTemperature","16 °C"},{"#myNotifier.Messages.Count.Unread","0"},{"#selecteditem","A sample with a long movie title"},{"#title","A sample with a long movie title"}};}
        public void Validate()
        {
            ArtworkPreview.Validate(Artwork);
            if(Values.Count>128||Plugins.Count>128)throw new InvalidDataException("Save at most 128 custom sample values and 128 plugin names.");
            foreach(var pair in Values)
            {
                if(pair.Key.Length>160||!Regex.IsMatch(pair.Key,@"^#[A-Za-z_][A-Za-z0-9_.]*$"))throw new InvalidDataException("Preview variable names must start with # and use letters, digits, periods or underscores.");
                if(pair.Value==null||pair.Value.Length>2048)throw new InvalidDataException("Sample values may contain at most 2048 characters.");XmlConvert.VerifyXmlChars(pair.Value);
                bool playback;if(PlaybackPreview.IsKey(pair.Key)&&!Boolean.TryParse(pair.Value,out playback))throw new InvalidDataException("Playback samples must be true or false: "+pair.Key);
                int idle;if(pair.Key.Equals(IdlePreview.Key,StringComparison.OrdinalIgnoreCase)&&!IdlePreview.TrySeconds(pair.Value,out idle))throw new InvalidDataException("Idle-time samples require whole seconds from 0 to 86400.");
            }
            foreach(string plugin in Plugins){if(String.IsNullOrWhiteSpace(plugin)||plugin!=plugin.Trim()||plugin.Length>160||plugin.Any(Char.IsControl))throw new InvalidDataException("Enter plugin names without leading spaces or control characters and with at most 160 characters.");XmlConvert.VerifyXmlChars(plugin);}
        }
        public PreviewSettings Copy()
        {Validate();var copy=new PreviewSettings();foreach(var pair in Values)copy.Values.Add(pair.Key,pair.Value);foreach(string plugin in Plugins)copy.Plugins.Add(plugin);foreach(var pair in Artwork)copy.Artwork.Add(pair.Key,pair.Value);return copy;}
        public void SetFrom(PreviewSettings other)
        {var copy=other.Copy();Values.Clear();Plugins.Clear();Artwork.Clear();foreach(var pair in copy.Values)Values.Add(pair.Key,pair.Value);foreach(string plugin in copy.Plugins)Plugins.Add(plugin);foreach(var pair in copy.Artwork)Artwork.Add(pair.Key,pair.Value);}
        public string SignatureText()
        {
            var text=new StringBuilder();foreach(var pair in Values.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase))text.Append('K').Append(pair.Key.Length).Append(':').Append(pair.Key).Append('V').Append(pair.Value.Length).Append(':').Append(pair.Value);
            foreach(string plugin in Plugins.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))text.Append('P').Append(plugin.Length).Append(':').Append(plugin);
            foreach(var pair in Artwork.OrderBy(p=>p.Key,StringComparer.Ordinal))text.Append('A').Append(pair.Key.Length).Append(':').Append(pair.Key).Append('I').Append(pair.Value.Length).Append(':').Append(pair.Value);return text.ToString();
        }
        public static PreviewSettings Preset(string name)
        {
            var result=new PreviewSettings();
            if(name=="Long title"){result.Values["#title"]="An unusually long movie title for testing narrow text fields and truncated labels";result.Values["#selecteditem"]=result.Values["#title"];}
            else if(name=="Weather"){result.Plugins.Add("World Weather");result.Values["#WorldWeather.TodayTemperature"]="−4 °C";}
            else if(name=="News")result.Values["#myNotifier.Messages.Count.Unread"]="12";
            else if(name!="Baseline data")throw new InvalidOperationException("Unknown preview preset.");
            return result;
        }
        public static string[] KnownPlugins(SkinProject project)
        {
            var result=new HashSet<string>(project.Editor.Preview.Plugins,StringComparer.OrdinalIgnoreCase);
            foreach(var control in project.Controls)foreach(string expression in new[]{control.Get("visible"),control.IncludeCondition})
                foreach(Match match in Regex.Matches(expression??"",@"plugin\.isenabled\s*\(\s*([^)]*)\)",RegexOptions.IgnoreCase))
                {string name=match.Groups[1].Value.Trim().Trim('\'', '"');if(name.Length>0)result.Add(name);}
            return result.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
    public static partial class ProjectFile
    {
        static void SavePreviewSettings(PreviewSettings settings,XmlElement state)
        {
            settings.Validate();var doc=state.OwnerDocument;var preview=doc.CreateElement("preview");state.AppendChild(preview);
            foreach(var pair in settings.Values.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase))
            {var item=doc.CreateElement("value");item.SetAttribute("name",pair.Key);item.SetAttribute("encoding","base64");item.InnerText=Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Value));preview.AppendChild(item);}
            foreach(string plugin in settings.Plugins.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))
            {var item=doc.CreateElement("plugin");item.SetAttribute("name",plugin);preview.AppendChild(item);}
            foreach(var pair in settings.Artwork.OrderBy(p=>p.Key,StringComparer.Ordinal))
            {var item=doc.CreateElement("artwork");item.SetAttribute("source",Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Key)));item.SetAttribute("encoding","base64");item.InnerText=Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Value));preview.AppendChild(item);}
        }
        static void LoadPreviewSettings(PreviewSettings settings,XmlElement state)
        {
            var containers=state.SelectNodes("preview");if(containers.Count==0)return;if(containers.Count>1)throw new InvalidDataException("Duplicate preview state in project.");
            var loaded=new PreviewSettings();var values=containers[0].SelectNodes("value");var plugins=containers[0].SelectNodes("plugin");if(values.Count>128||plugins.Count>128)throw new InvalidDataException("Too many preview entries.");
            foreach(XmlElement item in values)
            {
                string key=item.GetAttribute("name"),encoding=item.GetAttribute("encoding");if(loaded.Values.ContainsKey(key))throw new InvalidDataException("Duplicate sample value.");
                string value;if(encoding=="base64")value=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(item.InnerText));
                else if(encoding.Length==0&&item.HasAttribute("text"))value=item.GetAttribute("text");
                else throw new InvalidDataException("Unknown encoding or incomplete sample value.");loaded.Values.Add(key,value);
            }
            foreach(XmlElement item in plugins)if(!loaded.Plugins.Add(item.GetAttribute("name")))throw new InvalidDataException("Duplicate plugin name.");
            var artwork=containers[0].SelectNodes("artwork");if(artwork.Count>128)throw new InvalidDataException("Too many artwork samples.");
            foreach(XmlElement item in artwork)
            {
                if(item.GetAttribute("encoding")!="base64")throw new InvalidDataException("Unknown artwork sample encoding.");
                var utf8=new UTF8Encoding(false,true);string source=utf8.GetString(Convert.FromBase64String(item.GetAttribute("source"))),sample=utf8.GetString(Convert.FromBase64String(item.InnerText));
                if(loaded.Artwork.ContainsKey(source))throw new InvalidDataException("Duplicate artwork sample.");loaded.Artwork.Add(source,sample);
            }
            settings.SetFrom(loaded);
        }
    }
}
