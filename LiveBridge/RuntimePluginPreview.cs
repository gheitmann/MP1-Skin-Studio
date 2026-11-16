using System;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;

namespace MP1SkinStudio.LiveBridge
{
    static class RuntimePluginPreview
    {
        const int MaxItems = 50;

        internal static GUIFacadeControl FindList(GUIWindow window)
        {
            if (window.GetID != 34 ||
                !String.Equals(Path.GetFileName(window.WindowXmlFileName), "myHomePlugIns.xml", StringComparison.OrdinalIgnoreCase))
                return null;
            return window.GetControl(50) as GUIFacadeControl;
        }

        internal static void Read(XmlElement result, GUIWindow window, GUIFacadeControl list, string skin)
        {
            int count = list.Count;
            int selected = list.SelectedListItemIndex;
            int start = Math.Max(0, selected - MaxItems + 1);
            LiveWire.Attr(result, "count", count);
            LiveWire.Attr(result, "selection", Math.Max(0, selected - start));
            LiveWire.Attr(result, "focus", window.GetFocusControlId());
            foreach (var name in new[] { "pluginname", "pluginauthor", "plugindescription", "time", "date" })
                LiveWire.Attr(result, name, Text(GUIPropertyManager.GetProperty("#" + name), 2048));
            LiveWire.Attr(result, "writer", Text(GUILocalizeStrings.Get(200), 2048));
            LiveWire.Attr(result, "artwork", Image(skin, GUIPropertyManager.GetProperty("#selectedthumb")));

            for (int i = start; i < count && i - start < MaxItems; i++)
            {
                var native = list[i];
                var item = LiveWire.Add(result, "item");
                LiveWire.Attr(item, "title", Text(native.Label, 512, true));
                LiveWire.Attr(item, "detail", Text(native.Label2, 256, true));
                LiveWire.Attr(item, "image", Image(skin, native.ThumbnailImage));
                LiveWire.Attr(item, "marked", native.Selected ? "true" : "false");
            }
        }

        static string Text(string text, int limit, bool singleLine = false)
        {
            if (String.IsNullOrEmpty(text)) return "";
            var result = new StringBuilder();
            for (int i = 0; i < text.Length && result.Length < limit; i++)
            {
                char c = text[i];
                if (singleLine && (c == '|' || Char.IsControl(c))) result.Append(' ');
                else if (XmlConvert.IsXmlChar(c)) result.Append(c);
                else if (i + 1 < text.Length && result.Length + 2 <= limit && Char.IsSurrogatePair(text, i))
                    result.Append(c).Append(text[++i]);
            }
            return result.ToString();
        }

        static string Image(string skin, string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 500) return "";
            string prefix = Path.Combine(skin, "Media") + Path.DirectorySeparatorChar;
            try
            {
                string path = value.Replace('/', '\\');
                if (Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(path);
                    if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return "";
                    path = path.Substring(prefix.Length);
                }
                else if (path.StartsWith("Media\\", StringComparison.OrdinalIgnoreCase)) path = path.Substring(6);
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp" && extension != ".gif")
                    return "";
                return File.Exists(LiveWire.SafeRelative(prefix, path)) ? path.Replace('\\', '/') : "";
            }
            catch (ArgumentException) { return ""; }
            catch (InvalidDataException) { return ""; }
            catch (NotSupportedException) { return ""; }
        }
    }
}
