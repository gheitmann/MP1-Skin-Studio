using System;
using System.Globalization;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class CollectionProperty
    {
        public readonly string Key,Label;
        readonly string[] types;
        readonly bool positive;
        public CollectionProperty(string key,string label,bool positive,params string[] types)
        {Key=key;Label=label;this.positive=positive;this.types=types;}
        public bool AppliesTo(SkinControl control){return types.Contains(control.Get("type"));}
        public void Validate(string text)
        {
            int value;
            if(!Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out value)||(positive?value<=0:value<0))
                throw new InvalidOperationException("For "+Label+" enter "+(positive?"a positive":"a nonnegative")+" integer.");
        }
    }
    // Shared by the property panel and console checks; facade wrappers have no row geometry.
    public static class CollectionProperties
    {
        public static readonly CollectionProperty[] Fields={
            new CollectionProperty("textureHeight","Row / small texture height",true,"listcontrol","playlistcontrol","filmstrip","thumbnailpanel"),
            new CollectionProperty("textureWidth","Small texture width",true,"filmstrip","thumbnailpanel"),
            new CollectionProperty("spaceBetweenItems","Space between rows",false,"listcontrol","playlistcontrol"),
            new CollectionProperty("itemWidth","Item / icon width",true,"listcontrol","playlistcontrol","filmstrip","thumbnailpanel"),
            new CollectionProperty("itemHeight","Item / icon height",true,"listcontrol","playlistcontrol","filmstrip","thumbnailpanel"),
            new CollectionProperty("thumbWidth","Thumbnail width",true,"filmstrip","thumbnailpanel"),
            new CollectionProperty("thumbHeight","Thumbnail height",true,"filmstrip","thumbnailpanel"),
            new CollectionProperty("itemWidthBig","Large item width",true,"thumbnailpanel"),
            new CollectionProperty("itemHeightBig","Large item height",true,"thumbnailpanel"),
            new CollectionProperty("thumbWidthBig","Large thumbnail width",true,"thumbnailpanel"),
            new CollectionProperty("thumbHeightBig","Large thumbnail height",true,"thumbnailpanel"),
            new CollectionProperty("textureWidthBig","Large texture width",true,"thumbnailpanel"),
            new CollectionProperty("textureHeightBig","Large texture height",true,"thumbnailpanel")
        };
        public static CollectionProperty Find(string key){return Fields.FirstOrDefault(f=>f.Key==key);}
    }
}
