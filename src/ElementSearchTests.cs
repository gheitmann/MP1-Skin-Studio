using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunElementSearchTests()
        {
            Test("Elementsuche kombiniert Wörter über Name, Quelle und Eigenschaften",()=>
            {var c=Named(Demo(),"Filme");Check(ElementSearch.Matches(c,"FILME menu button")&&!ElementSearch.Matches(c,"Filme serien")&&ElementSearch.Matches(c,"  \t "),"Suchwörter nicht korrekt kombiniert.");});
            Test("ID-Suche vergleicht ganze numerische IDs statt Teilzeichenfolgen",()=>
            {var c=Named(Demo(),"Filme");Check(ElementSearch.Matches(c,"id:0010")&&!ElementSearch.Matches(c,"id:1")&&!ElementSearch.Matches(c,"id:")&&!ElementSearch.Matches(c,"id:abc"),"ID nicht exakt geprüft.");});
            Test("Gezielte Suche trennt Typ, Text und Quelldatei",()=>
            {var c=Named(Demo(),"Filme");Check(ElementSearch.Matches(c,"typ:BUTTON quelle:menu text:Fil")&&!ElementSearch.Matches(c,"typ:but")&&!ElementSearch.Matches(c,"text:menu"),"Feldsuche uneindeutig.");});
            Test("Bindungen und Grafiknamen bleiben als Suchwerte auffindbar",()=>
            {var p=Demo();Check(ElementSearch.Matches(Named(p,"Time"),"#time")&&ElementSearch.Matches(Named(p,"Filme"),"Schaltflaeche-blau.png")&&!ElementSearch.Matches(Named(p,"Filme"),"texture"),"Bindung oder Grafik nicht gefunden; oder Eigenschaftsnamen irrtümlich gesucht.");});
            Test("Elementsuche verändert weder XML noch Undo oder Editorzustand",()=>
            {var p=Demo();string before=ProjectFile.ContentSignature(p);foreach(var c in p.Controls)ElementSearch.Matches(c,"typ:button quelle:menu");Check(!p.Dirty&&p.Undo.Count==0&&ProjectFile.ContentSignature(p)==before,"Suche verändert Projekt.");});
        }
    }
    public sealed partial class StudioForm
    {
        SkinControl[] VisibleTreeControls(){return TreeControlNodes().Select(n=>(SkinControl)n.Tag).ToArray();}
        void RunSearchUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");var film=project.Controls.First(c=>c.Name=="Filme");SelectControl(film);fields["width"].Text="512";string signature=ProjectFile.ContentSignature(project);int total=project.Controls.Count;
            elementSearch.Text="typ:button quelle:menu";
            verify(VisibleTreeControls().Length==3&&project.Controls.Count==total&&fields["width"].Text=="512"&&ProjectFile.ContentSignature(project)==signature,"Suchfilter reduziert nur den Baum und erhält offene Eingaben sowie die vollständige Vorschau");
            tree.SelectedNode=tree.Nodes[0].Nodes[1];verify(selected.Key==film.Key&&fields["width"].Text=="512"&&((SkinControl)tree.SelectedNode.Tag).Key==film.Key,"Treffer im Baum lässt offene Eigenschaftseingaben und die vorherige Auswahl bestehen");
            bool blocked=false;try{NextSearchResult(false);}catch(InvalidOperationException){blocked=true;}
            verify(blocked&&selected.Key==film.Key&&fields["width"].Text=="512","Trefferauswahl blockiert bei noch nicht übernommenen Eigenschaften");Apply();
            NextSearchResult(false);verify(selected.Name=="Serien"&&((SkinControl)tree.SelectedNode.Tag).Key==selected.Key,"Enter-Auswahl springt zum nächsten Treffer und markiert den Baum");
            NextSearchResult(true);verify(selected.Name=="Filme","Rückwärtssuche findet den vorherigen Treffer");
            NextSearchResult(true);verify(selected.Name=="Musik","Rückwärtssuche läuft am Listenanfang zum letzten Treffer");
            elementSearch.Text="id:10";verify(VisibleTreeControls().Length==1&&VisibleTreeControls()[0].Name=="Filme","Gezielte ID-Suche filtert Include-Control im echten Baum");
            elementSearch.Text="kein-treffer-xyz";string key=selected.Key;NextSearchResult(false);
            verify(tree.Nodes.Count==0&&selected.Key==key&&status.Text.StartsWith("Keine passenden"),"Leere Trefferliste lässt die aktuelle Auswahl und Eigenschaften bestehen");
            var escape=new KeyEventArgs(Keys.Escape);SearchKey(escape);verify(elementSearch.Text.Length==0&&VisibleTreeControls().Length==total&&escape.SuppressKeyPress,"Escape löscht Suchfilter und stellt alle Baumknoten wieder her");
            SelectControl(project.Controls.First(c=>c.Name=="Filme"));elementSearch.Text="id:10";undo.PerformClick();verify(selected.Get("width")=="470"&&VisibleTreeControls().Single().Get("width")=="470","Undo aktualisiert gefilterte Control-Objekte ohne veraltete Treffer");
            page.SelectedIndex=page.FindStringExact("demo.ebenen.xml");verify(elementSearch.Text=="id:10"&&tree.Nodes.Count==0,"Seitenwechsel wendet bestehenden Suchfilter auf die neue Seite an");
            elementSearch.Clear();canvas.Update();var point=canvas.PointForTest(950,650);var hits=canvas.OverlappingAt(point);
            verify(String.Join(",",hits.Select(c=>c.Name))=="Ebene C,Ebene B,Ebene A","Treffer unter dem Mauszeiger sind in sichtbarer Reihenfolge von vorn nach hinten sortiert");
            project.Editor.Locked.Add(hits[0].Key);string before=ProjectFile.ContentSignature(project);elementSearch.Text="name:A";canvas.ExercisePick(point);var menu=StudioMenus.PeekForTest(this,"overlap");
            verify(menu.Items.Count==5&&menu.Items[2].Text.Contains("[S]")&&menu.Items[2].Enabled,"Überlagerungsliste enthält auch gesperrte Elemente zum Prüfen");menu.ChooseForTest(2);Application.DoEvents();
            verify(selected.Name=="Ebene C"&&fields.Values.All(b=>b.ReadOnly)&&tree.SelectedNode==null&&ProjectFile.ContentSignature(project)==before,"Echtes Rechtsklickmenü wählt verdecktes gesperrtes Element trotz Baumfilter ohne XML-Änderung");
            canvas.ExercisePick(point);menu=StudioMenus.PeekForTest(this,"overlap");menu.ChooseForTest(3);Application.DoEvents();
            verify(selected.Name=="Ebene B"&&project.Undo.Count==0,"Wiederverwendete Überlagerungsliste wählt mittlere Ebene ohne Drag oder Undo-Schritt");
            project.Editor.Hidden.Add(selected.Key);hits=canvas.OverlappingAt(point);verify(hits.Length==2&&hits.All(c=>c.Name!="Ebene B"),"Im Editor ausgeblendete Elemente erscheinen nicht unter dem Mauszeiger");
            var a=project.Controls.First(c=>c.Name=="Ebene A");project.Edit(a,SelfTestsForSearch("visible","false"),"Bedingung");RefreshProject(a.Key);canvas.ShowHidden=false;hits=canvas.OverlappingAt(point);
            verify(hits.Length==1&&hits[0].Name=="Ebene C","Bedingt unsichtbare Controls bleiben aus der Überlagerungsliste ausgefiltert");canvas.ShowHidden=true;
            verify(canvas.OverlappingAt(point).Length==2,"Versteckte-zeigen bezieht bedingt unsichtbare Controls ein, Editor-Ausblendung bleibt wirksam");
            verify(canvas.OverlappingAt(canvas.PointForTest(-20,200)).Length==0,"Außerhalb der Skinfläche werden keine Controls angeboten");
            canvas.ShowHidden=false;LoadProject(DemoRoot(),"demo.xml");verify(elementSearch.Text.Length==0,"Projektwechsel setzt die vorübergehende Elementsuche zurück");
        }
        static System.Collections.Generic.Dictionary<string,string> SelfTestsForSearch(string key,string value){return new System.Collections.Generic.Dictionary<string,string>{{key,value}};}
    }
}
