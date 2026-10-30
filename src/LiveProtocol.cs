using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace MP1SkinStudio.Live
{
    public sealed class LiveFontDefinition
    {
        public const string Prefix="@studio-font:";
        public string Family;public int Height,Style;
        public void Validate(){if(String.IsNullOrWhiteSpace(Family)||Family.Length>120||Family.Any(c=>Char.IsControl(c)||"/\\:".Contains(c))||Height<1||Height>96||Style<0||Style>3)throw new InvalidDataException("Choose an installed font family, height 1–96 and regular/bold/italic style.");}
        public string ToWire(){Validate();var doc=new XmlDocument();var root=doc.CreateElement("font");doc.AppendChild(root);root.SetAttribute("family",Family);root.SetAttribute("height",Height.ToString(CultureInfo.InvariantCulture));root.SetAttribute("style",Style.ToString(CultureInfo.InvariantCulture));return Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(doc.OuterXml));}
        public static LiveFontDefinition Parse(string value)
        {
            if(value==null||value.Length>2048||!value.StartsWith(Prefix,StringComparison.Ordinal))throw new InvalidDataException("Invalid preview font descriptor.");
            var bytes=Convert.FromBase64String(value.Substring(Prefix.Length));var doc=new XmlDocument{XmlResolver=null};using(var input=new MemoryStream(bytes))using(var reader=XmlReader.Create(input,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2048}))doc.Load(reader);
            var root=doc.DocumentElement;int height,style;if(root==null||root.Name!="font"||root.HasChildNodes||root.Attributes.Count!=3||!Int32.TryParse(root.GetAttribute("height"),out height)||!Int32.TryParse(root.GetAttribute("style"),out style))throw new InvalidDataException("Invalid preview font definition.");
            var result=new LiveFontDefinition{Family=root.GetAttribute("family"),Height=height,Style=style};result.Validate();return result;
        }
    }
    public static class LiveWire
    {
        public const int Protocol=1,MaximumBytes=8*1024*1024;
        public static readonly string[] Fields={"posX","posY","width","height","label","font","textcolor","disabledcolor","texture","textureFocus","textureNoFocus","align","valign","textalign","textvalign","textXOff","textYOff","colordiffuse","keepaspectratio","zoom","zoomfromtop","onleft","onright","onup","ondown","hyperlink","selected","visible","markWidth","markHeight","textureCheckmark","textureCheckmarkNoFocus","textureHeight","textureWidth","spaceBetweenItems","itemWidth","itemHeight","thumbWidth","thumbHeight","itemWidthBig","itemHeightBig","thumbWidthBig","thumbHeightBig","textureWidthBig","textureHeightBig","animations"};
        public static string DirectoryPath
        {
            get{string custom=Environment.GetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR");return String.IsNullOrWhiteSpace(custom)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MP1SkinStudio","Live"):Path.GetFullPath(custom);}
        }
        public static string UserSid {get{return WindowsIdentity.GetCurrent().User.Value;}}
        public static string ThemeKey(string value){return String.IsNullOrEmpty(value)||value.Equals("default",StringComparison.OrdinalIgnoreCase)||value.Equals("Skin default",StringComparison.OrdinalIgnoreCase)?"":value;}
        public static PipeSecurity PipePermissions()
        {
            var security=new PipeSecurity();security.SetAccessRuleProtection(true,false);
            security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));return security;
        }
        public static XmlDocument Document(string command)
        {
            var doc=new XmlDocument{XmlResolver=null};var root=doc.CreateElement("live");doc.AppendChild(root);root.SetAttribute("protocol",Protocol.ToString(CultureInfo.InvariantCulture));root.SetAttribute("command",command);return doc;
        }
        public static XmlElement Add(XmlNode parent,string name)
        {var element=parent.OwnerDocument.CreateElement(name);parent.AppendChild(element);return element;}
        public static void Attr(XmlElement node,string key,object value){node.SetAttribute(key,Convert.ToString(value,CultureInfo.InvariantCulture)??"");}
        public static int Int(XmlElement node,string key,int min,int max)
        {int value;if(!Int32.TryParse(node.GetAttribute(key),NumberStyles.Integer,CultureInfo.InvariantCulture,out value)||value<min||value>max)throw new InvalidDataException("Invalid live value: "+key);return value;}
        public static string Required(XmlElement node,string name,int maximum=1024)
        {string value=node.GetAttribute(name);if(String.IsNullOrWhiteSpace(value)||value.Length>maximum)throw new InvalidDataException("Missing or invalid live field: "+name);return value;}
        public static XmlDocument Parse(byte[] bytes)
        {
            if(bytes.Length==0||bytes.Length>MaximumBytes)throw new InvalidDataException("Invalid live packet size.");
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaximumBytes};
            var doc=new XmlDocument{XmlResolver=null};using(var stream=new MemoryStream(bytes))using(var reader=XmlReader.Create(stream,settings))doc.Load(reader);
            if(doc.DocumentElement==null||doc.DocumentElement.Name!="live"||doc.DocumentElement.GetAttribute("protocol")!="1")throw new InvalidDataException("Incompatible live protocol.");return doc;
        }
        static void ReadExactly(Stream stream,byte[] data)
        {int offset=0;while(offset<data.Length){int count=stream.Read(data,offset,data.Length-offset);if(count==0)throw new EndOfStreamException("Live connection closed.");offset+=count;}}
        public static XmlDocument Read(Stream stream)
        {var prefix=new byte[4];ReadExactly(stream,prefix);int count=BitConverter.ToInt32(prefix,0);if(count<1||count>MaximumBytes)throw new InvalidDataException("Live packet exceeds 8 MB.");var bytes=new byte[count];ReadExactly(stream,bytes);return Parse(bytes);}
        public static void Write(Stream stream,XmlDocument doc)
        {var data=new UTF8Encoding(false,true).GetBytes(doc.OuterXml);if(data.Length>MaximumBytes)throw new InvalidDataException("Live packet exceeds 8 MB.");var prefix=BitConverter.GetBytes(data.Length);stream.Write(prefix,0,4);stream.Write(data,0,data.Length);stream.Flush();}
        public static string SafeRelative(string root,string relative)
        {
            if(String.IsNullOrWhiteSpace(relative)||relative.Length>500||Path.IsPathRooted(relative)||relative.Replace('\\','/').Split('/').Any(p=>p==".."||p.Length==0))throw new InvalidDataException("Invalid relative live path.");
            string prefix=Path.GetFullPath(root).TrimEnd('\\','/')+Path.DirectorySeparatorChar,full=Path.GetFullPath(Path.Combine(prefix,relative));if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Live path leaves the skin.");return full;
        }
    }
    public sealed class LiveEndpoint
    {
        public int ProcessId;public long StartTicks;public string Pipe,Nonce,Name;
        public XmlDocument ToXml()
        {var doc=LiveWire.Document("endpoint");var root=doc.DocumentElement;LiveWire.Attr(root,"pid",ProcessId);LiveWire.Attr(root,"started",StartTicks);LiveWire.Attr(root,"pipe",Pipe);LiveWire.Attr(root,"nonce",Nonce);LiveWire.Attr(root,"name",Name);return doc;}
        public static LiveEndpoint FromXml(XmlDocument doc)
        {
            var root=doc.DocumentElement;int pid=LiveWire.Int(root,"pid",1,Int32.MaxValue);long ticks;if(!Int64.TryParse(root.GetAttribute("started"),out ticks))throw new InvalidDataException("Invalid live endpoint start time.");
            string pipe=LiveWire.Required(root,"pipe",140),nonce=LiveWire.Required(root,"nonce",64);if(!pipe.StartsWith("MP1SkinStudio.Live."+pid+".",StringComparison.Ordinal)||pipe.Any(c=>!Char.IsLetterOrDigit(c)&&c!='.'))throw new InvalidDataException("Invalid live pipe name.");
            return new LiveEndpoint{ProcessId=pid,StartTicks=ticks,Pipe=pipe,Nonce=nonce,Name=root.GetAttribute("name")};
        }
        public static LiveEndpoint[] Discover()
        {
            if(!Directory.Exists(LiveWire.DirectoryPath))return new LiveEndpoint[0];var result=new List<LiveEndpoint>();
            foreach(string file in Directory.GetFiles(LiveWire.DirectoryPath,"endpoint-*.xml").Take(64))
            {
                try{if(new FileInfo(file).Length>8192)continue;var endpoint=FromXml(LiveWire.Parse(File.ReadAllBytes(file)));using(var process=Process.GetProcessById(endpoint.ProcessId)){if(process.StartTime.ToUniversalTime().Ticks!=endpoint.StartTicks||process.HasExited)continue;}result.Add(endpoint);}
                catch(IOException){}catch(InvalidDataException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}catch(XmlException){}catch(System.ComponentModel.Win32Exception){}
            }
            return result.ToArray();
        }
        public override string ToString(){return (String.IsNullOrEmpty(Name)?"MediaPortal":Name)+" · PID "+ProcessId;}
    }
    public sealed class LiveItem
    {
        public string Token,Kind,Description,Parent,Path;public int Id;public bool Automatic;
        public readonly Dictionary<string,string> Values=new Dictionary<string,string>(StringComparer.Ordinal);
        public readonly HashSet<string> Supported=new HashSet<string>(StringComparer.Ordinal);
        public static LiveItem Read(XmlElement element)
        {
            var item=new LiveItem{Token=LiveWire.Required(element,"token",100),Kind=element.GetAttribute("type"),Description=element.GetAttribute("description"),Parent=element.GetAttribute("parent"),Path=element.GetAttribute("path"),Id=LiveWire.Int(element,"id",Int32.MinValue,Int32.MaxValue),Automatic=element.GetAttribute("automatic")=="true"};
            foreach(XmlElement value in element.SelectNodes("value")){string key=LiveWire.Required(value,"key",64);if(item.Values.ContainsKey(key))throw new InvalidDataException("Duplicate live property.");item.Values.Add(key,value.InnerText);if(value.GetAttribute("writable")=="true")item.Supported.Add(key);}return item;
        }
        public string Value(string key){string value;return Values.TryGetValue(key,out value)?value:"";}
        public override string ToString(){return Kind+" · "+Id+" · "+Description+" ("+Path+")";}
    }
}
