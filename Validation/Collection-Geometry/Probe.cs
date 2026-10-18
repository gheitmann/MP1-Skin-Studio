using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MP1SkinStudio;

// Pure geometry regression checks: no forms, canvas or application entry point.
public static class CollectionGeometryProbe
{
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS: "+name);}
    public static int Main(string[] args)
    {
        try
        {
            string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
            string fixture=Path.Combine(output,"fixture");Directory.CreateDirectory(fixture);
            File.WriteAllText(Path.Combine(fixture,"main.xml"),"<window><controls><control><type>filmstrip</type><id>50</id><posX>100</posX><posY>200</posY><width>900</width><height>400</height><itemWidth>240</itemWidth><itemHeight>327</itemHeight><textureWidth>230</textureWidth><textureHeight>327</textureHeight><thumbWidth>200</thumbWidth><thumbHeight>300</thumbHeight><thumbPosX>6</thumbPosX><thumbPosY>5</thumbPosY><textXOff>17</textXOff><textYOff>12</textYOff></control></controls></window>");
            var p=new SkinProject(fixture);p.OpenPage("main.xml");var c=p.Controls.Single();string original=c.Source.Text;
            var tile=CollectionGeometry.Tiles(c,LayoutTools.Bounds(c),p.Editor.Collection).First();
            Check(tile.Title.X==117&&tile.Title.Y==539,"Filmstrip text offsets use the item origin and texture height, independently of thumbnail bounds");
            Check(tile.Title.Width==230&&tile.Title.Height==327,"Filmstrip label dimensions use texture dimensions");
            Check(tile.Image==new RectangleF(106,205,200,300),"Filmstrip thumbnail geometry remains independent");
            Check(c.Source.Text==original&&!p.Dirty,"Calculating geometry does not rewrite XML");
            p.Edit(c,new System.Collections.Generic.Dictionary<string,string>{{"textYOff","-2000"}},"Offscreen title");c=p.Controls.Single();
            tile=CollectionGeometry.Tiles(c,LayoutTools.Bounds(c),p.Editor.Collection).First();
            Check(tile.Title.Bottom<LayoutTools.Bounds(c).Top,"Negative text offsets keep intentionally hidden labels outside the clipped viewport");
            var real=new SkinProject(Path.GetFullPath(args[1]));real.OpenPage("myvideo.xml");
            var film=real.Controls.Single(x=>x.Get("type")=="filmstrip"&&x.Source.RelativePath=="common.facade.video.default.xml");
            var rows=CollectionGeometry.Tiles(film,LayoutTools.Bounds(film),real.Editor.Collection);
            Check(rows.All(t=>t.Title.Bottom<LayoutTools.Bounds(film).Top),"Real Titan filmstrip suppresses offscreen labels through its original negative offset");
            Console.WriteLine(checks+" geometry checks passed.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
    }
}
