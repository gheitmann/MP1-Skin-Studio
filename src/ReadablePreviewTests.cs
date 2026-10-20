using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Drawing;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject MediaTipFixture(string root,string name)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);
            var xml=new StringBuilder("<window><id>7979</id><controls><control><type>label</type><id>10</id><width>200</width><label>#MediaTip.Headline</label></control>");
            foreach(int start in new[]{20,30,40,50,60})for(int line=0;line<5;line++)xml.Append("<control><type>fadelabel</type><id>").Append(start+line).Append("</id><description>Line ").Append(line+1).Append("</description><posX>10</posX><posY>").Append(line*30).Append("</posY><width>300</width><height>28</height><label>#MediaTip.FileName (#MediaTip.FileSize)</label></control>");
            xml.Append("</controls></window>");Write(folder,"MediaTipOverlay.xml",xml.ToString());var p=new SkinProject(folder);p.OpenPage("MediaTipOverlay.xml");return p;
        }
        static void RunReadablePreviewTests(string root)
        {
            Test("MediaTip examples expose exactly one five-line block without modifying XML or conditions",()=>
            {var p=MediaTipFixture(root,"readable-scenes");string xml=ProjectFile.XmlSignature(p);var state=new PreviewState(p);Check(ReadablePreview.SupportsMediaTip(p),"Expected supported fixture");for(int i=0;i<ReadablePreview.Scenes.Length;i++){p.Editor.MediaTipScene=ReadablePreview.Scenes[i];var shown=p.Controls.Where(c=>ReadablePreview.InScene(p,c)).ToArray();Check(shown.Length==6&&shown.Where(c=>SkinProject.ControlId(c)>=20).All(c=>SkinProject.ControlId(c)/10==i+2),"More than one variant displayed");Check(p.Controls.All(c=>state.Visible(c)==true),"Example changed actual visibility evaluation");}Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Example altered XML/history");});
            Test("MediaTip matching rejects other pages, IDs, incomplete blocks and ambiguous controls",()=>
            {var p=MediaTipFixture(root,"readable-shape");p.WindowId=1;Check(!ReadablePreview.SupportsMediaTip(p),"Unrelated window matched");p.WindowId=7979;p.Page="Other.xml";Check(!ReadablePreview.SupportsMediaTip(p),"Unrelated page matched");p.Page="MediaTipOverlay.xml";var removed=p.Controls.Last();p.Controls.Remove(removed);Check(!ReadablePreview.SupportsMediaTip(p),"Incomplete shape matched");p.Controls.Add(removed);var duplicate=SkinSource.FromText("<control><type>fadelabel</type><id>20</id></control>","duplicate.xml");p.Controls.Add(new SkinControl{Source=duplicate,Node=duplicate.Root,Key="duplicate|0",Defines=new Dictionary<string,string>(),Properties=new Dictionary<string,PropertyValue>{{"id",new PropertyValue("20","",true)},{"type",new PropertyValue("fadelabel","",true)}}});Check(!ReadablePreview.SupportsMediaTip(p),"Ambiguous ID matched");});
            Test("MediaTip sample captions preserve explicit data and reveal raw bindings in diagnostic mode",()=>
            {var p=MediaTipFixture(root,"readable-caption");var s=new PreviewState(p);var c=p.Controls.First(x=>SkinProject.ControlId(x)==20);string raw=c.Get("label");Check(ReadablePreview.Caption(s,c).Contains("Example song.flac")&&!ReadablePreview.Caption(s,c).Contains("#"),"No readable sample");Check(ReadablePreview.Caption(s,c,true)==raw,"Diagnostic binding hidden");p.Editor.Preview.Values["#MediaTip.FileName"]="Owner.flac";p.Editor.Preview.Values["#MediaTip.FileSize"]="9 MB";s=new PreviewState(p);Check(ReadablePreview.Caption(s,c)=="Owner.flac (9 MB)"&&c.Get("label")==raw,"Explicit data/source lost");p.Edit(p.Controls.Single(x=>SkinProject.ControlId(x)==10),Set("label","Owner headline"),"Headline edit");Check(ReadablePreview.SupportsMediaTip(p),"A headline text edit disabled the supported variant layout.");});
            Test("Generic unresolved captions shorten safely without declaring bindings resolved",()=>
            {var p=AuditFixture(root,"readable-generic","<control><type>label</type><description>Programme title</description><label>#(unsupported(#unknown))</label></control>");var s=new PreviewState(p);var c=p.Controls[0];string sig=ProjectFile.ContentSignature(p);Check(ReadablePreview.Caption(s,c)=="Programme title (example)"&&!s.ResolvedLabel(c,c.Get("label"))&&ReadablePreview.Caption(s,c,true)==c.Get("label")&&ProjectFile.ContentSignature(p)==sig,"Caption changed semantics/state");});
            Test("Readable captions retain literals, explicit empty strings and actual sample text",()=>
            {var p=AuditFixture(root,"readable-literals","<control><type>label</type><label>Normal text</label></control><control><type>label</type><label>-</label></control><control><type>label</type><label>#title</label></control>");var s=new PreviewState(p);Check(ReadablePreview.Caption(s,p.Controls[0])=="Normal text"&&ReadablePreview.Caption(s,p.Controls[1])==""&&ReadablePreview.Caption(s,p.Controls[2])==PreviewSettings.BaseValues()["#title"],"Known content replaced");});
            Test("Isolation follows selected group children without unrelated siblings",()=>
            {var p=AuditFixture(root,"readable-solo","<control><type>group</type><id>1</id><control><type>label</type><id>2</id><label>A</label></control></control><control><type>label</type><id>3</id><label>B</label></control>");Check(ReadablePreview.InSelection(p.Controls[0],p.Controls[0])&&ReadablePreview.InSelection(p.Controls[1],p.Controls[0])&&!ReadablePreview.InSelection(p.Controls[2],p.Controls[0])&&!ReadablePreview.InSelection(p.Controls[0],null),"Isolation scope wrong");});
            Test("Readable view options persist independently of source XML and are signed as editor state",()=>
            {var p=MediaTipFixture(root,"readable-save");string xml=ProjectFile.XmlSignature(p),signature=ProjectFile.EditorSignature(p);p.Editor.PreviewMode="all";p.Editor.MediaTipScene="radio";Check(ProjectFile.EditorSignature(p)!=signature&&ProjectFile.XmlSignature(p)==xml,"Mode missing from editor signature");string file=Path.Combine(root,"readable-save.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(loaded.Editor.PreviewMode=="all"&&loaded.Editor.MediaTipScene=="radio"&&ProjectFile.ContentSignature(loaded)==ProjectFile.ContentSignature(p)&&!loaded.Dirty,"View did not roundtrip");});
            Test("Invalid view settings reject save before replacing an existing project",()=>
            {var p=MediaTipFixture(root,"readable-invalid");string file=Path.Combine(root,"readable-invalid.mp1skinproj");ProjectFile.Save(p,file);byte[] before=File.ReadAllBytes(file);p.Editor.PreviewMode="unknown";Reject(()=>ProjectFile.Save(p,file));Check(before.SequenceEqual(File.ReadAllBytes(file)),"Invalid view overwrote project");p.Editor.PreviewMode="layout";p.Editor.MediaTipScene="unknown";Reject(()=>ProjectFile.Save(p,file));Check(before.SequenceEqual(File.ReadAllBytes(file)),"Invalid scene overwrote project");});
            Test("Legacy project view defaults retain diagnostic or clean intent",()=>
            {foreach(bool show in new[]{false,true}){var p=MediaTipFixture(root,"readable-legacy-"+show);p.Editor.ShowUnresolved=show;string file=Path.Combine(root,"readable-legacy-"+show+".mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,x=>System.Text.RegularExpressions.Regex.Replace(x," (previewMode|mediaTipScene)=\"[^\"]*\"",""));var loaded=ProjectFile.Load(file);Check(loaded.Editor.PreviewMode==(show?"all":"layout")&&loaded.Editor.MediaTipScene=="music","Legacy view intent lost");}});
        }
    }
}
