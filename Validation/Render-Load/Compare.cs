using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class CompareRenderLoad
{
    static byte[] Pixels(Bitmap image)
    {
        var bits=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{var bytes=new byte[image.Width*image.Height*4];for(int row=0;row<image.Height;row++)Marshal.Copy(IntPtr.Add(bits.Scan0,row*bits.Stride),bytes,row*image.Width*4,image.Width*4);return bytes;}
        finally{image.UnlockBits(bits);}
    }
    static int Main(string[] args)
    {
        try
        {
            var before=File.ReadAllLines(Path.Combine(args[0],"Source-Mappings.txt"));var after=File.ReadAllLines(Path.Combine(args[1],"Source-Mappings.txt"));
            if(!before.SequenceEqual(after))throw new Exception("XML mapping fingerprints differ.");
            Console.WriteLine("PASS "+before.Length+" XML byte/span/DOM mappings identical to baseline.");
            var images=new[]{"myHome.xml","BasicHome.xml","MyMinesweeper.xml"}.SelectMany(page=>new[]{page+".png",page+"-changed.png",page+"-all.png"}).Select(name=>Path.Combine(args[0],name)).ToArray();if(images.Length!=9)throw new Exception("Expected nine baseline renders.");
            foreach(string file in images)using(var a=new Bitmap(file))using(var b=new Bitmap(Path.Combine(args[1],Path.GetFileName(file))))
            {if(a.Size!=b.Size||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Canvas pixels differ: "+Path.GetFileName(file));Console.WriteLine("PASS pixel-identical "+Path.GetFileName(file));}
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
