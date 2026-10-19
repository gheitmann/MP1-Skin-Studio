using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MP1SkinStudio
{
    // Explicit replacements for image texture expressions, never skin XML edits.
    public static class ArtworkPreview
    {
        static readonly string[] extensions={".png",".jpg",".jpeg",".bmp",".gif"};
        public static void Validate(IDictionary<string,string> samples)
        {
            if(samples.Count>128)throw new InvalidDataException("Save at most 128 artwork samples.");
            foreach(var pair in samples)
            {
                if(String.IsNullOrWhiteSpace(pair.Key)||pair.Key.Length>2048||pair.Key!=pair.Key.Trim()||pair.Key=="-")throw new InvalidDataException("Artwork sources require a nonempty image texture expression of at most 2048 characters.");
                XmlConvert.VerifyXmlChars(pair.Key);string value=pair.Value;
                if(value==null||value.Length>512)throw new InvalidDataException("Artwork samples require a relative Media image path or an empty value.");
                if(value.Length==0)continue;
                var parts=value.Replace('\\','/').Split('/');
                if(value!=value.Trim()||value.Any(Char.IsControl)||value.IndexOfAny(new[]{':','#','?','*','"','<','>','|'})>=0||Path.IsPathRooted(value)||parts.Any(p=>p.Length==0||p=="."||p==".."||p.EndsWith(".")||p.EndsWith(" "))||!extensions.Contains(Path.GetExtension(value).ToLowerInvariant()))throw new InvalidDataException("Choose a PNG, JPG, BMP or GIF below the skin Media directory; absolute paths, bindings and parent paths cannot be samples.");
                XmlConvert.VerifyXmlChars(value);
            }
        }
        public static void Set(PreviewSettings settings,string source,string sample)
        {
            var copy=settings.Copy();source=(source??"").Trim();
            if(sample==null)copy.Artwork.Remove(source);else copy.Artwork[source]=sample.Replace('\\','/');
            copy.Validate();settings.SetFrom(copy);
        }
        public static string[] Sources(SkinProject project)
        {return project.Controls.Where(c=>c.Get("type")=="image").Select(c=>c.Get("texture").Trim()).Concat(project.Editor.Preview.Artwork.Keys).Where(s=>s.Length>0&&s!="-").Distinct(StringComparer.Ordinal).OrderBy(s=>s,StringComparer.Ordinal).ToArray();}
    }
    public sealed partial class PreviewState
    {
        public string ImageTexture(SkinControl control)
        {
            string original=control.Get("texture"),sample;
            return control.Get("type")=="image"&&Project.Editor.Preview.Artwork.TryGetValue(original.Trim(),out sample)?sample:original;
        }
        public bool? ArtworkAvailable(SkinControl control){return KnownControlThumb(control);}
    }
}
