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
    public sealed partial class RuntimeProperty
    {
        static readonly string[] checkFields={"textureCheckmark","textureCheckmarkNoFocus","markWidth","markHeight"};
        static readonly Dictionary<string,string[]> galleryFields=new Dictionary<string,string[]>
        {
            {"itemWidth",new[]{"_lowItemWidth","_itemWidth"}}, {"itemHeight",new[]{"_lowItemHeight","_itemHeight"}},
            {"textureWidth",new[]{"_lowTextureWidth","_textureWidth"}}, {"textureHeight",new[]{"_lowTextureHeight","_textureHeight"}},
            {"thumbWidth",new[]{"_widthThumbNailLow","_thumbNailWidth"}}, {"thumbHeight",new[]{"_heightThumbNailLow","_thumbNailHeight"}},
            {"itemWidthBig",new[]{"_bigItemWidth","_itemWidth"}}, {"itemHeightBig",new[]{"_bigItemHeight","_itemHeight"}},
            {"textureWidthBig",new[]{"_bigTextureWidth","_textureWidth"}}, {"textureHeightBig",new[]{"_bigTextureHeight","_textureHeight"}},
            {"thumbWidthBig",new[]{"_widthThumbBig","_bigThumbWidth","_thumbNailWidth"}}, {"thumbHeightBig",new[]{"_heightThumbBig","_bigThumbHeight","_thumbNailHeight"}}
        };
        static void AddSupportedSpecial(Type type,HashSet<string> fields)
        {
            if(type.Assembly!=typeof(GUIControl).Assembly)return;
            if(typeof(GUICheckMarkControl).IsAssignableFrom(type))foreach(string key in checkFields)fields.Add(key);
            if(typeof(GUIThumbnailPanel).IsAssignableFrom(type))foreach(var pair in galleryFields)if(pair.Value.All(n=>Field(type,n)!=null))fields.Add(pair.Key);
        }
        sealed class CheckState {public object Definition;public object[] Children;}
        sealed class CheckSelection {public string Expression;public bool Selected;}
        sealed class GalleryState {public int[] Values;}
        static bool? BigGallery(GUIThumbnailPanel control)
        {
            Type type=control.GetType();Func<string,int> n=name=>(int)Field(type,name).GetValue(control);
            bool small=n("_itemWidth")==n("_lowItemWidth")&&n("_itemHeight")==n("_lowItemHeight")&&n("_textureWidth")==n("_lowTextureWidth")&&n("_textureHeight")==n("_lowTextureHeight")&&n("_thumbNailWidth")==n("_widthThumbNailLow")&&n("_thumbNailHeight")==n("_heightThumbNailLow");
            bool big=n("_itemWidth")==n("_bigItemWidth")&&n("_itemHeight")==n("_bigItemHeight")&&n("_textureWidth")==n("_bigTextureWidth")&&n("_textureHeight")==n("_bigTextureHeight")&&n("_thumbNailWidth")==n("_widthThumbBig")&&n("_thumbNailHeight")==n("_heightThumbBig");
            return small==big?(bool?)null:big;
        }
        static void AddSpecial(Dictionary<string,RuntimeProperty> slots,GUIControl control,string root)
        {
            Type type=control.GetType();if(type.Assembly!=typeof(GUIControl).Assembly)return;
            var check=control as GUICheckMarkControl;
            if(check!=null)
            {
                var expression=Field(type,"_selected");
                slots["selected"]=new RuntimeProperty{Read=()=>new CheckSelection{Expression=(string)expression.GetValue(check),Selected=check.Selected},Parse=text=>new CheckSelection{Expression="",Selected=Boolean(text)},Format=value=>((CheckSelection)value).Selected?"yes":"no",Write=value=>{var state=(CheckSelection)value;expression.SetValue(check,state.Expression);check.Selected=state.Selected;}};
            }
            if(check!=null)foreach(string key in checkFields)
            {
                string wire=key;bool dimension=key=="markWidth"||key=="markHeight",horizontal=key=="markWidth";
                var definition=Field(type,dimension?horizontal?"_checkMarkWidth":"_checkMarkHeight":key=="textureCheckmark"?"_checkMarkFocusTextureName":"_checkMarkNoFocusTextureName");
                var children=(dimension?new[]{"_imageCheckMarkFocused","_imageCheckMarkNonFocused"}:new[]{key=="textureCheckmark"?"_imageCheckMarkFocused":"_imageCheckMarkNonFocused"}).Select(n=>Field(type,n)).ToArray();
                Func<GUIAnimation,object> read=image=>dimension?(object)(horizontal?image.Width:image.Height):image.FileName;
                slots[key]=new RuntimeProperty
                {
                    Read=()=>new CheckState{Definition=definition.GetValue(check),Children=children.Select(f=>{var image=f.GetValue(check) as GUIAnimation;return image==null?null:read(image);}).ToArray()},
                    Parse=text=>
                    {
                        object value;
                        if(dimension){int number=Integer(text,1,8192);value=horizontal?GUIGraphicsContext.ScaleHorizontal(number):GUIGraphicsContext.ScaleVertical(number);}
                        else{if(text==null||text.Length>500)throw new InvalidDataException("Invalid checkbox image path.");if(text.Length>0&&text!="-")LiveWire.SafeRelative(root,"Media/"+text);if(text.EndsWith(".xml",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Choose a checkbox image; replacing animation XML templates requires a page reload.");value=text;}
                        return new CheckState{Definition=value,Children=children.Select(f=>value).ToArray()};
                    },
                    Format=value=>{var state=(CheckState)value;object displayed=state.Children.FirstOrDefault(v=>v!=null)??state.Definition;if(!dimension)return (string)displayed;int scale=horizontal?GUIGraphicsContext.ScaleHorizontal(10000):GUIGraphicsContext.ScaleVertical(10000);return Math.Round((int)displayed*10000.0/Math.Max(1,scale)).ToString(CultureInfo.InvariantCulture);},
                    Write=value=>
                    {
                        var state=(CheckState)value;definition.SetValue(check,state.Definition);
                        for(int i=0;i<children.Length;i++)
                        {
                            var image=children[i].GetValue(check) as GUIAnimation;if(image==null||state.Children[i]==null)continue;
                            if(dimension){if(horizontal)image.Width=(int)state.Children[i];else image.Height=(int)state.Children[i];image.Refresh();}
                            else if(image.FileName!=(string)state.Children[i])
                            {
                                // Unallocated controls have no renderer resources to rebuild yet.
                                if(Field(typeof(GUIAnimation),"_images").GetValue(image)==null)Field(typeof(GUIAnimation),"_textureNames").SetValue(image,state.Children[i]);
                                else image.SetFileName((string)state.Children[i]);
                            }
                        }
                    }
                };
            }
            var gallery=control as GUIThumbnailPanel;
            if(gallery!=null)foreach(var pair in galleryFields)
            {
                string key=pair.Key;var fields=pair.Value.Select(n=>Field(type,n)).ToArray();if(fields.Any(f=>f==null))continue;
                bool big=key.EndsWith("Big",StringComparison.Ordinal),horizontal=key.Contains("Width");
                slots[key]=new RuntimeProperty
                {
                    Read=()=>new GalleryState{Values=fields.Select(f=>(int)f.GetValue(gallery)).ToArray()},
                    Parse=text=>
                    {
                        int number=Integer(text,1,8192),pixels=horizontal?GUIGraphicsContext.ScaleHorizontal(number):GUIGraphicsContext.ScaleVertical(number);bool? mode=BigGallery(gallery);
                        if(!mode.HasValue)throw new InvalidOperationException("The gallery's active size cannot be identified safely. Switch MP to a distinct small or large icon layout, then reconnect.");
                        var values=fields.Select(f=>(int)f.GetValue(gallery)).ToArray();for(int i=0;i<values.Length-1;i++)values[i]=pixels;if(mode.Value==big)values[values.Length-1]=pixels;return new GalleryState{Values=values};
                    },
                    Format=value=>{int scale=horizontal?GUIGraphicsContext.ScaleHorizontal(10000):GUIGraphicsContext.ScaleVertical(10000);return Math.Round(((GalleryState)value).Values[0]*10000.0/Math.Max(1,scale)).ToString(CultureInfo.InvariantCulture);},
                    Write=value=>{var values=((GalleryState)value).Values;for(int i=0;i<fields.Length;i++)fields[i].SetValue(gallery,values[i]);gallery.SetTextureDimensions(gallery.TextureWidth,gallery.TextureHeight);MarkCollectionRefresh(gallery);}
                };
            }
        }
    }
}
