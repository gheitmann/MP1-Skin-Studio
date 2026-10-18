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
        public static Dictionary<string,string> BaseValues()
        {return new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"#time","18:42"},{"#date","Samstag, 17. Oktober"},{"#header.label","Mein MediaPortal"},{"#WorldWeather.TodayTemperature","16 °C"},{"#myNotifier.Messages.Count.Unread","0"},{"#selecteditem","Ein Beispiel mit einem langen Filmtitel"},{"#title","Ein Beispiel mit einem langen Filmtitel"}};}
        public void Validate()
        {
            if(Values.Count>128||Plugins.Count>128)throw new InvalidDataException("Höchstens 128 eigene Beispielwerte und 128 Plugin-Namen speichern.");
            foreach(var pair in Values)
            {
                if(pair.Key.Length>160||!Regex.IsMatch(pair.Key,@"^#[A-Za-z_][A-Za-z0-9_.]*$"))throw new InvalidDataException("Variablenname für diese Vorschau: #Name, mit Buchstaben, Ziffern, Punkt oder Unterstrich.");
                if(pair.Value==null||pair.Value.Length>2048)throw new InvalidDataException("Ein Beispielwert darf höchstens 2048 Zeichen enthalten.");XmlConvert.VerifyXmlChars(pair.Value);
            }
            foreach(string plugin in Plugins){if(String.IsNullOrWhiteSpace(plugin)||plugin!=plugin.Trim()||plugin.Length>160||plugin.Any(Char.IsControl))throw new InvalidDataException("Plugin-Namen ohne führende Leerzeichen oder Steuerzeichen und mit höchstens 160 Zeichen eingeben.");XmlConvert.VerifyXmlChars(plugin);}
        }
        public PreviewSettings Copy()
        {Validate();var copy=new PreviewSettings();foreach(var pair in Values)copy.Values.Add(pair.Key,pair.Value);foreach(string plugin in Plugins)copy.Plugins.Add(plugin);return copy;}
        public void SetFrom(PreviewSettings other)
        {var copy=other.Copy();Values.Clear();Plugins.Clear();foreach(var pair in copy.Values)Values.Add(pair.Key,pair.Value);foreach(string plugin in copy.Plugins)Plugins.Add(plugin);}
        public string SignatureText()
        {
            var text=new StringBuilder();foreach(var pair in Values.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase))text.Append('K').Append(pair.Key.Length).Append(':').Append(pair.Key).Append('V').Append(pair.Value.Length).Append(':').Append(pair.Value);
            foreach(string plugin in Plugins.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))text.Append('P').Append(plugin.Length).Append(':').Append(plugin);return text.ToString();
        }
        public static PreviewSettings Preset(string name)
        {
            var result=new PreviewSettings();
            if(name=="Langer Titel"){result.Values["#title"]="Ein außergewöhnlich langer Filmtitel für die Prüfung schmaler Textfelder und abgeschnittener Beschriftungen";result.Values["#selecteditem"]=result.Values["#title"];}
            else if(name=="Wetter"){result.Plugins.Add("World Weather");result.Values["#WorldWeather.TodayTemperature"]="−4 °C";}
            else if(name=="Nachrichten")result.Values["#myNotifier.Messages.Count.Unread"]="12";
            else if(name!="Basisdaten")throw new InvalidOperationException("Unbekannte Vorschauvorlage.");
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
        }
        static void LoadPreviewSettings(PreviewSettings settings,XmlElement state)
        {
            var containers=state.SelectNodes("preview");if(containers.Count==0)return;if(containers.Count>1)throw new InvalidDataException("Doppelter Vorschauzustand im Projekt.");
            var loaded=new PreviewSettings();var values=containers[0].SelectNodes("value");var plugins=containers[0].SelectNodes("plugin");if(values.Count>128||plugins.Count>128)throw new InvalidDataException("Zu viele Vorschaueinträge.");
            foreach(XmlElement item in values)
            {
                string key=item.GetAttribute("name"),encoding=item.GetAttribute("encoding");if(loaded.Values.ContainsKey(key))throw new InvalidDataException("Doppelter Beispielwert.");
                string value;if(encoding=="base64")value=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(item.InnerText));
                else if(encoding.Length==0&&item.HasAttribute("text"))value=item.GetAttribute("text");
                else throw new InvalidDataException("Unbekannte Kodierung oder unvollständiger Beispielwert.");loaded.Values.Add(key,value);
            }
            foreach(XmlElement item in plugins)if(!loaded.Plugins.Add(item.GetAttribute("name")))throw new InvalidDataException("Doppelter Plugin-Name.");
            settings.SetFrom(loaded);
        }
    }
}
