using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MP1SkinStudio.Live;
namespace MP1SkinStudio
{
 public static partial class SelfTests
 {
  static XmlDocument LiveSnapshot(SkinProject project)
  {
   var doc=LiveWire.Document("snapshot");var root=doc.DocumentElement;LiveWire.Attr(root,"skin",project.Root);LiveWire.Attr(root,"theme",project.Theme);LiveWire.Attr(root,"page",project.Page);LiveWire.Attr(root,"window",project.WindowId);LiveWire.Attr(root,"epoch","test-epoch");
   int token=0;foreach(var c in project.Controls){var node=LiveWire.Add(root,"control");LiveWire.Attr(node,"token","control-"+(++token));LiveWire.Attr(node,"id",c.Get("id","0"));LiveWire.Attr(node,"type",c.Get("type"));LiveWire.Attr(node,"description",c.Get("description"));LiveWire.Attr(node,"path","/"+(token-1));foreach(string key in LiveWire.Fields){var value=LiveWire.Add(node,"value");LiveWire.Attr(value,"key",key);LiveWire.Attr(value,"writable","true");value.InnerText=c.Get(key);}}return doc;
  }
  static void RunLiveTests(string root)
  {
   Test("Live protocol round trip retains Unicode and XML text",()=>{var doc=LiveWire.Document("frame");LiveWire.Add(doc.DocumentElement,"text").InnerText="Größe & <test> 日本語";using(var stream=new MemoryStream()){LiveWire.Write(stream,doc);stream.Position=0;Check(LiveWire.Read(stream).OuterXml==doc.OuterXml,"Wire changes text.");}});
   Test("Live XML rejects external entities",()=>Reject(()=>LiveWire.Parse(Encoding.UTF8.GetBytes("<!DOCTYPE live [<!ENTITY x SYSTEM 'file:///private'>]><live protocol='1' command='frame'>&x;</live>"))));
   Test("Live protocol rejects unknown versions",()=>Reject(()=>LiveWire.Parse(Encoding.UTF8.GetBytes("<live protocol='99'/>"))));
   Test("Live default theme names normalize across MP and Studio",()=>{Check(LiveWire.ThemeKey("Skin default")==""&&LiveWire.ThemeKey(null)==""&&LiveWire.ThemeKey("default")==""&&LiveWire.ThemeKey("Night")=="Night","Native default theme mismatch.");});
   Test("Live framing rejects oversized and negative lengths",()=>{foreach(int length in new[]{-1,0,LiveWire.MaximumBytes+1})using(var stream=new MemoryStream(BitConverter.GetBytes(length)))Reject(()=>LiveWire.Read(stream));});
   Test("Live framing rejects truncated payload",()=>{using(var stream=new MemoryStream(new byte[]{5,0,0,0,60}))Reject(()=>LiveWire.Read(stream));});
   Test("Live relative paths reject traversal and rooted files",()=>{foreach(string path in new[]{"../file.xml","Media/../../outside","C:/outside.xml","/outside.xml","Media//a.png"})Reject(()=>LiveWire.SafeRelative(root,path));Check(LiveWire.SafeRelative(root,"Themes/Test/a.xml").StartsWith(Path.GetFullPath(root)),"Valid path rejected.");});
   string fixture=Path.Combine(root,"live-fixture");Directory.CreateDirectory(fixture);Write(fixture,"main.xml","<window><id>0</id><controls><control><type>label</type><id>0</id><description>First</description><posX>20</posX><posY>30</posY><label>First</label></control><control><type>label</type><id>0</id><description>Second</description><posX>40</posX><posY>60</posY><label>Second</label></control></controls></window>");
   Func<SkinProject> open=()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");return p;};
   Test("Live duplicate zero IDs use descriptions rather than first match",()=>{var p=open();var b=new LiveBinding(p,LiveSnapshot(p));Check(b.Matches.Count==2&&b.Matches.Values.SelectMany(v=>v).Select(v=>v.Token).Distinct().Count()==2,"ID zero linked incorrectly.");});
   Test("Live rejects different skin, theme and active page",()=>{var p=open();foreach(string field in new[]{"skin","theme","page","window"}){var snapshot=LiveSnapshot(p);snapshot.DocumentElement.SetAttribute(field,field=="skin"?Path.Combine(root,"other"):field=="window"?"99":"other");Reject(()=>new LiveBinding(p,snapshot));}});
   Test("Live unchanged model produces no overrides",()=>{var p=open();var b=new LiveBinding(p,LiveSnapshot(p));Check(b.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1).DocumentElement.ChildNodes.Count==0,"Unchanged controls were transmitted.");});
   Test("Live pending input previews without applying XML",()=>{var p=open();var before=p.Sources.Values.Single().Text;var b=new LiveBinding(p,LiveSnapshot(p));var drafts=new Dictionary<string,Dictionary<string,string>>{{p.Controls[0].Key,Set("label","Draft text")}};var frame=b.Frame(p,drafts,2);Check(frame.SelectSingleNode("/live/control/set[@key='label']").InnerText=="Draft text"&&p.Sources.Values.Single().Text==before,"Draft altered XML or was omitted.");});
   Test("Live applied edit and Undo return to empty frame",()=>{var baseline=open();var p=open();var b=new LiveBinding(baseline,LiveSnapshot(baseline));p.Edit(p.Controls[0],Set("posX","230"),"Move");Check(b.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1).SelectNodes("/live/control/set").Count==1,"Move missing.");p.UndoChange();Check(b.Frame(p,new Dictionary<string,Dictionary<string,string>>(),2).DocumentElement.ChildNodes.Count==0,"Undo retains override.");});
   Test("Live ambiguous runtime candidates require explicit linking",()=>{var p=open();var s=LiveSnapshot(p);s.DocumentElement.AppendChild(s.DocumentElement.FirstChild.CloneNode(true));s.DocumentElement.LastChild.Attributes["token"].Value="ambiguous";var b=new LiveBinding(p,s);Check(!b.Matches.ContainsKey(p.Controls[0].Key),"Ambiguous candidate silently selected.");var current=open();current.Edit(current.Controls[0],Set("label","Change"),"Edit");Reject(()=>b.Frame(current,new Dictionary<string,Dictionary<string,string>>(),1));b.Link(current.Controls[0],b.Items[0]);Check(b.Frame(current,new Dictionary<string,Dictionary<string,string>>(),2).SelectNodes("/live/control/set").Count==1,"Explicit link failed.");});
   Test("Live unsupported native fields are reported before sending",()=>{var p=open();var s=LiveSnapshot(p);((XmlElement)s.SelectSingleNode("/live/control/value[@key='font']")).SetAttribute("writable","false");var b=new LiveBinding(p,s);Reject(()=>b.Frame(p,new Dictionary<string,Dictionary<string,string>>{{p.Controls[0].Key,Set("font","font10")}},1));});
   Test("Live image insertion does not invent a label property",()=>{var baseline=open();var p=open();var b=new LiveBinding(baseline,LiveSnapshot(baseline));p.InsertControl("main.xml","image");var frame=b.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1);Check(frame.SelectNodes("/live/create/control").Count==1&&frame.SelectNodes("/live/create/control/label").Count==0,"Image template contains an unsupported label.");});
   Test("Live text insertion uses a unique bounded token and full placement",()=>{var baseline=open();var p=open();var b=new LiveBinding(baseline,LiveSnapshot(baseline));p.InsertControl("main.xml","label");var frame=b.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1);Check(frame.SelectNodes("/live/create/control/label").Count==1&&frame.SelectSingleNode("/live/create").Attributes["token"].Value.Length==68&&frame.SelectNodes("/live/place").Count==3,"Text insertion or layer placement missing.");});
   Test("Live source handshake catches editor/disk baseline drift",()=>{var baseline=open();var p=open();var b=new LiveBinding(baseline,LiveSnapshot(baseline));var verify=b.Verify(p);Check(verify.SelectNodes("/live/file").Count==1&&verify.SelectSingleNode("/live/file").Attributes["sha256"].Value.Length==64,"Hash handshake missing.");Write(fixture,"main.xml",p.Sources.Values.Single().Text.Replace("First","Different"));Reject(()=>b.Verify(open()));});
  }
 }
}
