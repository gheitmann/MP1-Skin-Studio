using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject GroupDemo(){var p=Demo();p.OpenPage("demo.gruppen.xml");return p;}
        static void RunGroupTests(string root)
        {
            Test("Gruppeninhalt umfasst eindeutige Blatt-Controls ohne die Gruppenrahmen",()=>
            {var p=GroupDemo();var leaves=p.ContainedLeaves(Named(p,"Medienbereich"));Check(leaves.Length==4&&leaves.All(c=>!LayoutTools.IsContainer(c))&&p.ContainedLeaves(Named(p,"Aktionen")).Length==2&&!p.Dirty&&p.Undo.Count==0,"Blattauswahl verändert Struktur oder enthält Gruppenrahmen.");});
            Test("Freie Geschwister innerhalb einer Gruppe lassen sich atomar ausrichten",()=>
            {var p=GroupDemo();string before=Named(p,"Aktionen").Source.Text;p.EditMany(LayoutTools.Align(p.ContainedLeaves(Named(p,"Aktionen")),"left",false),"links");Check(Named(p,"Serien öffnen").Get("posX")=="330"&&Named(p,"Aktionen").Get("posX")=="300","Gruppenrahmen oder falsche Position verändert.");p.UndoChange();Check(Named(p,"Aktionen").Source.Text==before,"Gruppenausrichtung nicht exakt rückgängig.");});
            Test("Gruppenrahmen und unterschiedliche verschachtelte Eltern werden nicht ausgerichtet",()=>
            {var p=GroupDemo();Reject(()=>LayoutTools.Align(new[]{Named(p,"Medienbereich"),Named(p,"Aktionen")},"left",false));Reject(()=>LayoutTools.Align(p.ContainedLeaves(Named(p,"Medienbereich")),"left",false));Check(!p.Dirty,"Abgewiesenes Ausrichten verändert Projekt.");});
            Test("Automatische Layouts schützen auch tief verschachtelte Controls",()=>
            {var p=GroupDemo();p.Edit(Named(p,"Medienbereich"),Set("layout","StackLayout"),"Layout");Check(p.ContainedLeaves(Named(p,"Medienbereich")).All(c=>c.AutomaticLayout),"Layoutschutz nicht vererbt.");Reject(()=>LayoutTools.Align(p.ContainedLeaves(Named(p,"Aktionen")),"left",false));p.UndoChange();Check(p.Controls.All(c=>!c.AutomaticLayout),"Layoutschutz nach Undo veraltet.");});
            Test("Style-Layout und Include-Inhalte erben den Schutz automatischer Positionierung",()=>
            {
                string folder=Path.Combine(root,"group-layout-style");Directory.CreateDirectory(folder);Write(folder,"references.xml","<controls><style Name=\"auto\"><layout>StackLayout</layout></style></controls>");
                Write(folder,"part.xml","<window><controls><control><type>button</type><id>7</id></control></controls></window>");Write(folder,"main.xml","<window><controls><control Style=\"auto\"><type>group</type><id>5</id><control><type>label</type><id>6</id></control><import>part.xml</import></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");var included=p.Controls.Single(c=>c.Get("id")=="7");Check(included.AutomaticLayout,"Import verliert Layoutschutz.");Reject(()=>LayoutTools.RequireManualGeometry(included));
            });
            Test("Veraltete Gruppenobjekte werden bei Inhaltsauswahl abgewiesen",()=>
            {var p=GroupDemo();var group=Named(p,"Medienbereich");p.Edit(group,Set("description","Neu"),"Name");Reject(()=>p.ContainedLeaves(group));Check(p.ContainedLeaves(Named(p,"Neu")).Length==4,"Aktuelle Gruppe nicht auswählbar.");});
            Test("Mehrfach eingebundene XML-Controls behalten Layoutschutz in jeder aktiven Verwendung",()=>
            {
                string folder=Path.Combine(root,"group-layout-reuse");Directory.CreateDirectory(folder);Write(folder,"part.xml","<window><controls><control><type>button</type><id>7</id></control></controls></window>");Write(folder,"main.xml","<window><controls><import>part.xml</import><control><type>group</type><layout>StackLayout</layout><control><type>label</type><id>6</id></control><import>part.xml</import></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");var reused=p.Controls.Where(c=>c.Get("id")=="7").ToArray();Check(reused.Length==2&&reused.All(c=>c.AutomaticLayout),"Freie erste Einbindung umgeht Layoutschutz derselben XML-Quelle.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        SkinControl GroupNamed(string name){return project.Controls.First(c=>c.Name==name);}
        TreeNode NodeNamed(string name){return TreeControlNodes().First(n=>((SkinControl)n.Tag).Name==name);}
        void RunGroupUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.gruppen.xml");var media=GroupNamed("Medienbereich");string before=media.Source.Text;
            verify(NodeNamed("Filme öffnen").Parent==NodeNamed("Aktionen")&&NodeNamed("Aktionen").Parent==NodeNamed("Medienbereich")&&TreeControlNodes().Length==7,"Gruppenbaum zeigt XML-Verschachtelung ohne fehlende oder doppelte Controls");
            SelectControl(media);canvas.Update();canvas.ExerciseDrag(media,40,30,true);verify(!project.Dirty&&project.Undo.Count==0&&status.Text.Contains("Gruppenrahmen"),"Gruppenrahmen kann nicht versehentlich als einzelnes Rechteck skaliert werden");
            fields["width"].Text="1600";bool blocked=false;try{SelectContainedLeaves();}catch(InvalidOperationException){blocked=true;}verify(blocked&&fields["width"].Text=="1600"&&selected.Key==media.Key,"Inhaltsauswahl verwirft keine offenen Eigenschaftseingaben");fields["width"].Text="1500";
            elementSearch.Text="id:310";verify(SearchResults().Length==1&&TreeControlNodes().Length==3&&NodeNamed("Aktionen").Name=="search-context","Suchtreffer behält die vollständige Gruppenkette als Kontext, ohne sie als Treffer zu zählen");
            NextSearchResult(false);verify(selected.Name=="Filme öffnen"&&tree.SelectedNode==NodeNamed("Filme öffnen")&&NodeNamed("Aktionen").Text.Contains("[Gruppe]"),"Trefferwahl erreicht tief verschachteltes Control und erhält Gruppenmarkierung");
            SelectFoundControl(GroupNamed("Medienbereich"));ShowProtectionMenu(protectButton);var menu=StudioMenus.PeekForTest(this,"protection");int index=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Enthaltene Einzel-Controls auswählen");menu.ChooseForTest(index);Application.DoEvents();
            verify(SelectedControls().Length==4&&SelectedControls().All(c=>!LayoutTools.IsContainer(c))&&elementSearch.Text.Length==0&&!project.Dirty&&project.Undo.Count==0,"Gruppeninhalt über echtes Menü auswählen entfernt Suchfilter ohne XML oder Undo-Schritt");
            verify(TreeControlNodes().Where(n=>canvas.SelectedKeys.Contains(((SkinControl)n.Tag).Key)).All(n=>n.BackColor.ToArgb()==System.Drawing.Color.FromArgb(33,72,102).ToArgb()),"Mehrfachauswahl markiert auch tiefe Baumknoten");
            canvas.Update();canvas.ExerciseDrag(GroupNamed("Panel"),40,30,false);verify(GroupNamed("Panel").Get("posX")=="250"&&GroupNamed("Filme öffnen").Get("posX")=="370"&&GroupNamed("Serien öffnen").Get("posY")=="680"&&GroupNamed("Medienbereich").Get("posX")=="210"&&project.Undo.Count==1,"Ausgewählte Blatt-Controls verschieben sich gemeinsam, Gruppenrahmen bleibt erhalten");undo.PerformClick();verify(GroupNamed("Medienbereich").Source.Text==before&&SelectedControls().Length==4,"Undo der Inhaltsbewegung erhält vollständiges XML und Mehrfachauswahl");
            SelectControl(GroupNamed("Aktionen"));SelectContainedLeaves();AlignSelection("left");verify(GroupNamed("Serien öffnen").Get("posX")=="330","Freie Geschwister-Controls innerhalb einer Gruppe werden über die Oberfläche ausgerichtet");undo.PerformClick();
            SelectControl(GroupNamed("Medienbereich"));SetProtection(true,true);SelectContainedLeaves();verify(SelectedControls().All(c=>project.IsLocked(c))&&fields.Values.All(f=>f.ReadOnly),"Inhaltsauswahl respektiert geerbte Gruppensperre");
            SelectControl(GroupNamed("Medienbereich"));SetProtection(true,false);project.Edit(selected,new System.Collections.Generic.Dictionary<string,string>{{"layout","StackLayout"}},"Layout");RefreshProject(selected.Key);SelectControl(GroupNamed("Aktionen"));SelectContainedLeaves();canvas.Update();string layoutXml=selected.Source.Text;int steps=project.Undo.Count;canvas.ExerciseDrag(selected,40,20,false);verify(selected.Source.Text==layoutXml&&project.Undo.Count==steps&&status.Text.Contains("Layoutregeln"),"Drag in automatisch angeordneter Gruppe verändert kein Control");
            undo.PerformClick();SelectControl(GroupNamed("Aktionen"));StructuralAction(()=>project.DeleteControls(new[]{selected}));verify(!TreeControlNodes().Any(n=>((SkinControl)n.Tag).Name=="Aktionen")&&TreeControlNodes().Length==4,"Entfernen einer Untergruppe entfernt ihre Baumkette vollständig");undo.PerformClick();verify(NodeNamed("Filme öffnen").Parent==NodeNamed("Aktionen")&&GroupNamed("Medienbereich").Source.Text==before,"Undo stellt verschachtelte Baumstruktur und vollständigen Quelltext wieder her");
            elementSearch.Text="id:311";SelectFoundControl(GroupNamed("Serien öffnen"));project.Editor.Hidden.Add(selected.Key);RefreshProject(selected.Key);verify(NodeNamed("Serien öffnen").Text.StartsWith("[A]")&&NodeNamed("Aktionen").Name=="search-context","Editorstatus aktualisiert auch gefilterte tiefe Baumknoten");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
