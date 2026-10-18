using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunSurfaceRegressionTests(string root)
        {
            Test("Included title text survives a later opaque background texture",()=>
            {
                var p=AuditFixture(root,"deferred-header","<import>header.xml</import><control><type>image</type><width>240</width><height>180</height><texture>black.png</texture></control>");
                Write(p.Root,"references.xml","<controls><skin><width>240</width><height>180</height></skin></controls>");
                Write(p.Root,"fonts.xml","<fonts><font><name>title</name><filename>Segoe UI</filename><height>64</height></font></fonts>");
                Write(p.Root,"header.xml","<window><controls><control><type>label</type><posX>10</posX><posY>10</posY><width>210</width><height>50</height><font>title</font><label>Visible title</label><textcolor>ffffff</textcolor></control></controls></window>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));using(var texture=new Bitmap(8,8))using(var g=Graphics.FromImage(texture)){g.Clear(Color.Black);texture.Save(Path.Combine(p.Root,"Media","black.png"));}
                p=new SkinProject(p.Root);p.OpenPage("main.xml");string signature=ProjectFile.ContentSignature(p);
                using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p),Grid=false,Size=new Size(294,250)})using(var output=new Bitmap(294,250))
                {
                    canvas.DrawToBitmap(output,new Rectangle(0,0,294,250));int white=0;
                    for(int y=45;y<95;y++)for(int x=37;x<247;x++){var color=output.GetPixel(x,y);if(color.R>180&&color.G>180&&color.B>180)white++;}
                    Check(white>60&&output.GetPixel(240,180).R<5,"Opaque background erased imported text or the background was not rendered.");
                }
                Check(signature==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Rendering changed the project.");
            });
            Test("Deferred collection text retains its collection clip",()=>
            {
                var p=CollectionDemo();var c=CollectionControl(p,"listcontrol");
                using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p)})using(var output=new Bitmap(240,150))using(var g=Graphics.FromImage(output))
                {
                    g.SetClip(new Rectangle(40,20,70,25));canvas.BeginTextPassForTest();
                    canvas.SurfaceTextForTest(g,c,"Text must remain clipped to this viewport",new RectangleF(0,0,220,130));
                    g.ResetClip();canvas.FlushTextPassForTest(g);bool drawn=false;
                    for(int y=0;y<150;y++)for(int x=0;x<240;x++)if(output.GetPixel(x,y).A>0){drawn=true;Check(x>=40&&x<110&&y>=20&&y<45,"Deferred text escaped its original clip.");}
                    Check(drawn,"Deferred text was not rendered.");
                }
            });
        }
    }
    public sealed partial class SkinCanvas
    {
        internal void BeginTextPassForTest(){pendingSurfaceText=new System.Collections.Generic.List<PendingSurfaceText>();}
        internal void FlushTextPassForTest(Graphics g)
        {
            var pending=pendingSurfaceText;
            try{FlushSurfaceText(g);}finally{foreach(var item in pending)item.Clip.Dispose();pendingSurfaceText=null;}
        }
    }
}
