using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class LanguagePack
    {
        sealed class Entry{public string Text,Prefix;}
        readonly Dictionary<int,Entry> values=new Dictionary<int,Entry>();readonly byte[] bytes;
        public readonly string Name,FileName,Digest;
        public int Count{get{return values.Count;}}
        public IEnumerable<int> Ids{get{return values.Keys;}}
        LanguagePack(byte[] data,string fileName,string name){bytes=data.ToArray();FileName=fileName;Name=name;Digest=ProjectFile.Digest(data);}
        public byte[] Snapshot(){return bytes.ToArray();}
        public bool TryGet(int id,bool prefix,out string text)
        {Entry entry;if(values.TryGetValue(id,out entry)){text=(prefix?entry.Prefix:"")+entry.Text;return true;}text=null;return false;}
        public static LanguagePack Load(string path)
        {if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("Language file exceeds 2 MB.");return FromBytes(File.ReadAllBytes(path),Path.GetFileName(path));}
        public static LanguagePack FromBytes(byte[] data,string fileName)
        {
            if(data==null||data.Length>2*1024*1024)throw new InvalidDataException("Language file exceeds 2 MB.");
            if(String.IsNullOrWhiteSpace(fileName)||fileName.Length>160||Path.GetFileName(fileName)!=fileName||fileName.Any(Char.IsControl))throw new InvalidDataException("Invalid language filename.");
            var doc=new XmlDocument{XmlResolver=null};var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024};using(var stream=new MemoryStream(data))using(var reader=XmlReader.Create(stream,settings))doc.Load(reader);
            var root=doc.DocumentElement;if(root==null||root.Name!="Language")throw new InvalidDataException("Expected a MediaPortal language file with a Language root.");string name=root.GetAttribute("name");if(name.Length==0)name=fileName;if(name.Length>160)throw new InvalidDataException("Language name is too long.");
            var pack=new LanguagePack(data,fileName,name);var nodes=root.SelectNodes("Section[@name='unmapped']/String");if(nodes.Count==0||nodes.Count>10000)throw new InvalidDataException("Language files must contain between 1 and 10000 unmapped entries.");
            foreach(XmlElement node in nodes)
            {int id;if(!Int32.TryParse(node.GetAttribute("id"),NumberStyles.None,CultureInfo.InvariantCulture,out id)||id<0||pack.values.ContainsKey(id))throw new InvalidDataException("Invalid or duplicate language text ID.");string text=node.InnerText,prefix=node.GetAttribute("prefix");if(text.Length+prefix.Length>32768)throw new InvalidDataException("A language entry is too long.");pack.values.Add(id,new Entry{Text=text,Prefix=prefix});}return pack;
        }
    }
    public sealed class LanguagePreview
    {
        public LanguagePack Primary,Fallback;public bool Prefixes=true;
        public bool Active{get{return Primary!=null||Fallback!=null;}}
        public LanguagePreview Copy(){return new LanguagePreview{Primary=Primary,Fallback=Fallback,Prefixes=Prefixes};}
        public void SetFrom(LanguagePreview other){Primary=other.Primary;Fallback=other.Fallback;Prefixes=other.Prefixes;}
        public bool TryGet(int id,out string text,out string origin)
        {if(Primary!=null&&Primary.TryGet(id,Prefixes,out text)){origin=Primary.Name;return true;}if(Fallback!=null&&Fallback.TryGet(id,Prefixes,out text)){origin=Fallback.Name+" (Fallback)";return true;}text=null;origin="not found";return false;}
        public IEnumerable<int> Ids(){return (Primary==null?Enumerable.Empty<int>():Primary.Ids).Concat(Fallback==null?Enumerable.Empty<int>():Fallback.Ids).Distinct().OrderBy(i=>i);}
        public string Signature(){return (Prefixes?"P":"-")+"|"+(Primary==null?"":Primary.Digest+":"+Primary.FileName)+"|"+(Fallback==null?"":Fallback.Digest+":"+Fallback.FileName);}
        public static bool TryId(string value,out int id){id=0;return !String.IsNullOrEmpty(value)&&Char.IsNumber(value[0])&&Int32.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out id);}
        public static bool UseLabelName(SkinControl control)
        {PropertyValue description;return control.Get("label").Length>0&&(!control.Properties.TryGetValue("description",out description)||description.Value.Length==0||(!description.Local&&!description.Origin.StartsWith("Style ",StringComparison.Ordinal)));}
    }
    public sealed partial class PreviewState
    {
        public string Label(SkinControl control,string value)
        {
            if(value=="-")return "";int id;string text,origin;if(Project.Editor.Language.Active&&TryLabelId(control,value,out id))return Project.Editor.Language.TryGet(id,out text,out origin)?Text(control,text):"["+id+"]";
            return Text(control,value);
        }
        internal bool ResolvedLabel(SkinControl control,string value)
        {
            if(value=="-")return true;int id;string text,origin;
            if(Project.Editor.Language.Active&&TryLabelId(control,value,out id))
            {if(!Project.Editor.Language.TryGet(id,out text,out origin))return false;value=text;}
            return TryText(control,value,out text);
        }
        public bool TryLabelId(SkinControl control,string value,out int id)
        {
            id=0;if(Project.Editor.Preview.Values.ContainsKey(value))return false;string raw=value,next;var seen=new HashSet<string>();while(control.Defines!=null&&control.Defines.TryGetValue(raw,out next)&&seen.Add(raw))raw=next;return LanguagePreview.TryId(raw,out id);
        }
    }
    public static partial class ProjectFile
    {
        static void SaveLanguagePreview(SkinProject project,XmlDocument doc,Dictionary<string,byte[]> entries)
        {
            var language=project.Editor.Language;if(!language.Active&&language.Prefixes)return;var container=doc.CreateElement("languagePreview");container.SetAttribute("prefixes",language.Prefixes?"true":"false");doc.SelectSingleNode("/skinStudioProject/editorState").AppendChild(container);
            foreach(var pair in new[]{new{Role="primary",Pack=language.Primary},new{Role="fallback",Pack=language.Fallback}})
            {if(pair.Pack==null)continue;string path="language/"+pair.Role+".xml";entries.Add(path,pair.Pack.Snapshot());var item=doc.CreateElement("pack");item.SetAttribute("role",pair.Role);item.SetAttribute("entry",path);item.SetAttribute("file",pair.Pack.FileName);item.SetAttribute("sha256",pair.Pack.Digest);container.AppendChild(item);}
            if(entries.Values.Sum(v=>(long)v.Length)>MaximumTotal)throw new InvalidDataException("Editor project exceeds 100 MB.");
        }
        static void LoadLanguagePreview(SkinProject project,XmlDocument doc,System.IO.Compression.ZipArchive archive)
        {
            var containers=doc.SelectNodes("/skinStudioProject/editorState/languagePreview");if(containers.Count==0)return;if(containers.Count!=1)throw new InvalidDataException("Duplicate language state.");var container=(XmlElement)containers[0];bool prefixes;if(!Boolean.TryParse(container.GetAttribute("prefixes"),out prefixes))throw new InvalidDataException("Invalid language option.");var language=new LanguagePreview{Prefixes=prefixes};var roles=new HashSet<string>();
            foreach(XmlElement item in container.SelectNodes("pack"))
            {string role=item.GetAttribute("role");if((role!="primary"&&role!="fallback")||!roles.Add(role)||item.GetAttribute("entry")!="language/"+role+".xml")throw new InvalidDataException("Invalid language snapshot.");byte[] data=ReadEntry(archive,item.GetAttribute("entry"));if(Digest(data)!=item.GetAttribute("sha256"))throw new InvalidDataException("Language snapshot is corrupted.");var pack=LanguagePack.FromBytes(data,item.GetAttribute("file"));if(role=="primary")language.Primary=pack;else language.Fallback=pack;}
            project.Editor.Language.SetFrom(language);
        }
    }
}
