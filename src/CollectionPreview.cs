using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class PreviewItem
    {public string Title="",Detail="",Image="";public PreviewItem Copy(){return new PreviewItem{Title=Title,Detail=Detail,Image=Image};}}
    public sealed class CollectionSettings
    {
        public static readonly string[] Modes={"auto","list","smallicons","largeicons","filmstrip","album","playlist","coverflow"};
        public string Mode="auto";public int Focus;
        public readonly List<PreviewItem> Items=new List<PreviewItem>{new PreviewItem{Title="A movie for tonight",Detail="2026 · 112 Min.",Image="Studio/Icon-Movie.png"},new PreviewItem{Title="Favorite series",Detail="Season 2",Image="Studio/Icon-Movie.png"},new PreviewItem{Title="A new discovery",Detail="2025 · 98 Min.",Image="Studio/Icon-Pictures.png"},new PreviewItem{Title="My collection",Detail="12 items",Image="Studio/Icon-Music.png"},new PreviewItem{Title="An unusually long title for narrow text fields",Detail="2024 · 141 Min.",Image="Studio/Icon-Movie.png"},new PreviewItem{Title="Music and concerts",Detail="Live",Image="Studio/Icon-Music.png"}};
        public void Validate()
        {
            if(!Modes.Contains(Mode)||Items.Count>50||Focus<0||Focus>49)throw new InvalidDataException("Collections require at most 50 items, a valid view and selection from 1 to 50.");
            foreach(var item in Items)
            {if(item==null||item.Title==null||item.Detail==null||item.Image==null||item.Title.Length>512||item.Detail.Length>256||item.Image.Length>512)throw new InvalidDataException("Collection item is too long.");foreach(string s in new[]{item.Title,item.Detail,item.Image}){XmlConvert.VerifyXmlChars(s);if(s.IndexOfAny(new[]{'\r','\n','\t','|'})>=0)throw new InvalidDataException("Enter items without line breaks, tabs or vertical bars.");}if(item.Image.Length>0&&(Path.IsPathRooted(item.Image)||item.Image.Split('/','\\').Contains("..")))throw new InvalidDataException("Specify sample images relative to the Media directory.");}
        }
        public int SelectedIndex {get{return Items.Count==0?-1:Math.Min(Focus,Items.Count-1);}}
        public CollectionSettings Copy(){Validate();var copy=new CollectionSettings{Mode=Mode,Focus=Focus};copy.Items.Clear();copy.Items.AddRange(Items.Select(i=>i.Copy()));return copy;}
        public void SetFrom(CollectionSettings value){var copy=value.Copy();Mode=copy.Mode;Focus=copy.Focus;Items.Clear();Items.AddRange(copy.Items);}
        public string Signature(){Validate();var b=new StringBuilder(Mode+":"+Focus);foreach(var item in Items)foreach(string s in new[]{item.Title,item.Detail,item.Image})b.Append('|').Append(s.Length).Append(':').Append(s);return b.ToString();}
        public static bool IsCollection(SkinControl c){return new[]{"listcontrol","playlistcontrol","thumbnailpanel","filmstrip","coverflow","facadeview"}.Contains(c.Get("type"));}
        public static string Kind(SkinControl c,string requested)
        {string type=c.Get("type");if(type=="thumbnailpanel")return requested=="largeicons"?"largeicons":"smallicons";if(type=="listcontrol"||type=="playlistcontrol")return c.Get("subtype")=="album"?"album":c.Get("subtype")=="playlist"||type=="playlistcontrol"?"playlist":"list";return type=="facadeview"?"list":type;}
        public static SkinControl Template(SkinProject p,SkinControl facade,string requested)
        {
            var choices=p.Controls.Where(c=>c.RuntimeParent==facade&&IsCollection(c)&&c.Get("type")!="facadeview").ToArray();
            if(choices.Length==0)return null;
            return choices.FirstOrDefault(c=>Kind(c,requested)==(requested=="auto"?"list":requested))??choices.First();
        }
        public static bool Active(SkinProject p,SkinControl c)
        {var current=c;while(current.RuntimeParent!=null){var parent=current.RuntimeParent;if(parent.Get("type")=="facadeview")return Template(p,parent,p.Editor.Collection.Mode)==current;current=parent;}return true;}
        public static string EffectiveMode(SkinProject p)
        {var facade=p.Controls.FirstOrDefault(c=>c.Get("type")=="facadeview"&&c.Get("id")=="50")??p.Controls.FirstOrDefault(c=>c.Get("type")=="facadeview");var template=facade==null?null:Template(p,facade,p.Editor.Collection.Mode);return template==null?(p.Editor.Collection.Mode=="auto"?"list":p.Editor.Collection.Mode):Kind(template,p.Editor.Collection.Mode);}
    }
    public sealed class CollectionTile
    {public int Index;public RectangleF Bounds,Image,Title,Detail;public bool Selected;}
    public static class CollectionGeometry
    {
        static float Size(SkinControl c,string key,float fallback){float v=c.Number(key,fallback);return Single.IsNaN(v)||Single.IsInfinity(v)?fallback:Math.Max(1,Math.Min(10000,v));}
        static float Offset(SkinControl c,string key,float fallback){float v=c.Number(key,fallback);return Single.IsNaN(v)||Single.IsInfinity(v)?fallback:Math.Max(-10000,Math.Min(10000,v));}
        static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
        public static bool ValidBounds(RectangleF rect){return Finite(rect.X)&&Finite(rect.Y)&&Finite(rect.Width)&&Finite(rect.Height)&&rect.Width>0&&rect.Height>0;}
        public static int Columns(SkinControl c,RectangleF rect,CollectionSettings settings)
        {
            if(!ValidBounds(rect))return 0;
            string kind=CollectionSettings.Kind(c,settings.Mode);float width=Size(c,kind=="coverflow"?"cardWidth":kind=="largeicons"?"itemWidthBig":"itemWidth",kind=="coverflow"?230:200);
            return (int)Math.Max(1,Math.Min(50,Math.Floor(rect.Width/width)));
        }
        public static CollectionTile[] Tiles(SkinControl c,RectangleF rect,CollectionSettings settings)
        {
            var result=new List<CollectionTile>();if(!ValidBounds(rect)||settings.Items.Count==0)return result.ToArray();string kind=CollectionSettings.Kind(c,settings.Mode);int selected=settings.SelectedIndex;
            if(kind=="list"||kind=="playlist"||kind=="album")
            {
                float height=Size(c,"textureHeight",64),gap=Math.Max(0,Math.Min(1000,Offset(c,"spaceBetweenItems",2)));int visible=(int)Math.Max(1,Math.Min(50,Math.Floor((rect.Height+gap)/(height+gap))));int start=Math.Max(0,selected-visible+1);
                for(int i=start;i<settings.Items.Count&&i-start<visible;i++)
                {var r=new RectangleF(rect.X,rect.Y+(i-start)*(height+gap),rect.Width,height);float x=Offset(c,"textXOff",20),y=Offset(c,"textYOff",0),x2=Offset(c,"textXOff2",rect.Width-160),y2=Offset(c,"textYOff2",y);result.Add(new CollectionTile{Index=i,Selected=i==selected,Bounds=r,Image=new RectangleF(r.X+Offset(c,"IconXOff",8),r.Y+Offset(c,"IconYOff",5),Size(c,"itemWidth",0),Size(c,"itemHeight",0)),Title=new RectangleF(r.X+x,r.Y+y,Math.Max(1,Math.Min(r.Width-x,x2-x-12)),Math.Max(1,height-y)),Detail=new RectangleF(r.X+x2,r.Y+y2,Math.Max(1,r.Width-x2),Math.Max(1,height-y2))});}
            }
            else
            {
                bool flow=kind=="coverflow",film=kind=="filmstrip",big=kind=="largeicons";float w=Size(c,flow?"cardWidth":big?"itemWidthBig":"itemWidth",flow?230:200),h=Size(c,flow?"cardHeight":big?"itemHeightBig":"itemHeight",flow?300:250);
                int columns=Columns(c,rect,settings),rows=flow||film?1:(int)Math.Max(1,Math.Min(50,Math.Floor(rect.Height/h)));int count=Math.Max(1,Math.Min(50,columns*rows));int start=flow?Math.Max(0,selected-(count-1)/2):film?Math.Max(0,selected-count+1):Math.Max(0,(selected/count)*count);
                for(int i=start;i<settings.Items.Count&&i-start<count;i++)
                {float x=rect.X+(i-start)%columns*w,y=rect.Y+(i-start)/columns*h,drawW=w,drawH=h;if(flow){int offset=i-selected;drawW=w*(offset==0?1:.65f);drawH=h*(offset==0?1:.82f);x=rect.X+rect.Width/2-w/2+offset*(w*.65f+Math.Max(0,Offset(c,"sideGap",20)));y=rect.Y+Math.Max(0,(rect.Height-h)/2)+(offset==0?0:h*.09f);}
                    var r=new RectangleF(x,y,drawW,drawH);float tx=flow?0:Offset(c,big?"thumbPosXBig":"thumbPosX",8),ty=flow?0:Offset(c,big?"thumbPosYBig":"thumbPosY",8);float tw=flow?drawW:Size(c,big?"thumbWidthBig":"thumbWidth",Math.Max(1,drawW-16)),th=flow?Math.Max(1,drawH-42):Size(c,big?"thumbHeightBig":"thumbHeight",Math.Max(1,drawH-55));
                    var title=new RectangleF(x+5,y+ty+th+3,Math.Max(1,drawW-10),Math.Max(1,drawH-th-ty-3));
                    // MP1 filmstrip labels start below the texture, not the thumbnail.
                    // Negative offsets are intentional and remain outside the viewport clip.
                    if(film){float textureHeight=Size(c,"textureHeight",80);title=new RectangleF(x+Offset(c,"textXOff",0),y+textureHeight+Offset(c,"textYOff",0),Size(c,"textureWidth",80),textureHeight);}
                    result.Add(new CollectionTile{Index=i,Selected=i==selected,Bounds=r,Image=new RectangleF(x+tx,y+ty,tw,th),Title=title,Detail=RectangleF.Empty});}
            }
            return result.ToArray();
        }
    }
    public sealed partial class PreviewState
    {
        public void UpdateCollectionValues()
        {
            var baseline=PreviewSettings.BaseValues();
            foreach(string key in new[]{"#itemcount","#selecteditem","#selecteditem2","#selectedindex","#selectedthumb"})
            {string value;if(Project.Editor.Preview.Values.TryGetValue(key,out value)||baseline.TryGetValue(key,out value))Values[key]=value;else Values.Remove(key);}
            if(!Project.Controls.Any(CollectionSettings.IsCollection))return;var settings=Project.Editor.Collection;int index=settings.SelectedIndex;var values=new Dictionary<string,string>{{"#itemcount",settings.Items.Count.ToString(CultureInfo.InvariantCulture)},{"#selecteditem",index<0?"":settings.Items[index].Title},{"#selecteditem2",index<0?"":settings.Items[index].Detail},{"#selectedindex",(index+1).ToString(CultureInfo.InvariantCulture)},{"#selectedthumb",index<0?"":settings.Items[index].Image}};
            foreach(var pair in values)if(!Project.Editor.Preview.Values.ContainsKey(pair.Key))Values[pair.Key]=pair.Value;
        }
    }
    public static partial class ProjectFile
    {
        static void SaveCollectionSettings(CollectionSettings settings,XmlElement state)
        {settings.Validate();var doc=state.OwnerDocument;var node=doc.CreateElement("collection");node.SetAttribute("mode",settings.Mode);node.SetAttribute("focus",settings.Focus.ToString(CultureInfo.InvariantCulture));state.AppendChild(node);foreach(var entry in settings.Items){var item=doc.CreateElement("item");item.SetAttribute("title",entry.Title);item.SetAttribute("detail",entry.Detail);item.SetAttribute("image",entry.Image);node.AppendChild(item);}}
        static void LoadCollectionSettings(CollectionSettings settings,XmlElement state)
        {var nodes=state.SelectNodes("collection");if(nodes.Count==0)return;if(nodes.Count!=1)throw new InvalidDataException("Duplicate collection state.");var node=(XmlElement)nodes[0];int focus;if(!Int32.TryParse(node.GetAttribute("focus"),out focus))throw new InvalidDataException("Invalid collection selection.");var loaded=new CollectionSettings{Mode=node.GetAttribute("mode"),Focus=focus};loaded.Items.Clear();if(node.SelectNodes("item").Count>50)throw new InvalidDataException("Too many collection items.");foreach(XmlElement item in node.SelectNodes("item"))loaded.Items.Add(new PreviewItem{Title=item.GetAttribute("title"),Detail=item.GetAttribute("detail"),Image=item.GetAttribute("image")});settings.SetFrom(loaded);}
    }
}
