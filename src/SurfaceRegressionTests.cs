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
            Test("Button focus textures follow simulated focus independently of designer selection",()=>
            {
                var p=AuditFixture(root,"button-preview-focus","<control><type>button</type><id>7</id><textureFocus>green.png</textureFocus><textureNoFocus>red.png</textureNoFocus><label>-</label></control><control><type>button</type><id>8</id><textureFocus>green.png</textureFocus><textureNoFocus>red.png</textureNoFocus><label>-</label></control>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));
                foreach(var pair in new[]{new{File="green.png",Color=Color.Lime},new{File="red.png",Color=Color.Red}})
                    using(var texture=new Bitmap(4,4))using(var g=Graphics.FromImage(texture)){g.Clear(pair.Color);texture.Save(Path.Combine(p.Root,"Media",pair.File));}
                var state=new PreviewState(p){FocusId=7};string signature=ProjectFile.ContentSignature(p);
                using(var canvas=new SkinCanvas{Project=p,State=state,Selected=p.Controls[1]})using(var image=new Bitmap(80,30))using(var g=Graphics.FromImage(image))
                {
                    canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,30,30));canvas.ControlSurfaceForTest(g,p.Controls[1],new RectangleF(40,0,30,30));
                    Check(image.GetPixel(10,10).G>240&&image.GetPixel(50,10).R>240,"Editor selection displaced simulated focus.");
                    canvas.FocusPreview=false;canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,30,30));Check(image.GetPixel(10,10).R>240,"Disabling focus preview still draws the focus texture.");
                }
                Check(ProjectFile.ContentSignature(p)==signature&&!p.Dirty,"Drawing focus modified project state.");
            });
            Test("Standalone labels use native center and right anchors without invented extents",()=>
            {
                var p=AuditFixture(root,"label-anchors","<control><type>label</type><posX>200</posX><posY>100</posY><align>center</align><label>0%</label></control>");var c=p.Controls[0];
                var area=SurfaceGeometry.LabelArea(c,LayoutTools.Bounds(c),new SizeF(40,20));Check(area.X==180&&area.Y==90&&area.Width==41,"Zero-size centered label used designer fallback dimensions.");
                p.Edit(c,Set("align","right"),"Align");c=p.Controls[0];area=SurfaceGeometry.LabelArea(c,LayoutTools.Bounds(c),new SizeF(40,20));Check(area.X==160&&area.Y==100,"Right label did not use its X coordinate as the right anchor.");
                p.Edit(c,new System.Collections.Generic.Dictionary<string,string>{{"align","center"},{"width","100"},{"height","40"}},"Extent");c=p.Controls[0];area=SurfaceGeometry.LabelArea(c,LayoutTools.Bounds(c),new SizeF(40,20));Check(area.X==230&&area.Y==110,"Explicit centered extents were ignored.");
            });
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
        internal void ControlSurfaceForTest(Graphics g,SkinControl c,RectangleF rect){DrawControl(g,c,rect);}
        internal void BeginTextPassForTest(){pendingSurfaceText=new System.Collections.Generic.List<PendingSurfaceText>();}
        internal void FlushTextPassForTest(Graphics g)
        {
            var pending=pendingSurfaceText;
            try{FlushSurfaceText(g);}finally{foreach(var item in pending)item.Clip.Dispose();pendingSurfaceText=null;}
        }
    }
}
