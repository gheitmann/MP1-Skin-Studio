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
        static void Reject(Action a){bool rejected=false;try{a();}catch{rejected=true;}Check(rejected,"Ungültige Operation wurde akzeptiert.");}
        static void Test(string name,Action a)
        {try{a();passed++;report.Add("PASS  "+name);}catch(Exception ex){failed++;report.Add("FAIL  "+name+" · "+ex.Message);}}
        static Dictionary<string,string> Set(string key,string value){return new Dictionary<string,string>{{key,value}};}
        static string Xml(string inside){return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<window>\r\n  <id>0</id>\r\n  <controls>\r\n    <control>\r\n      <type>label</type>\r\n      <id>0</id>\r\n      "+inside+"\r\n    </control>\r\n  </controls>\r\n</window>";}
        static void Write(string root,string name,string text){File.WriteAllText(Path.Combine(root,name),text,new UTF8Encoding(false));}
        public static int Run(string root)
        {
            Directory.CreateDirectory(root);passed=failed=0;report.Clear();
            Test("Unveränderte UTF-8-Datei bleibt bytegleich",()=>{string x=Xml("<!--keep--><posX>100</posX><unknown x=\"1\">x</unknown>");var s=SkinSource.FromText(x,"a.xml");Check(s.Text==x&&!s.Changed,"Parse verändert Quelle.");Check(s.Bytes().SequenceEqual(new UTF8Encoding(false).GetBytes(x)),"Bytes verändert.");});
            Test("Position wird als einzelner Quelltextbereich geändert",()=>{string x=Xml("<posX>100</posX><!--keep--><unknown x=\"1\">x</unknown>");var s=SkinSource.FromText(x,"a.xml");s.SetControlProperties(0,Set("posX","220"));Check(s.Text==x.Replace("<posX>100</posX>","<posX>220</posX>"),"Änderung betrifft weitere Bytes.");});
            Test("Doppelte ID 0: nur zweites Control geändert",()=>{string x="<window><controls><control><type>label</type><id>0</id><posX>10</posX></control><control><type>label</type><id>0</id><posX>10</posX></control></controls></window>";var s=SkinSource.FromText(x,"a.xml");s.SetControlProperties(1,Set("posX","80"));Check(s.Elements.Where(n=>n.Name=="control").First().Value("posX")=="10","Erstes Control geändert.");Check(s.Elements.Where(n=>n.Name=="control").Last().Value("posX")=="80","Zweites Control nicht geändert.");});
            Test("Mehrere Eigenschaften in einer Transaktion",()=>{var s=SkinSource.FromText(Xml("<posX>1</posX><posY>2</posY><width>3</width>"),"a.xml");s.SetControlProperties(0,new Dictionary<string,string>{{"posX","10"},{"posY","20"},{"width","30"}});var n=s.Elements.First(e=>e.Name=="control");Check(n.Value("posX")=="10"&&n.Value("posY")=="20"&&n.Value("width")=="30","Versetzte Quelltextbereiche.");});
            Test("Neue lokale Eigenschaft, unbekannte Tags bleiben",()=>{var s=SkinSource.FromText(Xml("<label>A</label><strange a=\"&gt;\">z</strange>"),"a.xml");s.SetControlProperties(0,Set("width","300"));Check(s.Text.Contains("<strange a=\"&gt;\">z</strange>")&&s.Text.Contains("<width>300</width>"),"Unbekannter Tag verloren.");});
            Test("Kommentar innerhalb einer Eigenschaft bleibt",()=>{var s=SkinSource.FromText(Xml("<label><!--retain-->old<!--end--></label>"),"a.xml");s.SetControlProperties(0,Set("label","Neu & gut"));Check(s.Text.Contains("<label><!--retain-->Neu &amp; gut<!--end--></label>"),"Kommentare oder Escaping fehlerhaft.");});
            Test("CDATA bleibt CDATA; schließende Sequenz wird gesplittet",()=>{var s=SkinSource.FromText(Xml("<label><![CDATA[old]]></label>"),"a.xml");s.SetControlProperties(0,Set("label","a]]>b"));Check(s.Elements.First(n=>n.Name=="label").Element.InnerText=="a]]>b"&&s.Text.Contains("<![CDATA["),"CDATA-Inhalt fehlerhaft.");});
            Test("XML-Entities und Umlaute werden korrekt gespeichert",()=>{var s=SkinSource.FromText(Xml("<label>old</label>"),"a.xml");string value="Ä <b> & \"Ö\"";s.SetControlProperties(0,Set("label",value));Check(s.Elements.First(n=>n.Name=="label").Element.InnerText==value,"Wert nicht identisch.");});
            Test("Self-closing Eigenschaft behält Attribute",()=>{var s=SkinSource.FromText(Xml("<label test=\"&gt;\" />"),"a.xml");s.SetControlProperties(0,Set("label","Neu"));Check(s.Elements.First(n=>n.Name=="label").Element.GetAttribute("test")==">","Attribut verloren.");});
            Test("IDs und Control-Typen sind geschützt",()=>{var s=SkinSource.FromText(Xml("<label>A</label>"),"a.xml");Reject(()=>s.SetControlProperties(0,Set("id","8")));Reject(()=>s.SetControlProperties(0,Set("type","image")));});
            Test("DTD und externe Entitäten werden abgelehnt",()=>Reject(()=>SkinSource.FromText("<!DOCTYPE window [<!ENTITY bad SYSTEM 'file:///C:/Windows/win.ini'>]><window>&bad;</window>","bad.xml")));
            Test("Fehlerhaftes XML wird abgelehnt",()=>Reject(()=>SkinSource.FromText("<window><controls></window>","bad.xml")));
            Test("Verschachtelte Eigenschaft wird nicht überschrieben",()=>{var s=SkinSource.FromText(Xml("<label><extension>Keep</extension></label>"),"a.xml");string before=s.Text;Reject(()=>s.SetControlProperties(0,Set("label","New")));Check(s.Text==before,"Geschütztes XML verändert.");});
            Test("UTF-16 LE und BOM bleiben erhalten",()=>{string path=Path.Combine(root,"utf16.xml");string text=Xml("<label>Ä</label>").Replace("utf-8","utf-16");File.WriteAllText(path,text,new UnicodeEncoding(false,true,true));var s=SkinSource.Read(path,"utf16.xml");var before=File.ReadAllBytes(path);Check(s.Bytes().SequenceEqual(before),"UTF-16-Roundtrip verändert.");s.SetControlProperties(0,Set("label","Ö"));Check(s.Bytes()[0]==255&&s.Bytes()[1]==254,"BOM verloren.");});
            Test("Windows-1252 bleibt erhalten; ungeeignete Zeichen abgelehnt",()=>{string path=Path.Combine(root,"ansi.xml");string text=Xml("<label>Ä</label>").Replace("utf-8","windows-1252");File.WriteAllText(path,text,Encoding.GetEncoding(1252));var s=SkinSource.Read(path,"ansi.xml");Check(s.Bytes().SequenceEqual(File.ReadAllBytes(path)),"ANSI-Roundtrip verändert.");s.SetControlProperties(0,Set("label","Ö"));string before=s.Text;Reject(()=>s.SetControlProperties(0,Set("label","漢字")));Check(s.Text==before,"Encoding-Fehler nicht zurückgerollt.");});
            string fixture=Path.Combine(root,"fixture");Directory.CreateDirectory(fixture);
            Write(fixture,"references.xml","<controls><skin><width>1920</width><height>1080</height></skin><define>#global:5</define><control><type>label</type><width>100</width><textcolor>ffffffff</textcolor></control><style Name=\"Title\"><width>200</width><font>font22</font></style></controls>");
            Write(fixture,"fonts.xml","<fonts><font><name>font22</name><height>22</height></font></fonts>");
            Write(fixture,"main.xml","<window><id>0</id><define>#local:240</define><controls><import>part.xml</import></controls></window>");
            Write(fixture,"part.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id><width>300</width><posX>#local</posX><label>#time</label><visible>window.isvisible(0)</visible><extension a=\"b\"/></control><control Style=\"Title\"><type>label</type><id>0</id></control></controls></window>");
            Test("Referenz → Style → lokaler Wert aufgelöst",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");Check(p.Controls[0].Get("width")=="300"&&p.Controls[1].Get("width")=="200","Reihenfolge fehlerhaft.");Check(!p.Controls[0].Properties["font"].Local&&p.Controls[0].Properties["width"].Local,"Herkunft falsch.");});
            Test("Defines im Include bleiben gebunden und auflösbar",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");Check(p.Controls[0].IsBound("posX")&&p.Controls[0].Number("posX",0)==240&&p.Controls[0].Get("posX")=="#local","Bindung wurde aufgelöst oder verloren.");});
            Test("Undo/Redo stellt exakten Quelltext wieder her",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");string original=p.Controls[0].Source.Text;p.Edit(p.Controls[0],Set("width","650"),"Test");string edited=p.Controls[0].Source.Text;p.UndoChange();Check(p.Controls[0].Source.Text==original&&!p.Dirty,"Undo unvollständig.");p.RedoChange();Check(p.Controls[0].Source.Text==edited&&p.Dirty,"Redo unvollständig.");});
            Test("ZIP exportiert nur richtige Include-Datei; Original bleibt bytegleich",()=>{var original=File.ReadAllBytes(Path.Combine(fixture,"part.xml"));var p=new SkinProject(fixture);p.OpenPage("main.xml");p.Edit(p.Controls[0],Set("width","700"),"Test");string zip=Path.Combine(root,"export.zip");p.Export(zip);using(var archive=ZipFile.OpenRead(zip)){Check(archive.GetEntry("part.xml")!=null&&archive.GetEntry("main.xml")==null,"Falsche Dateien exportiert.");using(var reader=new StreamReader(archive.GetEntry("part.xml").Open()))Check(reader.ReadToEnd()==p.Controls[0].Source.Text,"ZIP-Inhalt abweichend.");}Check(File.ReadAllBytes(Path.Combine(fixture,"part.xml")).SequenceEqual(original),"Originaldatei überschrieben.");});
            Test("Pfad außerhalb des Skinordners wird abgelehnt",()=>{var p=new SkinProject(fixture);Reject(()=>p.SafePath("../escape.xml"));Reject(()=>p.SafePath("C:\\Windows\\win.ini"));});
            Test("Export auf Skin-XML statt ZIP wird abgelehnt",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");p.Edit(p.Controls[0],Set("width","800"),"Test");var bytes=File.ReadAllBytes(Path.Combine(fixture,"part.xml"));Reject(()=>p.Export(Path.Combine(fixture,"part.xml")));Check(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(fixture,"part.xml"))),"Original wurde überschrieben.");});
            Test("Zyklische und fehlende Includes werden gemeldet",()=>{Write(fixture,"cycle.xml","<window><controls><import>cycle.xml</import><import>missing.xml</import></controls></window>");var p=new SkinProject(fixture);p.OpenPage("cycle.xml");Check(p.Warnings.Any(w=>w.Contains("Zyklische"))&&p.Warnings.Any(w=>w.Contains("fehlt")),"Warnungen fehlen.");});
            Test("Theme überschreibt Datei, fehlende Dateien fallen auf Basis zurück",()=>{Directory.CreateDirectory(Path.Combine(fixture,"Themes","Test"));Write(Path.Combine(fixture,"Themes","Test"),"part.xml","<window><controls><control><type>label</type><id>0</id><label>Theme</label></control></controls></window>");var p=new SkinProject(fixture){Theme="Test"};p.OpenPage("main.xml");Check(p.Controls[0].Get("label")=="Theme"&&p.Controls[0].Source.RelativePath=="Themes/Test/part.xml","Theme-Resolver fehlerhaft.");});
            Test("Unbekannte Vorschau-Bedingung ist unbekannt, nicht wahr",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");var s=new PreviewState(p);Check(s.Evaluate("#(eq(#plugin.a,'B'))",new HashSet<string>())==null,"Unbekannte Bedingung stillschweigend akzeptiert.");});
            Test("Vorschau-Boolesche Bedingungen und Plugin-Zustand",()=>{var p=new SkinProject(fixture);p.OpenPage("main.xml");var s=new PreviewState(p);Check(s.Evaluate("[window.isvisible(0)|window.isvisible(35)]+!plugin.isenabled(World Weather)",new HashSet<string>())==true,"Boolesche Bedingung falsch.");Check(s.Evaluate("no+unknown.test()",new HashSet<string>())==false,"Dreiwertige Logik falsch.");});
            Test("Echte Titan-Dateien laden; Uhr gezielt ändern; Animationskommentare bleiben",()=>{var p=new SkinProject(StudioForm.DemoRoot());p.OpenPage("demo.xml");var time=p.Controls.First(c=>c.Get("description")=="Time");string source=time.Source.Text;int before=Int32.Parse(time.Get("posX"));p.Edit(time,Set("posX",(before-100).ToString()),"Uhr verschieben");var after=p.Controls.First(c=>c.Get("description")=="Time");Check(after.Source.RelativePath=="common.time.xml"&&after.Source.Text.Contains("<!--label>#(iif"),"Quellzuordnung oder Kommentar verloren.");Check(after.Source.Text.Contains("<animation effect=\"fade\" time=\"250\">WindowOpen</animation>"),"Animation verloren.");p.UndoChange();Check(p.Controls.First(c=>c.Get("description")=="Time").Source.Text==source,"Titan-Undo nicht identisch.");});
            RunDesignerTests(root);
            RunProjectFileTests(root);
            RunEditorStateTests(root);
            RunSkinAuditTests(root);
            RunPropertyResetTests(root);
            RunPreviewSettingsTests(root);
            RunStructuralTests(root);
            RunElementSearchTests();
            RunGroupTests(root);
            RunTextEditTests(root);
            RunLanguageTests(root);RunConditionScopeTests(root);RunStackTests(root);RunCollectionTests(root);
            report.Add("");report.Add(passed+" bestanden, "+failed+" fehlgeschlagen.");
            File.WriteAllLines(Path.Combine(root,"test-results.txt"),report.ToArray(),new UTF8Encoding(false));
            return failed==0?0:1;
        }
    }
}
