using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static int passed,failed;
        static List<string> report=new List<string>();
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Reject(Action a){bool rejected=false;try{a();}catch{rejected=true;}Check(rejected,"Invalid operation was accepted.");}
        static void Test(string name,Action a)
        {try{a();passed++;report.Add("PASS  "+name);}catch(Exception ex){failed++;report.Add("FAIL  "+name+" · "+ex.Message);}}
        static Dictionary<string,string> Set(string key,string value){return new Dictionary<string,string>{{key,value}};}
        static string Xml(string inside){return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<window>\r\n  <id>0</id>\r\n  <controls>\r\n    <control>\r\n      <type>label</type>\r\n      <id>0</id>\r\n      "+inside+"\r\n    </control>\r\n  </controls>\r\n</window>";}
        static void Write(string root,string name,string text){File.WriteAllText(Path.Combine(root,name),text,new UTF8Encoding(false));}
        public static int Run(string root)
        {
            Directory.CreateDirectory(root);passed=failed=0;report.Clear();
            Test("Unchanged UTF-8 file remains byte-identical",()=>{string x=Xml("<!--keep--><posX>100</posX><unknown x=\"1\">x</unknown>");var s=SkinSource.FromText(x,"a.xml");Check(s.Text==x&&!s.Changed,"Parsing changes the source.");Check(s.Bytes().SequenceEqual(new UTF8Encoding(false).GetBytes(x)),"Bytes were modified.");});
            Test("Position changes affect a single source span",()=>{string x=Xml("<posX>100</posX><!--keep--><unknown x=\"1\">x</unknown>");var s=SkinSource.FromText(x,"a.xml");s.SetControlProperties(0,Set("posX","220"));Check(s.Text==x.Replace("<posX>100</posX>","<posX>220</posX>"),"Change affects additional bytes.");});
            Test("Duplicate ID 0: only the second control changes",()=>{string x="<window><controls><control><type>label</type><id>0</id><posX>10</posX></control><control><type>label</type><id>0</id><posX>10</posX></control></controls></window>";var s=SkinSource.FromText(x,"a.xml");s.SetControlProperties(1,Set("posX","80"));Check(s.Elements.Where(n=>n.Name=="control").First().Value("posX")=="10","First control was changed.");Check(s.Elements.Where(n=>n.Name=="control").Last().Value("posX")=="80","Second control was not changed.");});
            Test("Multiple properties update in one transaction",()=>{var s=SkinSource.FromText(Xml("<posX>1</posX><posY>2</posY><width>3</width>"),"a.xml");s.SetControlProperties(0,new Dictionary<string,string>{{"posX","10"},{"posY","20"},{"width","30"}});var n=s.Elements.First(e=>e.Name=="control");Check(n.Value("posX")=="10"&&n.Value("posY")=="20"&&n.Value("width")=="30","Source spans are offset incorrectly.");});
            Test("New local property preserves unknown tags",()=>{var s=SkinSource.FromText(Xml("<label>A</label><strange a=\"&gt;\">z</strange>"),"a.xml");s.SetControlProperties(0,Set("width","300"));Check(s.Text.Contains("<strange a=\"&gt;\">z</strange>")&&s.Text.Contains("<width>300</width>"),"Unknown tag was lost.");});
            Test("Comments inside a property are preserved",()=>{var s=SkinSource.FromText(Xml("<label><!--retain-->old<!--end--></label>"),"a.xml");s.SetControlProperties(0,Set("label","Neu & gut"));Check(s.Text.Contains("<label><!--retain-->Neu &amp; gut<!--end--></label>"),"Incorrect comments or escaping.");});
            Test("CDATA remains CDATA and closing sequences are split",()=>{var s=SkinSource.FromText(Xml("<label><![CDATA[old]]></label>"),"a.xml");s.SetControlProperties(0,Set("label","a]]>b"));Check(s.Elements.First(n=>n.Name=="label").Element.InnerText=="a]]>b"&&s.Text.Contains("<![CDATA["),"Incorrect CDATA content.");});
            Test("XML entities and accented characters are saved correctly",()=>{var s=SkinSource.FromText(Xml("<label>old</label>"),"a.xml");string value="Ä <b> & \"Ö\"";s.SetControlProperties(0,Set("label",value));Check(s.Elements.First(n=>n.Name=="label").Element.InnerText==value,"Value is not identical.");});
            Test("Self-closing property preserves its attributes",()=>{var s=SkinSource.FromText(Xml("<label test=\"&gt;\" />"),"a.xml");s.SetControlProperties(0,Set("label","Neu"));Check(s.Elements.First(n=>n.Name=="label").Element.GetAttribute("test")==">","Attribute was lost.");});
            Test("IDs and control types are protected",()=>{var s=SkinSource.FromText(Xml("<label>A</label>"),"a.xml");Reject(()=>s.SetControlProperties(0,Set("id","8")));Reject(()=>s.SetControlProperties(0,Set("type","image")));});
            Test("DTDs and external entities are rejected",()=>Reject(()=>SkinSource.FromText("<!DOCTYPE window [<!ENTITY bad SYSTEM 'file:///C:/Windows/win.ini'>]><window>&bad;</window>","bad.xml")));
            Test("Malformed XML is rejected",()=>Reject(()=>SkinSource.FromText("<window><controls></window>","bad.xml")));
            Test("Nested property content is not overwritten",()=>{var s=SkinSource.FromText(Xml("<label><extension>Keep</extension></label>"),"a.xml");string before=s.Text;Reject(()=>s.SetControlProperties(0,Set("label","New")));Check(s.Text==before,"Protected XML was changed.");});
            Test("UTF-16 LE encoding and BOM are preserved",()=>{string path=Path.Combine(root,"utf16.xml");string text=Xml("<label>Ä</label>").Replace("utf-8","utf-16");File.WriteAllText(path,text,new UnicodeEncoding(false,true,true));var s=SkinSource.Read(path,"utf16.xml");var before=File.ReadAllBytes(path);Check(s.Bytes().SequenceEqual(before),"UTF-16 roundtrip changed content.");s.SetControlProperties(0,Set("label","Ö"));Check(s.Bytes()[0]==255&&s.Bytes()[1]==254,"BOM was lost.");});
            Test("Windows-1252 is preserved and unsupported characters are rejected",()=>{string path=Path.Combine(root,"ansi.xml");string text=Xml("<label>Ä</label>").Replace("utf-8","windows-1252");File.WriteAllText(path,text,Encoding.GetEncoding(1252));var s=SkinSource.Read(path,"ansi.xml");Check(s.Bytes().SequenceEqual(File.ReadAllBytes(path)),"ANSI roundtrip changed content.");s.SetControlProperties(0,Set("label","Ö"));string before=s.Text;Reject(()=>s.SetControlProperties(0,Set("label","漢字")));Check(s.Text==before,"Encoding error was not rolled back.");});
            string fixture=Path.Combine(root,"fixture");Directory.CreateDirectory(fixture);
            Write(fixture,"references.xml","<controls><skin><width>1920</width><height>1080</height></skin><define>#global:5</define><control><type>label</type><width>100</width><textcolor>ffffffff</textcolor></control><style Name=\"Title\"><width>200</width><font>font22</font></style></controls>");
            Write(fixture,"fonts.xml","<fonts><font><name>font22</name><height>22</height></font></fonts>");
            Write(fixture,"main.xml","<window><id>0</id><define>#local:240</define><controls><import>part.xml</import></controls></window>");
            Write(fixture,"part.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id><width>300</width><posX>#local</posX><label>#time</label><visible>window.isvisible(0)</visible><extension a=\"b\"/></control><control Style=\"Title\"><type>label</type><id>0</id></control></controls></window>");
            Test("Reference, style and local value precedence is resolved",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");Check(p.Controls[0].Get("width")=="300"&&p.Controls[1].Get("width")=="200","Incorrect order.");Check(!p.Controls[0].Properties["font"].Local&&p.Controls[0].Properties["width"].Local,"Incorrect provenance.");});
            Test("Defines in includes remain bound and resolvable",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");Check(p.Controls[0].IsBound("posX")&&p.Controls[0].Number("posX",0)==240&&p.Controls[0].Get("posX")=="#local","Binding was resolved or lost.");});
            Test("Undo/redo restores exact source text",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");string original=p.Controls[0].Source.Text;p.Edit(p.Controls[0],Set("width","650"),"Test");string edited=p.Controls[0].Source.Text;p.UndoChange();Check(p.Controls[0].Source.Text==original&&!p.Dirty,"Undo is incomplete.");p.RedoChange();Check(p.Controls[0].Source.Text==edited&&p.Dirty,"Redo is incomplete.");});
            Test("ZIP exports only the correct include file and preserves the original bytes",()=>{var original=File.ReadAllBytes(Path.Combine(fixture,"part.xml"));var p=new SkinProject(fixture);p.OpenPage("main.xml");p.Edit(p.Controls[0],Set("width","700"),"Test");string zip=Path.Combine(root,"export.zip");p.Export(zip);using(var archive=ZipFile.OpenRead(zip)){Check(archive.GetEntry("part.xml")!=null&&archive.GetEntry("main.xml")==null,"Wrong files were exported.");using(var reader=new StreamReader(archive.GetEntry("part.xml").Open()))Check(reader.ReadToEnd()==p.Controls[0].Source.Text,"ZIP content differs.");}Check(File.ReadAllBytes(Path.Combine(fixture,"part.xml")).SequenceEqual(original),"Original file was overwritten.");});
            Test("Paths outside the skin directory are rejected",()=>{var p=new SkinProject(fixture);Reject(()=>p.SafePath("../escape.xml"));Reject(()=>p.SafePath("C:\\Windows\\win.ini"));});
            Test("Exporting to skin XML instead of ZIP is rejected",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");p.Edit(p.Controls[0],Set("width","800"),"Test");var bytes=File.ReadAllBytes(Path.Combine(fixture,"part.xml"));Reject(()=>p.Export(Path.Combine(fixture,"part.xml")));Check(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(fixture,"part.xml"))),"Original was overwritten.");});
            Test("Cyclic and missing includes are reported",()=>{Write(fixture,"cycle.xml","<window><controls><import>cycle.xml</import><import>missing.xml</import></controls></window>");var p=new SkinProject(fixture);p.OpenPage("cycle.xml");Check(p.Warnings.Any(w=>w.Contains("Cyclic"))&&p.Warnings.Any(w=>w.Contains("Missing")),"Warnings are missing.");});
            Test("Theme files override base files with fallback for missing files",()=>{Directory.CreateDirectory(Path.Combine(fixture,"Themes","Test"));Write(Path.Combine(fixture,"Themes","Test"),"part.xml","<window><controls><control><type>label</type><id>0</id><label>Theme</label></control></controls></window>");var p=new SkinProject(fixture){Theme="Test"};p.OpenPage("main.xml");Check(p.Controls[0].Get("label")=="Theme"&&p.Controls[0].Source.RelativePath=="Themes/Test/part.xml","Incorrect theme resolver.");});
            Test("Unknown preview conditions remain unknown",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");var s=new PreviewState(p);Check(s.Evaluate("#(eq(#plugin.a,'B'))",new HashSet<string>())==null,"Unknown condition was silently accepted.");});
            Test("Preview Boolean conditions and plugin states are evaluated",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");var s=new PreviewState(p);Check(s.Evaluate("[window.isvisible(0)|window.isvisible(35)]+!plugin.isenabled(World Weather)",new HashSet<string>())==true,"Incorrect Boolean condition.");Check(s.Evaluate("no+unknown.test()",new HashSet<string>())==false,"Incorrect three-valued logic.");});
            Test("Real Titan files load and clock edits preserve animation comments",()=>{var p=new SkinProject(StudioForm.DemoRoot());p.OpenPage("demo.xml");var time=p.Controls.First(c=>c.Get("description")=="Time");string source=time.Source.Text;int before=Int32.Parse(time.Get("posX"));p.Edit(time,Set("posX",(before-100).ToString()),"Move clock");var after=p.Controls.First(c=>c.Get("description")=="Time");Check(after.Source.RelativePath=="common.time.xml"&&after.Source.Text.Contains("<!--label>#(iif"),"Source mapping or comment was lost.");Check(after.Source.Text.Contains("<animation effect=\"fade\" time=\"250\">WindowOpen</animation>"),"Animation was lost.");p.UndoChange();Check(p.Controls.First(c=>c.Get("description")=="Time").Source.Text==source,"Titan undo is not identical.");});
            RunDesignerTests(root);
            RunProjectFileTests(root);
            RunEditorStateTests(root);
            RunWorkflowToolsTests(root);
            RunSkinAuditTests(root);
            RunPropertyResetTests(root);
            RunPreviewSettingsTests(root);
            RunStructuralTests(root);
            RunElementSearchTests();
            RunGroupTests(root);
            RunGroupCreationTests(root);
            RunGroupRemovalTests(root);
            RunSurfaceRegressionTests(root);
            RunTextEditTests(root);
            RunLanguageTests(root);RunConditionScopeTests(root);RunStackTests(root);RunGridTests(root);RunCollectionTests(root);
            RunLiveTests(root);
            report.Add("");report.Add(passed+" passed, "+failed+" failed.");
            File.WriteAllLines(Path.Combine(root,"test-results.txt"),report.ToArray(),new UTF8Encoding(false));
            return failed==0?0:1;
        }
    }
}
