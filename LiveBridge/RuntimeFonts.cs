using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using MediaPortal.GUI.Library;
using MP1SkinStudio.Live;

namespace MP1SkinStudio.LiveBridge
{
    public static class RuntimeFonts
    {
        const int MaximumVariants=32;
        static readonly Dictionary<string,GUIFont> cache=new Dictionary<string,GUIFont>();
        static readonly FieldInfo registry=typeof(GUIFontManager).GetField("ListFonts",BindingFlags.Static|BindingFlags.NonPublic);
        static Func<LiveFontDefinition,string,int,GUIFont> create=Create;
        public static bool Registered(object font){var fonts=registry.GetValue(null) as List<GUIFont>;return fonts!=null&&fonts.Contains(font as GUIFont);}
        public static string RegistryStamp
        {get{var fonts=registry.GetValue(null) as List<GUIFont>;return fonts==null?"":String.Join("|",fonts.Where(f=>!cache.Values.Contains(f)).Select(f=>RuntimeHelpers.GetHashCode(f).ToString(CultureInfo.InvariantCulture)+":"+f.FontName+":"+f.FileName+":"+f.FontSize+":"+(int)f.FontStyle));}}
        public static string Resolve(string value)
        {
            if(value==null||value.Length>2048)throw new InvalidDataException("Invalid live font.");
            if(!value.StartsWith(LiveFontDefinition.Prefix,StringComparison.Ordinal))
            {
                var loaded=GUIFontManager.GetFont(value);if(value.Length==0||loaded==null)throw new InvalidDataException("This font is not loaded by MP.");
                if(loaded.FontName==value)return value;
                var aliases=typeof(GUIFontManager).GetField("DictFontAlias",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as Dictionary<string,string>;
                string target;if(aliases!=null&&aliases.TryGetValue(value,out target)&&target==loaded.FontName)return value;
                throw new InvalidDataException("This font is not loaded by MP. Update to bridge 1.3 for edited font definitions.");
            }
            var definition=LiveFontDefinition.Parse(value);
            lock(GUIFontManager.Renderlock)
            {
                var fonts=registry.GetValue(null) as List<GUIFont>;if(fonts==null||fonts.Count==0)throw new InvalidOperationException("MP fonts are not initialized.");
                foreach(string old in cache.Where(p=>!fonts.Contains(p.Value)).Select(p=>p.Key).ToArray())cache.Remove(old);
                double dpi=96;var form=GUIGraphicsContext.form;if(form!=null&&form.IsHandleCreated)using(var g=form.CreateGraphics())dpi=g.DpiY;
                double basis=576*(Environment.OSVersion.Version.Major>=6?dpi/96:1);int points=Math.Max(1,(int)(definition.Height*GUIGraphicsContext.Height*GUIGraphicsContext.ZoomVertical/basis));
                if(points>256)throw new InvalidDataException("The scaled font size exceeds the live preview limit.");
                string key=definition.ToWire()+"|"+points.ToString(CultureInfo.InvariantCulture)+"|"+GUIGraphicsContext.SkinCacheFolder;
                GUIFont existing;if(cache.TryGetValue(key,out existing)){if(create==Create&&typeof(GUIFont).GetField("_d3dxFont",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(existing)==null)throw new InvalidOperationException("MP is rebuilding font resources. Wait for MP to finish, then reconnect.");return existing.FontName;}
                if(cache.Count>=MaximumVariants)throw new InvalidOperationException("The 32-variant live font cache is full. Save your project and restart MP before creating more variants.");
                string name;using(var sha=SHA256.Create())name="mp1studiofont_"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","").Substring(0,24).ToLowerInvariant();
                // Never replace a registered alias or reuse a pre-existing font ID.
                int id=fonts.Select(f=>f.ID).DefaultIfEmpty(-1).Max()+1;GUIFont font=create(definition,name,points);font.ID=id;
                try
                {
                    // A distinct system-font fallback is used for preview text. This avoids
                    // the native atlas slot limit and leaves every original font untouched.
                    if(create==Create)Initialize(font);
                    fonts.Add(font);cache.Add(key,font);return name;
                }
                catch{font.Dispose();throw;}
            }
        }
        static GUIFont Create(LiveFontDefinition definition,string name,int points)
        {
            using(var family=new FontFamily(definition.Family))if(!family.IsStyleAvailable((FontStyle)definition.Style))throw new InvalidDataException("This family does not support the requested font style.");
            // The parameterless SDK constructor does not reinitialize the global renderer.
            var font=new GUIFont{FontName=name,FileName=definition.Family,FontSize=points,FontStyle=(FontStyle)definition.Style,ID=-1};
            font.SetRange(32,32);font.Load();return font;
        }
        static void Initialize(GUIFont font)
        {
            var device=typeof(GUIGraphicsContext).GetField("DX9Device",BindingFlags.Public|BindingFlags.Static);if(device==null||device.GetValue(null)==null)throw new InvalidOperationException("MP's font renderer is not ready.");
            font.InitializeDeviceObjects();
            var native=typeof(GUIFont).GetField("_d3dxFont",BindingFlags.Instance|BindingFlags.NonPublic);
            if(native==null||native.GetValue(font)==null)throw new InvalidOperationException("MP could not allocate the preview font.");
        }
        public static void Invalidate(GUIControl control)
        {
            var nameField=FindField(control.GetType(),"_fontName");var cached=FindField(control.GetType(),"_font");
            if(nameField!=null&&cached!=null&&cached.FieldType==typeof(GUIFont))
            {
                string alias=nameField.GetValue(control) as string;
                if(!String.IsNullOrEmpty(alias)){var current=GUIFontManager.GetFont(alias);if(current!=null&&Registered(current)&&current.FontName==alias)cached.SetValue(control,current);}
            }
            for(Type type=control.GetType();type!=null;type=type.BaseType)
            {
                var context=type.GetField("_context",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(context!=null&&context.FieldType.Name=="FontRenderContext")context.SetValue(control,null);
                foreach(string name in new[]{"_textwidth","_textheight"}){var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(field!=null&&field.FieldType==typeof(int))field.SetValue(control,0);}
                var child=type.GetField("_labelControl",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(child!=null){var label=child.GetValue(control) as GUIControl;if(label!=null)Invalidate(label);}
            }
        }
        static FieldInfo FindField(Type type,string name){for(;type!=null;type=type.BaseType){var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly);if(field!=null)return field;}return null;}
    }
}
