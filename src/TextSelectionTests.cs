using System;
using System.Linq;
using System.Drawing;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunTextSelectionTests(string root)
        {
            Test("Single-line text selection excludes adjacent rows and preserves source dimensions",()=>
            {
                var p=AuditFixture(root,"text-selection-rows","<control><type>label</type><posX>632</posX><posY>274</posY><font>font11</font><label>Album Artists</label></control><control><type>label</type><posX>632</posX><posY>302</posY><font>font11</font><label>Artists</label></control>");p.FontSpecs["font11"]=new SkinFontSpec{Family="Segoe UI",Height=11};string xml=ProjectFile.XmlSignature(p);
                using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p)})using(var bitmap=new Bitmap(100,100))using(var g=Graphics.FromImage(bitmap))
                {var a=canvas.SelectionBoundsForTest(g,p.Controls[0]);var b=canvas.SelectionBoundsForTest(g,p.Controls[1]);Check(a.Height<28&&a.Bottom<b.Top&&a.Width<239&&canvas.BoundsOf(p.Controls[0]).Height==64,"Synthetic field extents leaked into text selection.");Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Controls.All(c=>c.Get("height")==""),"Selection generated XML dimensions.");}
            });
            Test("Text selection retains explicit clipping anchors and non-text control frames",()=>
            {
                var p=AuditFixture(root,"text-selection-anchors","<control><type>label</type><posX>400</posX><posY>100</posY><width>30</width><height>80</height><align>right</align><valign>bottom</valign><label>A long caption</label></control><control><type>button</type><width>200</width><height>90</height></control>");
                using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p)})using(var bitmap=new Bitmap(100,100))using(var g=Graphics.FromImage(bitmap))
                {var a=canvas.SelectionBoundsForTest(g,p.Controls[0]);Check(a.Width<=32&&a.Height<80&&a.Y>125&&a.Bottom<=182,"Text clipping/vertical anchor changed.");Check(canvas.SelectionBoundsForTest(g,p.Controls[1])==canvas.BoundsOf(p.Controls[1]),"Button surface became a caption-only target.");}
            });
        }
    }
    public sealed partial class SkinCanvas
    {internal RectangleF SelectionBoundsForTest(Graphics graphics,SkinControl control){return SelectionBounds(graphics,control);}}
}
