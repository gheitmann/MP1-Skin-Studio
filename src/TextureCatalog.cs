using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class TextureAsset
    {
        public string RelativePath,PhysicalPath;
        public bool FromTheme;
        public int Width,Height;
        public long Size;
    }
    public static class TextureCatalog
    {
        static readonly HashSet<string> extensions=new HashSet<string>(new[]{".png",".jpg",".jpeg",".bmp",".gif"},StringComparer.OrdinalIgnoreCase);
        public static List<TextureAsset> List(SkinProject project)
        {
            var found=new Dictionary<string,TextureAsset>(StringComparer.OrdinalIgnoreCase);
            Scan(project.SafePath("Media"),false,found);
            if(project.Theme.Length>0)Scan(project.SafePath("Themes/"+project.Theme+"/Media"),true,found);
            return found.Values.OrderBy(a=>a.RelativePath,StringComparer.OrdinalIgnoreCase).ToList();
        }
        static void Scan(string root,bool fromTheme,Dictionary<string,TextureAsset> found)
        {
            if(!Directory.Exists(root))return;var todo=new Stack<string>();todo.Push(root);
            while(todo.Count>0)
            {
                string folder=todo.Pop();
                if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;
                foreach(string path in Directory.GetFiles(folder))
                {
                    if(!extensions.Contains(Path.GetExtension(path))||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)continue;
                    string relative=path.Substring(root.TrimEnd('\\').Length+1).Replace('\\','/');
                    found[relative]=new TextureAsset{RelativePath=relative,PhysicalPath=path,FromTheme=fromTheme,Size=new FileInfo(path).Length};
                    if(found.Count>20000)throw new InvalidOperationException("More than 20,000 image files. Limit the skin directory to the required media.");
                }
                foreach(string child in Directory.GetDirectories(folder))todo.Push(child);
            }
        }
        public static Bitmap Thumbnail(TextureAsset asset,int width,int height,out string error)
        {
            error=null;
            try
            {
                if(new FileInfo(asset.PhysicalPath).Length>32L*1024*1024)throw new InvalidDataException("Image exceeds 32 MB.");
                using(var stream=new FileStream(asset.PhysicalPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                using(var image=Image.FromStream(stream,false,false))
                {
                    asset.Width=image.Width;asset.Height=image.Height;
                    if(image.Width>8192||image.Height>8192||(long)image.Width*image.Height>32000000)throw new InvalidDataException("Image dimensions are too large for previews.");
                    var result=new Bitmap(width,height,PixelFormat.Format32bppArgb);
                    using(var g=Graphics.FromImage(result))
                    {g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;float ratio=Math.Min(width/(float)image.Width,height/(float)image.Height);float w=image.Width*ratio,h=image.Height*ratio;g.DrawImage(image,(width-w)/2,(height-h)/2,w,h);}
                    return result;
                }
            }
            catch(Exception ex){error=ex.Message;return null;}
        }
    }
    public static class DemoAssets
    {
        public static void Generate(string root)
        {
            string media=Path.Combine(root,"Media","Studio");Directory.CreateDirectory(media);
            SavePanel(Path.Combine(media,"Button-blue.png"),470,130,Color.FromArgb(36,133,201),Color.FromArgb(15,64,104));
            SavePanel(Path.Combine(media,"Button-dark.png"),470,130,Color.FromArgb(43,66,87),Color.FromArgb(20,33,48));
            SavePanel(Path.Combine(media,"Button-turquoise.png"),470,130,Color.FromArgb(36,154,147),Color.FromArgb(12,64,79));
            SavePanel(Path.Combine(media,"Panel-transparent.png"),600,360,Color.FromArgb(190,36,66,96),Color.FromArgb(105,10,24,40));
            string[] names={"Icon-Movie.png","Icon-Music.png","Icon-Pictures.png"};
            for(int i=0;i<names.Length;i++)using(var bitmap=new Bitmap(200,200))
            {
                using(var g=Graphics.FromImage(bitmap))using(var pen=new Pen(Color.FromArgb(127,210,255),10))
                {
                    g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
                    if(i==0){g.DrawRectangle(pen,38,50,124,100);g.DrawLine(pen,58,50,58,150);g.DrawLine(pen,142,50,142,150);for(int y=65;y<150;y+=30){g.DrawLine(pen,40,y,54,y);g.DrawLine(pen,146,y,160,y);}}
                    else if(i==1){g.DrawLine(pen,80,60,80,145);g.DrawLine(pen,80,60,145,45);g.DrawLine(pen,145,45,145,127);g.DrawEllipse(pen,43,134,37,23);g.DrawEllipse(pen,108,116,37,23);}
                    else{g.DrawRectangle(pen,35,45,130,110);g.DrawEllipse(pen,116,64,18,18);g.DrawLines(pen,new[]{new Point(42,142),new Point(82,92),new Point(111,125),new Point(133,102),new Point(158,140)});}
                }
                bitmap.Save(Path.Combine(media,names[i]),ImageFormat.Png);
            }
        }
        static void SavePanel(string path,int width,int height,Color first,Color last)
        {
            using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(bitmap))
            using(var brush=new LinearGradientBrush(new Rectangle(0,0,width,height),first,last,65f))
            using(var pen=new Pen(Color.FromArgb(150,151,211,255),3))
            {g.Clear(Color.Transparent);g.FillRectangle(brush,0,0,width,height);g.DrawRectangle(pen,2,2,width-5,height-5);bitmap.Save(path,ImageFormat.Png);}
        }
    }
}
