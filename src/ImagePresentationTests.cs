using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinControl ImageFixture(string root,string name,string properties)
        {return AuditFixture(root,name,"<control><type>image</type><id>1</id>"+properties+"</control>").Controls[0];}
        static void RunImagePresentationTests(string root)
        {
            Test("Aspect fit uses native left top defaults rather than invented centering",()=>
            {var c=ImageFixture(root,"image-fit-default","<keepaspectratio>yes</keepaspectratio>");var p=ImagePresentation.Plan(c,new RectangleF(10,20,100,100),new Size(400,100));Check(p.Valid&&p.Supported&&p.Target==new RectangleF(10,20,100,25)&&p.Source==new RectangleF(0,0,400,100),"Default fit alignment or source was changed.");});
            Test("Image horizontal and vertical alignment cover all static anchors",()=>
            {
                foreach(string align in new[]{"left","center","right"})foreach(string valign in new[]{"top","middle","bottom"})
                {var c=ImageFixture(root,"image-anchor-"+align+"-"+valign,"<keepaspectratio>yes</keepaspectratio><align>"+align+"</align><valign>"+valign+"</valign>");var wide=ImagePresentation.Plan(c,new RectangleF(10,20,100,100),new Size(400,100));var tall=ImagePresentation.Plan(c,new RectangleF(10,20,100,100),new Size(100,400));Check(wide.Target.Y==20+(valign=="bottom"?75:valign=="middle"?37.5f:0)&&tall.Target.X==10+(align=="right"?75:align=="center"?37.5f:0),"An image anchor did not position the contained image.");}
            });
            Test("Legacy centered and align rules retain native XML processing order",()=>
            {
                var right=ImageFixture(root,"image-center-first","<keepaspectratio>yes</keepaspectratio><centered>yes</centered><align>right</align>");var center=ImageFixture(root,"image-center-last","<keepaspectratio>yes</keepaspectratio><align>right</align><centered>yes</centered>");Check(ImagePresentation.Plan(right,new RectangleF(0,0,100,100),new Size(100,400)).Target.X==75&&ImagePresentation.Plan(center,new RectangleF(0,0,100,100),new Size(100,400)).Target.X==37.5f,"Legacy centered ignored source order.");
                var p=AuditFixture(root,"image-layered","<control Style='Right'><type>image</type><id>1</id><centered>no</centered><valign>bottom</valign></control>");Write(p.Root,"references.xml","<window><control><type>image</type><keepaspectratio>yes</keepaspectratio><centered>yes</centered></control><style Name='Right'><align>right</align></style></window>");p=new SkinProject(p.Root);p.OpenPage("main.xml");var plan=ImagePresentation.Plan(p.Controls[0],new RectangleF(0,0,100,100),new Size(100,400));Check(plan.Target.X==75&&plan.Target.Y==0,"Inherited centered=false reset axes that were not both centered.");
            });
            Test("Crop to fill chooses centered source pixels while top crop retains the source top",()=>
            {
                var c=ImageFixture(root,"image-crop-center","<keepaspectratio>yes</keepaspectratio><zoom>yes</zoom>");var center=ImagePresentation.Plan(c,new RectangleF(5,10,200,100),new Size(200,400));Check(center.Target==new RectangleF(5,10,200,100)&&center.Source==new RectangleF(0,150,200,100),"Center crop sampled the wrong source area.");
                var top=ImageFixture(root,"image-crop-top","<keepaspectratio>yes</keepaspectratio><zoomfromtop>yes</zoomfromtop><zoom>yes</zoom>");Check(ImagePresentation.Plan(top,new RectangleF(5,10,200,100),new Size(200,400)).Source==new RectangleF(0,0,200,100),"Top crop did not take precedence.");
                Check(ImagePresentation.Plan(c,new RectangleF(0,0,100,200),new Size(400,200)).Source==new RectangleF(150,0,100,200),"Landscape crop was not horizontally centered.");
            });
            Test("Stretch mode ignores crop flags until proportions are enabled",()=>
            {var c=ImageFixture(root,"image-stretch","<keepaspectratio>no</keepaspectratio><zoom>yes</zoom><zoomfromtop>yes</zoomfromtop><align>right</align>");var p=ImagePresentation.Plan(c,new RectangleF(10,20,150,75),new Size(80,300));Check(p.Target==new RectangleF(10,20,150,75)&&p.Source==new RectangleF(0,0,80,300),"Crop flags distorted stretch-mode source selection.");});
            Test("Image presentation definitions resolve without changing missing inputs into known states",()=>
            {
                var p=AuditFixture(root,"image-settings-define","<control><type>image</type><keepaspectratio>#aspect</keepaspectratio><align>#alignment</align></control>");var c=p.Controls[0];c.Defines["#aspect"]="yes";c.Defines["#alignment"]="right";Check(ImagePresentation.Plan(c,new RectangleF(0,0,100,100),new Size(100,400)).Target.X==75,"Scoped presentation definitions were not resolved.");c.Defines.Remove("#alignment");Check(!ImagePresentation.Supported(c)&&PreviewProblems.Run(p,new PreviewState(p)).Issues.Any(i=>i.Code=="PREVIEW_IMAGE_LAYOUT"&&i.Information),"Unknown image settings were silently accepted.");
            });
            Test("Image fit plans reject invalid extents and zero-pixel extreme crops",()=>
            {var c=ImageFixture(root,"image-bounds","<keepaspectratio>yes</keepaspectratio><zoom>yes</zoom>");foreach(var bounds in new[]{new RectangleF(0,0,0,10),new RectangleF(0,0,-1,10),new RectangleF(Single.NaN,0,10,10),new RectangleF(0,0,Single.PositiveInfinity,10),new RectangleF(0,0,100000,1)})Check(!ImagePresentation.Plan(c,bounds,new Size(1,100)).Valid,"Invalid/extreme crop was sent to GDI+.");Check(!ImagePresentation.Plan(c,new RectangleF(0,0,10,10),Size.Empty).Valid,"Zero-size source was accepted.");});
            Test("Collection frames retain their earlier static fit independently of image alignment rules",()=>
            {var c=ImageFixture(root,"image-collection-isolation","<keepaspectratio>yes</keepaspectratio><zoom>yes</zoom><align>right</align>");c.Properties["type"]=new PropertyValue("filmstrip","",true);var plan=ImagePresentation.Plan(c,new RectangleF(0,0,100,100),new Size(100,400));Check(plan.Target==new RectangleF(37.5f,0,25,100)&&plan.Source==new RectangleF(0,0,100,400),"Image crop changes leaked into collection frame geometry.");});
            Test("Production image rendering samples actual cropped pixels and preserves tint",()=>
            {
                var project=AuditFixture(root,"image-crop-canvas","<control><type>image</type><keepaspectratio>yes</keepaspectratio><zoom>yes</zoom><colordiffuse>ffffffff</colordiffuse></control>");
                using(var source=new Bitmap(20,80))using(var g=Graphics.FromImage(source))using(var canvas=new SkinCanvas{Project=project,State=new PreviewState(project)})using(var output=new Bitmap(40,40))using(var paint=Graphics.FromImage(output))
                {g.Clear(Color.Lime);g.FillRectangle(Brushes.Red,0,0,20,20);g.FillRectangle(Brushes.Blue,0,60,20,20);canvas.SurfaceImageForTest(paint,project.Controls[0],source,new RectangleF(0,0,40,40));Check(output.GetPixel(20,20).G>240&&output.GetPixel(20,5).G>240,"Production renderer did not use the center crop source.");project.Edit(project.Controls[0],Set("zoomfromtop","yes"),"Top crop");canvas.SurfaceImageForTest(paint,project.Controls[0],source,new RectangleF(0,0,40,40));Check(output.GetPixel(20,5).R>240,"Production renderer did not retain top source pixels.");}
            });
            Test("Image fitting edits roundtrip and export with exact undo while originals remain unchanged",()=>
            {
                var p=AuditFixture(root,"image-fit-save","<control><type>image</type><id>1</id><keepaspectratio>yes</keepaspectratio><centered>yes</centered><zoom>no</zoom></control>");string before=p.Controls[0].Source.Text;byte[] original=File.ReadAllBytes(p.Controls[0].Source.Path);p.Edit(p.Controls[0],new System.Collections.Generic.Dictionary<string,string>{{"zoom","yes"},{"zoomfromtop","yes"}},"Crop image");string after=p.Controls[0].Source.Text,file=Path.Combine(root,"image-fit.mp1skinproj"),zip=Path.Combine(root,"image-fit.zip");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(ImagePresentation.Plan(loaded.Controls[0],new RectangleF(0,0,100,100),new Size(100,400)).Source.Y==0,"Crop behavior changed after reopening.");loaded.Export(zip);using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after,"Export changed fitting XML.");loaded.UndoChange();Check(!loaded.Dirty&&loaded.Controls[0].Source.Text==before,"Restored fitting undo failed.");loaded.RedoChange();Check(loaded.Controls[0].Source.Text==after&&File.ReadAllBytes(loaded.Controls[0].Source.Path).SequenceEqual(original),"Redo changed source files.");
            });
        }
    }
}
