using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ArtworkFixture(string root,string name)
        {
            var p=AuditFixture(root,name,"<control><type>image</type><id>1</id><posX>100</posX><posY>100</posY><width>300</width><height>200</height><texture>#runtime.art</texture></control><control><type>image</type><id>2</id><visible>!control.hasthumb(1)</visible><texture>fallback.png</texture></control>");
            Directory.CreateDirectory(Path.Combine(p.Root,"Media"));using(var image=new Bitmap(12,12))using(var g=Graphics.FromImage(image)){g.Clear(Color.Red);image.Save(Path.Combine(p.Root,"Media","sample.png"),ImageFormat.Png);}return p;
        }
        static void RunArtworkPreviewTests(string root)
        {
            Test("Explicit artwork samples drive image content and fallback conditions together",()=>
            {
                var p=ArtworkFixture(root,"artwork-conditions");var s=new PreviewState(p);var image=p.Controls[0];Check(s.ArtworkAvailable(image)==null&&s.Visible(p.Controls[1])==null,"Runtime artwork was inferred.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");Check(s.ImageTexture(image)=="sample.png"&&s.ArtworkAvailable(image)==true&&s.Visible(p.Controls[1])==false,"Artwork and HasThumb disagree.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","");Check(s.ImageTexture(image)==""&&s.ArtworkAvailable(image)==false&&s.Visible(p.Controls[1])==true,"Explicit empty artwork fell back to its runtime binding.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art",null);Check(s.ImageTexture(image)=="#runtime.art"&&s.ArtworkAvailable(image)==null&&s.Visible(p.Controls[1])==null,"Removing sample did not restore uncertainty.");
            });
            Test("Artwork samples match exact image expressions without rewriting variables or button textures",()=>
            {
                var p=ArtworkFixture(root,"artwork-exact");p.Editor.Preview.Values["#runtime.art"]="missing.png";ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");var s=new PreviewState(p);
                Check(s.Text(p.Controls[0],"#runtime.art")=="missing.png"&&s.ImageTexture(p.Controls[0])=="sample.png","Image override leaked into general text values.");
                p.Controls[1].Properties["texture"]=new PropertyValue("#runtime.art","",true);Check(s.ImageTexture(p.Controls[1])=="sample.png","Identical image expressions do not share samples.");
                p.Controls[1].Properties["type"]=new PropertyValue("button","",true);Check(s.ImageTexture(p.Controls[1])=="#runtime.art","Image sample changed a button texture.");
                p.Controls[0].Properties["texture"]=new PropertyValue("#runtime.Art","",true);Check(s.ImageTexture(p.Controls[0])=="#runtime.Art","Case-distinct source expressions were merged.");
            });
            Test("Artwork samples survive copies and media presets without losing other preview state",()=>
            {
                var p=ArtworkFixture(root,"artwork-copy");p.Editor.Preview.Values["#custom"]="Keep";p.Editor.Preview.Plugins.Add("Keep plugin");ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");string signature=p.Editor.Preview.SignatureText();p.Editor.Preview.SetFrom(p.Editor.Preview);
                Check(signature==p.Editor.Preview.SignatureText(),"Self-copy lost artwork samples.");var copy=p.Editor.Preview.Copy();copy.Artwork.Clear();Check(p.Editor.Preview.Artwork.Count==1,"Copied artwork shares mutable storage.");
                Write(p.Root,"list.xml","<window><controls><control><type>listcontrol</type><id>50</id></control></controls></window>");p.OpenPage("list.xml");MediaFolderPreview.Apply(p,"music");
                Check(p.Editor.Preview.Artwork["#runtime.art"]=="sample.png"&&p.Editor.Preview.Values["#custom"]=="Keep"&&p.Editor.Preview.Plugins.Contains("Keep plugin"),"Folder scenario erased artwork or unrelated data.");
            });
            Test("Invalid artwork paths and capacity failures preserve all samples and saved archives",()=>
            {
                var p=ArtworkFixture(root,"artwork-validation");ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");string signature=ProjectFile.ContentSignature(p),file=Path.Combine(root,"artwork-safe.mp1skinproj");ProjectFile.Save(p,file);byte[] bytes=File.ReadAllBytes(file);
                foreach(string invalid in new[]{"../outside.png","C:\\outside.png","/outside.png","//server/share.png","a/./x.png","a//x.png","#runtime.png","x.svg","a:/x.png","a/../x.png","x.png ","a./x.png","x.png\n","x?.png"}){Reject(()=>ArtworkPreview.Set(p.Editor.Preview,"#runtime.art",invalid));Check(ProjectFile.ContentSignature(p)==signature,"Rejected path changed artwork.");}
                foreach(string key in new[]{""," ","-",new string('x',2049)})Reject(()=>ArtworkPreview.Set(p.Editor.Preview,key,"sample.png"));
                var full=p.Editor.Preview.Copy();for(int i=0;i<127;i++)full.Artwork.Add("expression"+i,"sample.png");string before=full.SignatureText();Reject(()=>ArtworkPreview.Set(full,"overflow","sample.png"));Check(full.SignatureText()==before,"Capacity rejection changed the map.");
                p.Editor.Preview.Artwork["#runtime.art"]="../outside.png";Reject(()=>ProjectFile.Save(p,file));Check(File.ReadAllBytes(file).SequenceEqual(bytes),"Invalid sample replaced the last readable archive.");
            });
            Test("Artwork project persistence remains independent of XML undo redo and export",()=>
            {
                var p=ArtworkFixture(root,"artwork-save");string xml=ProjectFile.XmlSignature(p);ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Sample changed XML history.");
                p.Edit(p.Controls[0],Set("width","330"),"Resize image");string after=p.Controls[0].Source.Text,signature=ProjectFile.ContentSignature(p),file=Path.Combine(root,"artwork-save.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(loaded)==signature&&new PreviewState(loaded).ArtworkAvailable(loaded.Controls[0])==true,"Saved artwork changed after reopening.");loaded.UndoChange();Check(!loaded.Dirty&&loaded.Editor.Preview.Artwork.Count==1,"Undo changed samples or left XML dirty.");loaded.RedoChange();Check(loaded.Controls[0].Source.Text==after,"Restored redo changed image XML.");string zip=Path.Combine(root,"artwork-export.zip");loaded.Export(zip);
                using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open())){string exported=reader.ReadToEnd();Check(exported==after&&exported.Contains("#runtime.art")&&!exported.Contains("sample.png"),"Artwork samples leaked into export.");}
            });
            Test("Legacy projects load without artwork and malformed or duplicate artwork is rejected",()=>
            {
                var p=ArtworkFixture(root,"artwork-archive");string legacy=Path.Combine(root,"artwork-legacy.mp1skinproj");ProjectFile.Save(p,legacy);Check(ProjectFile.Load(legacy).Editor.Preview.Artwork.Count==0,"Legacy archive acquired samples.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","");string valid=Path.Combine(root,"artwork-valid.mp1skinproj");ProjectFile.Save(p,valid);Check(ProjectFile.Load(valid).Editor.Preview.Artwork["#runtime.art"]=="","Empty sample did not persist.");
                foreach(string kind in new[]{"duplicate","encoding","path","utf8"})
                {
                    string file=Path.Combine(root,"artwork-bad-"+kind+".mp1skinproj");File.Copy(valid,file,true);
                    using(var archive=ZipFile.Open(file,ZipArchiveMode.Update)){var entry=archive.GetEntry("project.xml");var doc=new XmlDocument{XmlResolver=null};using(var reader=new StreamReader(entry.Open()))doc.LoadXml(reader.ReadToEnd());var node=(XmlElement)doc.SelectSingleNode("//preview/artwork");if(kind=="duplicate")node.ParentNode.AppendChild(node.CloneNode(true));else if(kind=="encoding")node.SetAttribute("encoding","unknown");else if(kind=="utf8")node.SetAttribute("source","/w==");else node.InnerText=Convert.ToBase64String(Encoding.UTF8.GetBytes("../outside.png"));entry.Delete();using(var writer=new StreamWriter(archive.CreateEntry("project.xml").Open()))writer.Write(doc.OuterXml);}
                    Reject(()=>ProjectFile.Load(file));
                }
            });
            Test("Artwork diagnostics explain visible missing corrupt and runtime images without false empty findings",()=>
            {
                var p=ArtworkFixture(root,"artwork-report");var s=new PreviewState(p);string signature=ProjectFile.ContentSignature(p);var issue=PreviewProblems.Run(p,s).Issues.Single(i=>i.Code=="PREVIEW_ARTWORK");Check(issue.Property=="texture"&&issue.Detail.Contains("Sample Artwork")&&issue.Detail.Contains("#runtime.art")&&ProjectFile.ContentSignature(p)==signature,"Runtime artwork was not explained read-only.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","missing.png");issue=PreviewProblems.Run(p,s).Issues.First(i=>i.Code=="PREVIEW_ARTWORK"&&i.ControlKey==p.Controls[0].Key);Check(issue.Detail.Contains("explicit artwork sample")&&issue.Detail.Contains("No readable image"),"Unavailable sample was not identified.");
                File.WriteAllText(Path.Combine(p.Root,"Media","bad.png"),"Not an image");ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","bad.png");Check(s.ArtworkAvailable(p.Controls[0])==null,"Corrupt sample became known available.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","");Check(!PreviewProblems.Run(p,s).Issues.Any(i=>i.ControlKey==p.Controls[0].Key),"Explicit empty artwork was reported as unavailable.");
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");Check(!PreviewProblems.Run(p,s).Issues.Any(i=>i.ControlKey==p.Controls[0].Key),"Valid sample still reports original missing binding.");
            });
            Test("Artwork sample dialogs keep cancel read-only and return changes only on apply",()=>
            {
                var p=ArtworkFixture(root,"artwork-dialog");string signature=ProjectFile.ContentSignature(p);
                using(var dialog=new ArtworkPreviewDialog(p)){dialog.SelectForTest("#runtime.art");dialog.SampleForTest("sample.png");Check(ProjectFile.ContentSignature(p)==signature&&dialog.Result==null,"Dialog draft leaked before Apply.");}
                Check(ProjectFile.ContentSignature(p)==signature,"Cancel changed project samples.");
                using(var dialog=new ArtworkPreviewDialog(p)){dialog.SelectForTest("#runtime.art");dialog.SampleForTest("");dialog.FilterForTest("no-match");dialog.FilterForTest("runtime");dialog.AcceptForTest();Check(dialog.DialogResult==DialogResult.OK&&dialog.Result.Artwork["#runtime.art"]==""&&ProjectFile.ContentSignature(p)==signature,"Search lost a draft or Apply failed to return independent samples.");}
                ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");using(var dialog=new PreviewSettingsDialog(p)){dialog.SetValueForTest("#custom","Changed");Check(dialog.CollectForTest().Artwork["#runtime.art"]=="sample.png","Sample Data erased artwork.");dialog.PresetForTest("Baseline data");Check(dialog.CollectForTest().Artwork.Count==0,"Baseline preset retained old artwork.");}
            });
            Test("Artwork expressions remain stable across other pages and theme assets take precedence",()=>
            {
                var p=ArtworkFixture(root,"artwork-theme");ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");Write(p.Root,"other.xml","<window><controls><control><type>image</type><id>88</id><texture>#runtime.art</texture></control></controls></window>");p.OpenPage("other.xml");Check(new PreviewState(p).ImageTexture(p.Controls[0])=="sample.png","Page change lost identical-expression sample.");
                string theme=Path.Combine(p.Root,"Themes","Test","Media");Directory.CreateDirectory(theme);using(var image=new Bitmap(15,17))image.Save(Path.Combine(theme,"sample.png"),ImageFormat.Png);p.Theme="Test";p.OpenPage("other.xml");Check(p.Resolve("Media/sample.png")==Path.Combine(theme,"sample.png")&&new PreviewState(p).ArtworkAvailable(p.Controls[0])==true,"Theme sample did not resolve through ordinary asset precedence.");
                p.Theme="";p.OpenPage("main.xml");Check(new PreviewState(p).ImageTexture(p.Controls[0])=="sample.png","Returning to original page lost sample.");
            });
            Test("Explicit artwork samples do not guess which duplicate control ID is active",()=>
            {
                var p=ArtworkFixture(root,"artwork-duplicate");Write(p.Root,"duplicate.xml","<window><controls><control><type>image</type><id>7</id><texture>#one</texture></control><control><type>image</type><id>7</id><texture>#two</texture></control><control><type>label</type><id>8</id><visible>!control.hasthumb(7)</visible></control></controls></window>");p.OpenPage("duplicate.xml");ArtworkPreview.Set(p.Editor.Preview,"#one","");ArtworkPreview.Set(p.Editor.Preview,"#two","");var state=new PreviewState(p);
                Check(p.Controls.Take(2).All(c=>state.ArtworkAvailable(c)==false)&&state.Visible(p.Controls[2])==null&&PreviewProblems.Run(p,state).Issues.Any(i=>i.Detail.Contains("Control ID 7 is ambiguous")),"Samples erased duplicate-ID uncertainty.");
            });
            Test("Canvas renders selected sample artwork and explicit empty suppresses original image",()=>
            {
                var p=ArtworkFixture(root,"artwork-render");p.Editor.ShowUnresolved=false;ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","sample.png");
                using(var canvas=new SkinCanvas{Project=p,State=new PreviewState(p),Size=new Size(800,500),Grid=false})using(var bitmap=new Bitmap(800,500))
                {
                    canvas.DrawToBitmap(bitmap,new Rectangle(0,0,800,500));int red=0;for(int y=0;y<500;y+=3)for(int x=0;x<800;x+=3){Color c=bitmap.GetPixel(x,y);if(c.R>220&&c.G<30&&c.B<30)red++;}Check(red>100,"Sample image was absent from production canvas rendering.");
                    ArtworkPreview.Set(p.Editor.Preview,"#runtime.art","");canvas.ResetAssets();canvas.DrawToBitmap(bitmap,new Rectangle(0,0,800,500));red=0;for(int y=0;y<500;y+=3)for(int x=0;x<800;x+=3){Color c=bitmap.GetPixel(x,y);if(c.R>220&&c.G<30&&c.B<30)red++;}Check(red==0,"Empty sample still renders original or cached image.");
                }
            });
        }
    }
}
