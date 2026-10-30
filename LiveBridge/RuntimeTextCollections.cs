using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MediaPortal.GUI.Library;

namespace MP1SkinStudio.LiveBridge
{
    public sealed partial class RuntimeProperty
    {
        static bool ScrollingText(Type type){return typeof(GUITextControl).IsAssignableFrom(type)||typeof(GUITextScrollUpControl).IsAssignableFrom(type);}
        static bool Collection(Type type){return typeof(GUIListControl).IsAssignableFrom(type)||typeof(GUIFilmstripControl).IsAssignableFrom(type)||typeof(GUIThumbnailPanel).IsAssignableFrom(type);}
        static bool FontAdapter(Type type){return ScrollingText(type)||Collection(type)||typeof(GUICheckMarkControl).IsAssignableFrom(type);}
        static string ExtendedField(Type type,string key)
        {
            if((ScrollingText(type)||Collection(type))&&key=="textcolor")return "_textColor";
            if(typeof(GUIListControl).IsAssignableFrom(type))
            {switch(key){case "textXOff":return "_textOffsetX";case "textYOff":return "_textOffsetY";case "itemWidth":return "_imageWidth";case "itemHeight":return "_imageHeight";}}
            else if(Collection(type))
            {switch(key){case "itemWidth":return "_itemWidth";case "itemHeight":return "_itemHeight";case "itemWidthBig":return typeof(GUIFilmstripControl).IsAssignableFrom(type)?"_itemBigWidth":"_bigItemWidth";case "itemHeightBig":return typeof(GUIFilmstripControl).IsAssignableFrom(type)?"_itemBigHeight":"_bigItemHeight";}}
            if(typeof(GUIThumbnailPanel).IsAssignableFrom(type))
            {switch(key){case "keepaspectratio":return "_keepAspectRatio";case "textXOff":return "_textXOff";case "textYOff":return "_textYOff";}}
            if(typeof(GUIFilmstripControl).IsAssignableFrom(type))
            {switch(key){case "textureWidth":return "_textureWidth";case "textureHeight":return "_textureHeight";case "thumbWidth":return "_thumbNailWidth";case "thumbHeight":return "_thumbNailHeight";}}
            return null;
        }
        static void AddSupportedExtended(Type type,HashSet<string> fields)
        {
            if(type.Assembly!=typeof(GUIControl).Assembly)return;
            foreach(string key in MP1SkinStudio.Live.LiveWire.Fields){string name=ExtendedField(type,key);if(name!=null&&Field(type,name)!=null)fields.Add(key);}
            if(FontAdapter(type)&&Field(type,"_fontName")!=null&&Field(type,"_font")!=null)fields.Add("font");
            if(ScrollingText(type)&&type.GetProperty("Property")!=null&&type.GetProperty("Label")!=null)fields.Add("label");
        }
        static void MarkCollectionRefresh(GUIControl control)
        {if(!Collection(control.GetType())||control.GetType().Assembly!=typeof(GUIControl).Assembly)return;var refresh=Field(control.GetType(),"_refresh");if(refresh!=null&&refresh.FieldType==typeof(bool))refresh.SetValue(control,true);}
        public static bool NeedsLayout(string key){return key=="font"||key=="posX"||key=="posY"||key=="spaceBetweenItems"||key=="width"||key=="height"||key.ToLowerInvariant().Contains("width")||key.ToLowerInvariant().Contains("height");}
        public static void ReflowLayout(GUIControl control)
        {
            Type type=control.GetType();if(!Collection(type)||type.Assembly!=typeof(GUIControl).Assembly)return;
            var selected=type.GetProperty("SelectedListItemIndex");int selection=selected==null?-1:(int)selected.GetValue(control,null);bool rebuilt=false;
            if(control is GUIListControl)
            {
                // AllocResources rebuilds row visuals; Dispose would also delete list data.
                if(Field(type,"_upDownControl").GetValue(control)!=null&&Field(type,"_verticalScrollbar").GetValue(control)!=null){control.AllocResources();rebuilt=true;}
            }
            else
            {
                bool ready=control is GUIFilmstripControl||Field(type,"_font").GetValue(control)!=null&&Field(type,"_controlUpDown").GetValue(control)!=null;
                if(ready&&(int)Field(type,"_itemWidth").GetValue(control)>0&&(int)Field(type,"_itemHeight").GetValue(control)>0)
                {var calculate=control is GUIFilmstripControl?typeof(GUIFilmstripControl):typeof(GUIThumbnailPanel);var method=calculate.GetMethod("Calculate",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null);if(method!=null){method.Invoke(control,null);rebuilt=true;}}
            }
            var count=type.GetProperty("Count");if(rebuilt&&selection>=0&&selected!=null&&selected.CanWrite&&count!=null&&selection<(int)count.GetValue(control,null))selected.SetValue(control,selection,null);MarkCollectionRefresh(control);
        }
        static void ReflowText(GUIControl control,string text)
        {
            var type=control.GetType();type.GetMethod("Clear",Type.EmptyTypes).Invoke(control,null);type.GetProperty("Label").SetValue(control,text,null);
            var contains=Field(type,"_containsProperty");if(contains!=null)contains.SetValue(control,text.IndexOf('#')>=0);
            var previous=Field(type,"_previousProperty");if(previous!=null)previous.SetValue(control,null);
        }
        sealed class NativeFont {public string Name;public object Font;}
        static void AddExtended(Dictionary<string,RuntimeProperty> slots,GUIControl control)
        {
            Type type=control.GetType();
            if(type.Assembly!=typeof(GUIControl).Assembly)return;
            foreach(string key in MP1SkinStudio.Live.LiveWire.Fields)
            {
                string name=ExtendedField(type,key);var field=name==null?null:Field(type,name);if(field==null)continue;
                // Keep native setters when available, except the list icon/row distinction.
                if(slots.ContainsKey(key)&&!(Collection(type)&&key.StartsWith("item",StringComparison.Ordinal)&&NeedsLayout(key)))continue;
                string wire=key;bool x=key.ToLowerInvariant().Contains("width")||key=="textXOff",y=key.ToLowerInvariant().Contains("height")||key=="textYOff";bool dimension=key.ToLowerInvariant().Contains("width")||key.ToLowerInvariant().Contains("height");
                slots[key]=new RuntimeProperty
                {
                    Read=()=>field.GetValue(control),
                    Parse=text=>{if(text==null||text.Length>16384)throw new InvalidDataException("Live value is too long.");if(field.FieldType==typeof(long))return Color(text);if(field.FieldType==typeof(bool))return Boolean(text);int number=Integer(text,dimension?1:-1000000,dimension?8192:1000000);return x?GUIGraphicsContext.ScaleHorizontal(number):y?GUIGraphicsContext.ScaleVertical(number):number;},
                    Format=value=>{if(field.FieldType==typeof(long))return unchecked((uint)(long)value).ToString("x8",CultureInfo.InvariantCulture);if(field.FieldType==typeof(bool))return (bool)value?"yes":"no";int scale=x?GUIGraphicsContext.ScaleHorizontal(10000):y?GUIGraphicsContext.ScaleVertical(10000):10000;return Math.Round((int)value*10000.0/Math.Max(1,scale)).ToString(CultureInfo.InvariantCulture);},
                    Write=value=>
                    {
                        field.SetValue(control,value);
                        var strip=control as GUIFilmstripControl;
                        if(strip!=null&&(wire=="textureWidth"||wire=="textureHeight"))strip.SetTextureDimensions(strip.TextureWidth,strip.TextureHeight);
                        if(strip!=null&&(wire=="thumbWidth"||wire=="thumbHeight")){int px=0,py=0,w=0,h=0;strip.GetThumbDimensions(ref px,ref py,ref w,ref h);strip.SetThumbDimensions(px,py,w,h);}
                        MarkCollectionRefresh(control);
                    }
                };
            }
            if(ScrollingText(type))
            {
                var property=type.GetProperty("Property");
                slots["label"]=new RuntimeProperty{Read=()=>property.GetValue(control,null),Write=value=>ReflowText(control,(string)value),Parse=text=>{if(text==null||text.Length>16384)throw new InvalidDataException("Live text exceeds its limit.");int id;return Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out id)&&id>=0?GUILocalizeStrings.Get(id):text;},Format=Number};
            }
            var fontName=Field(type,"_fontName");var font=Field(type,"_font");
            if(FontAdapter(type)&&fontName!=null&&font!=null)
            {
                slots["font"]=new RuntimeProperty
                {
                    Read=()=>new NativeFont{Name=(string)fontName.GetValue(control),Font=font.GetValue(control)},
                    Parse=text=>{string name=RuntimeFonts.Resolve(text);return new NativeFont{Name=name,Font=GUIFontManager.GetFont(name)};},
                    Format=value=>((NativeFont)value).Name??"",
                    Write=value=>{var state=(NativeFont)value;object actual=state.Font;if(actual!=null&&!RuntimeFonts.Registered(actual)){var current=GUIFontManager.GetFont(state.Name);if(current!=null&&current.FontName==state.Name)actual=current;else return;}fontName.SetValue(control,state.Name);font.SetValue(control,actual);RuntimeFonts.Invalidate(control);if(ScrollingText(type))ReflowText(control,(string)type.GetProperty("Property").GetValue(control,null));MarkCollectionRefresh(control);}
                };
            }
        }
    }
}
