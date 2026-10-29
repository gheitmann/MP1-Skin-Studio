using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;

namespace MP1SkinStudio.LiveBridge
{
    // Native values are retained separately from wire values: disconnect must
    // restore exact runtime pixels, not a rounded skin-coordinate conversion.
    public sealed partial class RuntimeProperty
    {
        public Func<object> Read;public Action<object> Write;public Func<string,object> Parse;public Func<object,string> Format;
        public string Value {get{return Format(Read());}}
        public static HashSet<string> SupportedFields(Type type)
        {
            if(!typeof(GUIControl).IsAssignableFrom(type))throw new ArgumentException("A MediaPortal control type is required.");
            var fields=new HashSet<string>(StringComparer.Ordinal);
            foreach(string key in LiveWire.Fields){string name=PropertyName(type,key);var p=name==null?null:type.GetProperty(name,BindingFlags.Public|BindingFlags.Instance);if(p!=null&&p.CanRead&&p.CanWrite&&p.GetIndexParameters().Length==0)fields.Add(key);}
            if(typeof(GUIButtonControl).IsAssignableFrom(type)){if(Field(type,"_focusedTextureName")!=null&&Field(type,"_imageFocused")!=null)fields.Add("textureFocus");if(Field(type,"_nonFocusedTextureName")!=null&&Field(type,"_imageNonFocused")!=null)fields.Add("textureNoFocus");}
            AddSupportedExtended(type,fields);fields.Add("visible");return fields;
        }
        static string Number(object value){return Convert.ToString(value,CultureInfo.InvariantCulture);}
        static int Integer(string value,int minimum=-1000000,int maximum=1000000)
        {int result;if(!Int32.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out result)||result<minimum||result>maximum)throw new InvalidDataException("Live geometry requires a bounded integer.");return result;}
        static bool Boolean(string value)
        {if(value=="yes"||value=="true")return true;if(value=="no"||value=="false")return false;throw new InvalidDataException("Live Boolean value must be yes/no or true/false.");}
        static long Color(string value)
        {uint result;value=value.TrimStart('#');if((value.Length!=6&&value.Length!=8)||!UInt32.TryParse(value,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out result))throw new InvalidDataException("Live color must use RRGGBB or AARRGGBB.");return value.Length==6?result|0xff000000L:result;}
        static object EnumValue(Type type,string value)
        {
            string token=value.ToLowerInvariant();if(token=="middle")token="center";
            string name=Enum.GetNames(type).FirstOrDefault(n=>n.Equals("ALIGN_"+token,StringComparison.OrdinalIgnoreCase)||n.Equals(token,StringComparison.OrdinalIgnoreCase));
            if(name==null)throw new InvalidDataException("Unsupported live alignment: "+value);return Enum.Parse(type,name);
        }
        public static Dictionary<string,RuntimeProperty> For(GUIControl control,string skinRoot)
        {
            var slots=new Dictionary<string,RuntimeProperty>(StringComparer.Ordinal);
            foreach(string key in LiveWire.Fields)
            {
                string name=PropertyName(control.GetType(),key);if(name==null)continue;var property=control.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
                if(property==null||!property.CanRead||!property.CanWrite||property.GetIndexParameters().Length>0)continue;
                string field=key;Type type=property.PropertyType;
                bool scaledX=field=="posX"||field=="width"||field=="textXOff"||field=="markWidth"||field.ToLowerInvariant().Contains("width");
                bool scaledY=field=="posY"||field=="height"||field=="textYOff"||field=="spaceBetweenItems"&&!(control is GUITextControl)||field.ToLowerInvariant().Contains("height");
                bool positive=field.ToLowerInvariant().Contains("width")||field.ToLowerInvariant().Contains("height");
                slots[field]=new RuntimeProperty
                {
                    Read=()=>property.GetValue(control,null),
                    Write=value=>{if(field=="posX")control.SetPosition((int)value,control.YPosition);else if(field=="posY")control.SetPosition(control.XPosition,(int)value);else property.SetValue(control,value,null);Refresh(control);},
                    Parse=text=>
                    {
                        if(text==null||text.Length>16384)throw new InvalidDataException("Live text exceeds 16,384 characters.");
                        if(type==typeof(string))
                        {
                            if(field=="font")ValidateFont(text);
                            if(field=="label"){int resource;if(Int32.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out resource)&&resource>=0)return GUILocalizeStrings.Get(resource);}
                            if(field.StartsWith("texture",StringComparison.Ordinal)&&text.Length>0&&text!="-"){LiveWire.SafeRelative(skinRoot,"Media/"+text);}
                            return text;
                        }
                        if(type==typeof(bool))return Boolean(text);
                        if(type==typeof(long))return Color(text);
                        if(type.IsEnum)return EnumValue(type,text);
                        if(type==typeof(int)){int number=Integer(text,positive?1:field=="spaceBetweenItems"?0:-1000000,positive?8192:1000000);return scaledX?GUIGraphicsContext.ScaleHorizontal(number):scaledY?GUIGraphicsContext.ScaleVertical(number):number;}
                        throw new InvalidDataException("Unsupported native property type.");
                    },
                    Format=value=>
                    {
                        if(type==typeof(long))return unchecked((uint)(long)value).ToString("x8",CultureInfo.InvariantCulture);
                        if(type==typeof(bool))return (bool)value?"yes":"no";
                        if(type.IsEnum)return value.ToString().Replace("ALIGN_","").ToLowerInvariant();
                        if(type==typeof(int)&&(scaledX||scaledY)){int scale=scaledX?GUIGraphicsContext.ScaleHorizontal(10000):GUIGraphicsContext.ScaleVertical(10000);return Math.Round((int)value*10000.0/Math.Max(1,scale)).ToString(CultureInfo.InvariantCulture);}
                        return Number(value);
                    }
                };
            }
            AddButtonTexture(slots,control,"textureFocus","_focusedTextureName","_imageFocused",skinRoot);
            AddButtonTexture(slots,control,"textureNoFocus","_nonFocusedTextureName","_imageNonFocused",skinRoot);
            AddExtended(slots,control);
            var allow=typeof(GUIControl).GetField("_allowHiddenFocus",BindingFlags.Instance|BindingFlags.NonPublic);
            slots["visible"]=new RuntimeProperty
            {
                Read=()=>new Visibility{Condition=control.GetVisibleCondition(),Allow=allow!=null&&(bool)allow.GetValue(control),Shown=control.Visible},
                Write=value=>{var v=(Visibility)value;control.SetVisibleCondition(v.Condition,v.Allow);control.Visible=v.Shown;control.SetInitialVisibility();},
                Parse=text=>{if(text.Length>4096)throw new InvalidDataException("Visibility condition is too long.");bool? shown=text=="yes"||text=="true"||text.Length==0?(bool?)true:text=="no"||text=="false"?(bool?)false:null;int condition=shown.HasValue?0:GUIInfoManager.TranslateString(text);if(!shown.HasValue&&condition==0)throw new InvalidDataException("MediaPortal cannot resolve this visibility condition.");return new Visibility{Condition=condition,Shown=shown??true};},
                Format=value=>{var v=(Visibility)value;return v.Condition==0?(v.Shown?"yes":"no"):"Runtime condition "+v.Condition;}
            };
            return slots;
        }
        sealed class Visibility {public int Condition;public bool Allow,Shown;}
        static void ValidateFont(string text)
        {var font=GUIFontManager.GetFont(text);if(text.Length==0||font==null||!String.Equals(font.FontName,text,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("This font is not loaded by MediaPortal.");}
        static FieldInfo Field(Type type,string name)
        {for(Type t=type;t!=null;t=t.BaseType){var field=t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(field!=null)return field;}return null;}
        static void AddButtonTexture(Dictionary<string,RuntimeProperty> slots,GUIControl control,string key,string fieldName,string imageName,string root)
        {
            if(!(control is GUIButtonControl))return;var field=Field(control.GetType(),fieldName);var child=Field(control.GetType(),imageName);if(field==null||child==null)return;
            slots[key]=new RuntimeProperty{Read=()=>field.GetValue(control),Write=value=>{field.SetValue(control,value);var image=child.GetValue(control);if(image!=null){var file=image.GetType().GetProperty("FileName");if(file!=null&&file.CanWrite)file.SetValue(image,value,null);}Refresh(control);},Parse=text=>{if(text.Length>500)throw new InvalidDataException("Texture path too long.");if(text.Length>0&&text!="-")LiveWire.SafeRelative(root,"Media/"+text);return text;},Format=Number};
        }
        static void Refresh(GUIControl control)
        {var method=control.GetType().GetMethod("Refresh",Type.EmptyTypes);if(method!=null)method.Invoke(control,null);MarkCollectionRefresh(control);}
        static string PropertyName(Type type,string key)
        {
            switch(key)
            {
                case "posX":return "XPosition";case "posY":return "YPosition";case "width":return "Width";case "height":return "Height";
                case "label":return "Label";case "font":return "FontName";case "textcolor":return "TextColor";case "disabledcolor":return "DisabledColor";
                case "texture":return typeof(GUIImage).IsAssignableFrom(type)?"FileName":null;
                case "colordiffuse":return "ColourDiffuse";case "textXOff":return "TextOffsetX";case "textYOff":return "TextOffsetY";
                case "align":return typeof(GUIGroup).IsAssignableFrom(type)?"GroupAlignment":typeof(GUIImage).IsAssignableFrom(type)?"ImageAlignment":"TextAlignment";
                case "valign":return typeof(GUIImage).IsAssignableFrom(type)?"ImageVAlignment":"TextVAlignment";
                case "textalign":return "TextAlignment";case "textvalign":return "TextVAlignment";
                case "keepaspectratio":return "KeepAspectRatio";case "zoom":return "Zoom";case "zoomfromtop":return "ZoomFromTop";
                case "onleft":return "NavigateLeft";case "onright":return "NavigateRight";case "onup":return "NavigateUp";case "ondown":return "NavigateDown";
                case "hyperlink":return "HyperLink";case "selected":return "Selected";
                case "markWidth":return "CheckMarkWidth";case "markHeight":return "CheckMarkHeight";
                case "textureHeight":return typeof(GUIListControl).IsAssignableFrom(type)?"ItemHeight":"TextureHeight";case "textureWidth":return "TextureWidth";case "spaceBetweenItems":return type.GetProperty("Space")!=null?"Space":"SpaceBetweenItems";
                case "itemWidth":return Collection(type)?null:"ItemWidth";case "itemHeight":return Collection(type)?null:"ItemHeight";case "thumbWidth":return "ThumbWidth";case "thumbHeight":return "ThumbHeight";
                case "itemWidthBig":return Collection(type)?null:"ItemWidthBig";case "itemHeightBig":return Collection(type)?null:"ItemHeightBig";case "thumbWidthBig":return "ThumbWidthBig";case "thumbHeightBig":return "ThumbHeightBig";
                default:return null;
            }
        }
    }
}
