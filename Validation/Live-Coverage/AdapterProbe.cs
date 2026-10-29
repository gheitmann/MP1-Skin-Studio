using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using MediaPortal.GUI.Library;
using MP1SkinStudio.LiveBridge;
using MP1SkinStudio.Live;
class AdapterProbe
{
 static readonly List<string> results=new List<string>();
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);results.Add("PASS "+name);}
 static FieldInfo Field(Type type,string name){for(var t=type;t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly);if(f!=null)return f;}throw new Exception("SDK field missing: "+name);}
 static void Reject(System.Action action,string name){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,name);}
 static void Scalar(GUIControl control,string key,string nativeField,string input,object expected)
 {
  var slots=RuntimeProperty.For(control,"C:\\Skin");var slot=slots[key];var original=slot.Read();var field=Field(control.GetType(),nativeField);object before=field.GetValue(control);slot.Write(slot.Parse(input));Check(Object.Equals(field.GetValue(control),expected),control.GetType().Name+" "+key+" changes the intended native field");slot.Write(original);Check(Object.Equals(field.GetValue(control),before),control.GetType().Name+" "+key+" restores its exact native value");
 }
 [STAThread]static int Main(string[] args)
 {
  try
  {
   Directory.CreateDirectory(args[0]);GUIGraphicsContext.form=new System.Windows.Forms.Form{ClientSize=new Size(1280,720)};GUIGraphicsContext.SkinSize=new Size(1920,1080);GUIGraphicsContext.OverScanWidth=1280;GUIGraphicsContext.OverScanHeight=720;
   var list=new GUIListControl(0);var rowBefore=list.ItemHeight;
   Scalar(list,"itemHeight","_imageHeight","60",GUIGraphicsContext.ScaleVertical(60));Check(list.ItemHeight==rowBefore,"list icon height does not overwrite row height");
   Scalar(list,"itemWidth","_imageWidth","90",GUIGraphicsContext.ScaleHorizontal(90));Scalar(list,"textureHeight","_itemHeight","80",GUIGraphicsContext.ScaleVertical(80));Scalar(list,"spaceBetweenItems","_spaceBetweenItems","12",GUIGraphicsContext.ScaleVertical(12));Scalar(list,"textXOff","_textOffsetX","-6",GUIGraphicsContext.ScaleHorizontal(-6));Scalar(list,"textcolor","_textColor","ff66aa22",0xff66aa22L);
   Check(list.NeedRefresh(),"list updates request native layout refresh");Reject(()=>RuntimeProperty.For(list,"C:\\Skin")["textureHeight"].Parse("-1"),"negative row dimensions are rejected");Reject(()=>RuntimeProperty.For(list,"C:\\Skin")["spaceBetweenItems"].Parse("-1"),"negative row spacing is rejected");
   var scroll=new GUITextScrollUpControl(0);var slots=RuntimeProperty.For(scroll,"C:\\Skin");Check(slots.ContainsKey("label")&&slots.ContainsKey("font")&&slots.ContainsKey("textcolor"),"scrolling textbox exposes label, font and color together");Scalar(scroll,"textcolor","_textColor","ffaa4400",0xffaa4400L);
   // Empty text exercises setter/reset state without requiring a font renderer.
   var contains=Field(scroll.GetType(),"_containsProperty");scroll.Property="#original.binding";contains.SetValue(scroll,true);object original=slots["label"].Read();slots["label"].Write(slots["label"].Parse(""));Check(scroll.Property==""&&!(bool)contains.GetValue(scroll),"scrolling text replacement clears a previous dynamic-binding flag");slots["label"].Write(original);Check(scroll.Property=="#original.binding"&&(bool)contains.GetValue(scroll),"scrolling text restore retains the original dynamic binding");
   var name=Field(scroll.GetType(),"_fontName");name.SetValue(scroll,"Original font alias");var fontBefore=slots["font"].Read();scroll.Property="";name.SetValue(scroll,"Temporary alias");slots["font"].Write(fontBefore);Check((string)name.GetValue(scroll)=="Original font alias","font restoration retains the exact native font alias");Reject(()=>slots["font"].Parse("not-a-loaded-font"),"an unavailable live font is rejected");
   var strip=new GUIFilmstripControl(0);Scalar(strip,"textureWidth","_textureWidth","240",GUIGraphicsContext.ScaleHorizontal(240));Scalar(strip,"textureHeight","_textureHeight","160",GUIGraphicsContext.ScaleVertical(160));Scalar(strip,"thumbWidth","_thumbNailWidth","180",GUIGraphicsContext.ScaleHorizontal(180));Scalar(strip,"thumbHeight","_thumbNailHeight","120",GUIGraphicsContext.ScaleVertical(120));
   var thumbs=new GUIThumbnailPanel(0);Scalar(thumbs,"keepaspectratio","_keepAspectRatio","no",false);Scalar(thumbs,"textcolor","_textColor","ff3399ff",0xff3399ffL);
   foreach(var collection in new GUIControl[]{list,strip,thumbs})
   {
    var items=(List<GUIListItem>)Field(collection.GetType(),"_listItems").GetValue(collection);var item=new GUIListItem("Keep native list data");items.Add(item);Scalar(collection,"itemWidth",collection is GUIListControl?"_imageWidth":"_itemWidth","200",GUIGraphicsContext.ScaleHorizontal(200));Scalar(collection,"itemHeight",collection is GUIListControl?"_imageHeight":"_itemHeight","100",GUIGraphicsContext.ScaleVertical(100));RuntimeProperty.ReflowLayout(collection);Check(items.Count==1&&Object.ReferenceEquals(items[0],item)&&Object.ReferenceEquals(Field(collection.GetType(),"_listItems").GetValue(collection),items),"geometry keeps original native list items in "+collection.GetType().Name);
   }
   var group=new GUIGroup(0);var align=RuntimeProperty.For(group,"C:\\Skin")["align"];var beforeAlign=align.Read();align.Write(align.Parse("right"));Check(group.GroupAlignment.ToString().Contains("RIGHT"),"group alignment uses the native group property");align.Write(beforeAlign);Check(Object.Equals(group.GroupAlignment,beforeAlign),"group alignment restores exactly");
   foreach(var control in new GUIControl[]{list,scroll,strip,thumbs,group,new GUILabelControl(0),new GUICheckMarkControl(0)})Check(RuntimeProperty.SupportedFields(control.GetType()).SetEquals(RuntimeProperty.For(control,"C:\\Skin").Keys),"metadata audit matches actual slots for "+control.GetType().Name);
   list.Type="listcontrol";list.GetID=111;strip.Type="filmstrip";strip.GetID=112;scroll.Type="textboxscrollup";scroll.GetID=113;
   GUIGraphicsContext.Skin="C:\\Skin";var window=new GUIWindow();typeof(GUIWindow).GetField("_windowLoaded",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,true);typeof(GUIWindow).GetField("_windowXmlFileName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,"C:\\Skin\\test.xml");window.Children.Add(list);window.Children.Add(strip);window.Children.Add(scroll);
   var session=new RuntimeSession(()=>window);var snapshot=session.Handle(LiveWire.Document("snapshot"));string epoch=snapshot.DocumentElement.GetAttribute("epoch"),token=((XmlElement)snapshot.SelectSingleNode("/live/control[@id='111']")).GetAttribute("token");
   int originalRow=list.ItemHeight,originalIcon=(int)Field(list.GetType(),"_imageHeight").GetValue(list);var nativeItems=Field(list.GetType(),"_listItems").GetValue(list);
   var frame=LiveWire.Document("frame");LiveWire.Attr(frame.DocumentElement,"epoch",epoch);var node=LiveWire.Add(frame.DocumentElement,"control");LiveWire.Attr(node,"token",token);var row=LiveWire.Add(node,"set");LiveWire.Attr(row,"key","textureHeight");row.InnerText="80";var icon=LiveWire.Add(node,"set");LiveWire.Attr(icon,"key","itemHeight");icon.InnerText="60";session.Handle(frame);
   Check(list.ItemHeight==GUIGraphicsContext.ScaleVertical(80)&&(int)Field(list.GetType(),"_imageHeight").GetValue(list)==GUIGraphicsContext.ScaleVertical(60),"complete live frame keeps row and icon height distinct");Check(Object.ReferenceEquals(Field(list.GetType(),"_listItems").GetValue(list),nativeItems)&&list.Count==1,"complete frame retains native list data");
   var reset=LiveWire.Document("frame");LiveWire.Attr(reset.DocumentElement,"epoch",epoch);session.Handle(reset);Check(list.ItemHeight==originalRow&&(int)Field(list.GetType(),"_imageHeight").GetValue(list)==originalIcon&&list.Count==1,"empty frame restores native row and icon state with data intact");
   row.InnerText="0";Reject(()=>session.Handle(frame),"invalid compound collection frame is rejected atomically");Check(list.ItemHeight==originalRow&&(int)Field(list.GetType(),"_imageHeight").GetValue(list)==originalIcon&&list.Count==1,"invalid frame leaves native layout and data unchanged");session.Reset();
   Console.WriteLine(results.Count+" extended adapter checks passed.");return 0;
  }catch(Exception ex){results.Add(ex.ToString());return 1;}finally{File.WriteAllLines(Path.Combine(args[0],"Results.txt"),results);foreach(string line in results)Console.WriteLine(line);if(GUIGraphicsContext.form!=null)GUIGraphicsContext.form.Dispose();}
 }
}
