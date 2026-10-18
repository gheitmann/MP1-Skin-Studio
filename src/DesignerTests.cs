using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject Demo(){var p=new SkinProject(StudioForm.DemoRoot());p.OpenPage("demo.xml");return p;}
        static SkinControl Named(SkinProject p,string name){return p.Controls.First(c=>c.Get("description")==name);}
        static void RunDesignerTests(string root)
        {
            Test("Mehrere Dateien ändern: ein Undo/Redo stellt beide exakt wieder her",()=>
            {
                var p=Demo();var a=Named(p,"Filme");var b=Named(p,"Time");string sa=a.Source.Text,sb=b.Source.Text;
                p.EditMany(new[]{new ControlEdit(a,Set("posX","220")),new ControlEdit(b,Set("posX","1700"))},"gemeinsam");
                Check(p.Undo.Count==1&&p.Sources.Values.Count(s=>s.Changed)==2,"Keine gemeinsame Transaktion.");
                p.UndoChange();Check(a.Source.Text==sa&&b.Source.Text==sb&&!p.Dirty,"Mehrdateien-Undo unvollständig.");
                p.RedoChange();Check(Named(p,"Filme").Get("posX")=="220"&&Named(p,"Time").Get("posX")=="1700","Mehrdateien-Redo unvollständig.");
            });
            Test("Fehler im zweiten Element rollt gesamte Mehrfachänderung zurück",()=>
            {
                var p=Demo();var a=Named(p,"Filme");var b=Named(p,"Time");string sa=a.Source.Text,sb=b.Source.Text;
                Reject(()=>p.EditMany(new[]{new ControlEdit(a,Set("posX","400")),new ControlEdit(b,Set("id","9"))},"fehlerhaft"));
                Check(a.Source.Text==sa&&b.Source.Text==sb&&!p.Dirty&&p.Undo.Count==0,"Fehlgeschlagene Transaktion hinterlässt Änderungen.");
            });
            Test("Einfügen erzeugt vollständiges Control; Undo/Redo erhält Identität",()=>
            {
                var p=Demo();string before=Named(p,"Filme").Source.Text;int count=p.Controls.Count;
                string key=p.InsertControl("demo.menu.xml","button");var c=p.Controls.First(x=>x.Key==key);string id=c.Get("id");
                Check(p.Controls.Count==count+1&&SkinProject.Focusable(c)&&Int32.Parse(id)>0&&p.Controls.Count(x=>x.Get("id")==id)==1,"Neue Schaltfläche ungültig.");
                p.UndoChange();Check(Named(p,"Filme").Source.Text==before&&p.Controls.Count==count,"Einfügen-Undo unvollständig.");p.RedoChange();Check(p.Controls.First(x=>x.Key==key).Get("id")==id,"Redo vergibt andere Identität.");
            });
            Test("Text- und Bildvorlagen werden als gültiges XML eingefügt",()=>
            {
                var p=Demo();string a=p.InsertControl("demo.menu.xml","label"),b=p.InsertControl("demo.menu.xml","image");
                Check(p.Controls.First(c=>c.Key==a).Get("type")=="label"&&p.Controls.First(c=>c.Key==b).Get("type")=="image","Vorlagen fehlerhaft.");
                Check(p.Controls.First(c=>c.Key==a).Get("id")!=p.Controls.First(c=>c.Key==b).Get("id"),"Neue IDs kollidieren.");
            });
            Test("Neue ID berücksichtigt nicht geöffnete Seiten und Navigationsziele",()=>
            {
                string folder=Path.Combine(root,"id-scan");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>10</id></control></controls></window>");
                Write(folder,"other.xml","<window><controls><control><type>button</type><id>10000</id><onleft>10001</onleft></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Check(p.NextControlId()==10002,"Andere Seite oder reserviertes Navigationsziel übersehen.");
            });
            Test("Duplizieren erhält Original, Kommentare, Styles und unbekannte Einträge",()=>
            {
                var p=Demo();var original=Named(p,"Filme");string raw=original.Source.Text.Substring(original.Node.Start,original.Node.End-original.Node.Start);
                string key=p.DuplicateControl(original,false);var clone=p.Controls.First(c=>c.Key==key);
                Check(original.Source.Text.Contains(raw),"Original-Control verändert.");
                Check(clone.Get("id")!="10"&&clone.Get("posX")=="170"&&clone.Get("posY")=="610","Neue Identität oder Versatz fehlt.");
                Check(clone.Node.Child("editorUnknown")!=null&&clone.Source.Text.Substring(clone.Node.Start,clone.Node.End-clone.Node.Start).Contains("Dieser Kommentar"),"Unbekannte XML-Daten der Kopie verloren.");
            });
            Test("Gebundene Positionen werden beim Duplizieren geschützt",()=>
            {
                string folder=Path.Combine(root,"clone-bindings");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><define>#x:120</define><controls><control><type>label</type><id>0</id><posX>#x</posX><posY>20</posY></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Reject(()=>p.DuplicateControl(p.Controls[0],false));Check(!p.Dirty,"Abgewiesene Kopie verändert Original.");
                string key=p.DuplicateControl(p.Controls[0],true);Check(p.Controls.First(c=>c.Key==key).Get("posX")=="150","Freigegebener Define-Versatz falsch.");
            });
            Test("Ausrichten berücksichtigt rechtsbündige Label-Anker",()=>
            {
                var p=Demo();var time=Named(p,"Time");var film=Named(p,"Filme");
                var edits=LayoutTools.Align(new[]{time,film},"left",false);p.EditMany(edits,"links");
                Check(LayoutTools.Bounds(Named(p,"Time")).Left==LayoutTools.Bounds(Named(p,"Filme")).Left,"Label-Anker beim Ausrichten falsch.");
            });
            Test("Gleiche horizontale Abstände erhalten äußere Kanten",()=>
            {
                var p=Demo();p.Edit(Named(p,"Serien"),Set("posX","800"),"verschieben");
                var selection=new[]{Named(p,"Filme"),Named(p,"Serien"),Named(p,"Musik")};
                p.EditMany(LayoutTools.Align(selection,"hspace",false),"verteilen");
                var a=LayoutTools.Bounds(Named(p,"Filme"));var b=LayoutTools.Bounds(Named(p,"Serien"));var c=LayoutTools.Bounds(Named(p,"Musik"));
                Check(a.Left==140&&c.Right==1770&&Math.Abs((b.Left-a.Right)-(c.Left-b.Right))<=1,"Abstände oder äußere Kanten falsch.");
            });
            Test("Ausrichten überschreibt gebundene Position nicht ohne Freigabe",()=>
            {
                var p=Demo();p.Edit(Named(p,"Filme"),Set("posX","#unresolved"),"Bindung");
                Reject(()=>LayoutTools.Align(new[]{Named(p,"Filme"),Named(p,"Musik")},"left",false));
                Check(Named(p,"Filme").Get("posX")=="#unresolved","Bindung verloren.");
            });
            Test("Navigation verbindet nur gewählte Richtung und ist rückgängig",()=>
            {
                var p=Demo();string before=Named(p,"Filme").Source.Text;
                p.ConnectNavigation(Named(p,"Filme"),Named(p,"Musik"),"onright",false);
                Check(p.NavigationTarget(Named(p,"Filme"),"onright").Name=="Musik"&&Named(p,"Filme").Get("onleft")=="12","Falsche Richtung geändert.");
                p.UndoChange();Check(Named(p,"Filme").Source.Text==before,"Navigation-Undo verändert andere Daten.");
            });
            Test("Navigation lehnt doppelte ID und nicht fokussierbares Ziel ab",()=>
            {
                var p=Demo();Reject(()=>p.ConnectNavigation(Named(p,"Filme"),Named(p,"Time"),"onright",false));
                string folder=Path.Combine(root,"duplicate-nav");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>1</id></control><control><type>button</type><id>2</id></control><control><type>button</type><id>2</id></control></controls></window>");
                var q=new SkinProject(folder);q.OpenPage("main.xml");Reject(()=>q.ConnectNavigation(q.Controls[0],q.Controls[1],"onright",false));Check(!q.Dirty,"Mehrdeutige Verbindung gespeichert.");
            });
            Test("Navigation schützt dynamische Zielbindung",()=>
            {
                var p=Demo();p.Edit(Named(p,"Filme"),Set("onright","#target"),"Bindung");
                Reject(()=>p.ConnectNavigation(Named(p,"Filme"),Named(p,"Musik"),"onright",false));
                p.ConnectNavigation(Named(p,"Filme"),Named(p,"Musik"),"onright",true);Check(Named(p,"Filme").Get("onright")=="12","Freigegebene Verbindung nicht gespeichert.");
            });
            Test("Navigation erkennt numerisch gleiche IDs mit führenden Nullen",()=>
            {
                string folder=Path.Combine(root,"numeric-ids");Directory.CreateDirectory(folder);
                Write(folder,"main.xml","<window><controls><control><type>button</type><id>1</id></control><control><type>button</type><id>02</id></control><control><type>button</type><id>2</id></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");Reject(()=>p.ConnectNavigation(p.Controls[0],p.Controls[2],"onright",false));Check(!p.Dirty,"Numerisch doppelte ID nicht erkannt.");
            });
            Test("Leerer selbstschließender Controls-Bereich ist erweiterbar und exakt rückgängig",()=>
            {
                string folder=Path.Combine(root,"empty-controls");Directory.CreateDirectory(folder);string xml="<window><id>0</id><controls retain=\"yes\" /></window>";Write(folder,"main.xml",xml);
                var p=new SkinProject(folder);p.OpenPage("main.xml");p.InsertControl("main.xml","label");Check(p.Controls.Count==1&&p.Controls[0].Source.Text.Contains("retain=\"yes\""),"Leerer Bereich oder Attribut beschädigt.");p.UndoChange();Check(p.Source(p.Resolve("main.xml")).Text==xml,"Undo normalisiert Original.");
            });
            Test("Nicht auflösbare Geometrie wird auch nach Bindungsfreigabe nicht geraten",()=>
            {
                var p=Demo();p.Edit(Named(p,"Filme"),Set("posX","#(unknown())"),"Formel");
                Reject(()=>LayoutTools.Align(new[]{Named(p,"Filme"),Named(p,"Musik")},"left",true));Reject(()=>p.DuplicateControl(Named(p,"Filme"),true));
                Check(Named(p,"Filme").Get("posX")=="#(unknown())","Formel durch geratenen Wert ersetzt.");
            });
        }
    }
}
