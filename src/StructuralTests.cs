using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject StructureFixture(string root,string name)
        {
            return AuditFixture(root,name,"<!--outside--><control custom=\"keep\"><description>A</description><type>label</type><id>1</id><label>A</label><!--inside--></control>\n<control><description>G</description><type>group</type><id>2</id><control><description>Child</description><type>label</type><id>0</id><label>Kind</label></control></control>\n<control><description>B</description><type>button</type><id>3</id><label>B</label></control>");
        }
        static void RunStructuralTests(string root)
        {
            Test("Projekt mit vollständig geleerten Quellen lässt sich öffnen, rückgängig machen und wiederholen",()=>
            {
                var p=ResetFixture(root,"structure-empty-project");p.DeleteControls(p.Controls,false,true);string file=Path.Combine(root,"structure-empty.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(loaded.Controls.Count==0,"Leerer Stand nicht wiederhergestellt.");loaded.UndoChange();Check(loaded.Controls.Count==2&&!loaded.Dirty,"Leere Quellen nicht wiederhergestellt.");loaded.RedoChange();Check(loaded.Controls.Count==0,"Redo leerer Quellen falsch.");
            });
            Test("Ersetztes Control mit gleicher Ordinalposition übernimmt keine Identität oder Sperre des Originals",()=>
            {
                var p=AuditFixture(root,"structure-replacement","<control><type>label</type><id>0</id><description>Original</description></control>");p.DeleteControls(p.Controls);string key=p.InsertControl("main.xml","button");p.Editor.Locked.Add(key);string file=Path.Combine(root,"structure-replacement.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls[0].Name=="Original"&&!loaded.IsLocked(loaded.Controls[0]),"Sperre der neuen Identität dem Original gegeben.");loaded.RedoChange();Check(loaded.Controls[0].Get("type")=="button"&&loaded.IsLocked(loaded.Controls[0]),"Neue Identität oder Sperre bei Redo verloren.");
            });
            Test("Veraltete Control-Objekte können nach Strukturänderungen keine anderen Controls bearbeiten",()=>
            {
                var p=StructureFixture(root,"structure-stale");var old=Named(p,"A");p.ReorderControl(old,"front");string signature=ProjectFile.ContentSignature(p);
                Reject(()=>p.Edit(old,Set("label","Falsch"),"veraltet"));Reject(()=>p.ResetProperty(new[]{old},"label",false));Reject(()=>p.DuplicateControl(old,false));
                Check(signature==ProjectFile.ContentSignature(p)&&Named(p,"A").Get("label")=="A","Veraltete Auswahl hat anderes Control geändert.");
            });
            Test("Ebenenwechsel erhält rohe Controls, Erweiterungen, IDs und exaktes Undo/Redo",()=>
            {
                var p=StructureFixture(root,"structure-order");string before=p.Controls[0].Source.Text;var raw=p.Controls.Where(c=>c.Node.Parent.Name=="controls").ToDictionary(c=>c.Name,c=>c.Source.Text.Substring(c.Node.Start,c.Node.End-c.Node.Start));
                p.ReorderControl(Named(p,"A"),"front");string after=p.Controls[0].Source.Text;
                Check(p.Controls.Select(c=>c.Name).SequenceEqual(new[]{"G","Child","B","A"})&&raw.All(pair=>after.Contains(pair.Value))&&after.Contains("<!--outside-->"),"Rohtext oder Reihenfolge verloren.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&!p.Dirty,"Undo nicht exakt.");p.RedoChange();Check(p.Controls[0].Source.Text==after,"Redo nicht exakt.");
            });
            Test("Sperren und Ausblendungen folgen überquerten Controls und verschachtelten Kindern",()=>
            {
                var p=StructureFixture(root,"structure-flags");p.Editor.Locked.Add(Named(p,"Child").Key);p.Editor.Hidden.Add(Named(p,"B").Key);
                p.ReorderControl(Named(p,"A"),"front");Check(p.IsLocked(Named(p,"Child"))&&!p.IsLocked(Named(p,"A"))&&p.IsEditorHidden(Named(p,"B")),"Markierung am falschen Control.");
                p.UndoChange();Check(p.IsLocked(Named(p,"Child"))&&p.IsEditorHidden(Named(p,"B")),"Undo verliert Markierungen.");
            });
            Test("Nachträglich gesetzte Editor-Markierungen werden beim Undo übersetzt statt überschrieben",()=>
            {
                var p=StructureFixture(root,"structure-later-flags");p.ReorderControl(Named(p,"G"),"front");p.Editor.Hidden.Add(Named(p,"Child").Key);p.Editor.Locked.Add(Named(p,"B").Key);p.UndoChange();
                Check(p.IsEditorHidden(Named(p,"Child"))&&p.IsLocked(Named(p,"B"))&&!p.IsLocked(Named(p,"G")),"Spätere Einstellung auf alten Gesamtstand zurückgesetzt.");
            });
            Test("Gesperrtes Kind blockiert Änderung seiner Gruppe ohne Teiländerung",()=>
            {
                var p=StructureFixture(root,"structure-locked-group");p.Editor.Locked.Add(Named(p,"Child").Key);string before=ProjectFile.ContentSignature(p);
                Reject(()=>p.ReorderControl(Named(p,"G"),"front"));Reject(()=>p.DeleteControls(new[]{Named(p,"G")},true,true));Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Gruppenschutz umgangen.");
            });
            Test("Ebenengrenzen und Include-Barrieren bleiben No-op ohne zusätzlichen Undo-Schritt",()=>
            {
                var p=AuditFixture(root,"structure-boundary","<control><type>label</type><description>A</description></control><import>part.xml</import><control><type>label</type><description>B</description></control>");Write(p.Root,"part.xml","<window><controls/></window>");p.OpenPage("main.xml");p.ReorderControl(Named(p,"A"),"front");
                Check(!p.Dirty&&p.Undo.Count==0,"Über Include hinweg verschoben.");
                var q=StructureFixture(root,"structure-modes");q.ReorderControl(Named(q,"A"),"forward");Check(q.Controls.First().Name=="G","Eine Ebene vor falsch.");q.ReorderControl(Named(q,"A"),"backward");Check(q.Controls.First().Name=="A","Eine Ebene zurück falsch.");q.ReorderControl(Named(q,"B"),"back");Check(q.Controls.First().Name=="B","Ganz nach hinten falsch.");
            });
            Test("Entfernen einer Gruppe zählt Kinder und stellt XML und Ausblendungen wieder her",()=>
            {
                var p=StructureFixture(root,"structure-delete-group");string before=p.Controls[0].Source.Text;p.Editor.Hidden.Add(Named(p,"Child").Key);var plan=p.InspectDeletion(new[]{Named(p,"G"),Named(p,"Child")});
                Check(plan.Roots.Length==1&&plan.RemovedKeys.Count==2,"Eltern-/Kind-Auswahl doppelt gezählt.");p.DeleteControls(plan.Roots);Check(p.Controls.Count==2&&p.Editor.Hidden.Count==0,"Gruppe oder verwaiste Markierungen erhalten.");
                p.UndoChange();Check(p.Controls[0].Source.Text==before&&p.IsEditorHidden(Named(p,"Child")),"Wiederherstellung unvollständig.");p.RedoChange();Check(p.Controls.Count==2,"Redo entfernt Gruppe nicht.");
            });
            Test("Verweisprüfung findet Navigation und Sichtbarkeit und ignoriert gemeinsam entfernte Verweise",()=>
            {
                var p=AuditFixture(root,"structure-refs","<control><description>A</description><type>button</type><id>10</id><onright>11</onright></control><control><description>B</description><type>button</type><id>11</id><onleft>10</onleft><visible>control.hasfocus(10)</visible></control>");
                var plan=p.InspectDeletion(new[]{Named(p,"A")});Check(plan.References.Count==2,"Navigations- oder Sichtbarkeitsverweis fehlt.");Reject(()=>p.DeleteControls(new[]{Named(p,"A")}));Check(!p.Dirty,"Verweisschutz ändert XML.");
                Check(p.InspectDeletion(p.Controls).References.Count==0,"Interne Verweise wurden als verbleibend gemeldet.");p.DeleteControls(p.Controls);Check(p.Controls.Count==0,"Mehrfachentfernen unvollständig.");
            });
            Test("Include-Entfernung erfordert Freigabe und bleibt eine dateiübergreifende Transaktion",()=>
            {
                var p=ResetFixture(root,"structure-shared");var plan=p.InspectDeletion(p.Controls);Check(plan.SharedFiles.SequenceEqual(new[]{"part.xml"}),"Include nicht gekennzeichnet.");Reject(()=>p.DeleteControls(p.Controls));Check(!p.Dirty,"Teiländerung vor Freigabe.");p.DeleteControls(p.Controls,false,true);Check(p.Controls.Count==0&&p.Undo.Count==1,"Keine gemeinsame Transaktion.");p.UndoChange();Check(p.Controls.Count==2&&!p.Dirty,"Gemeinsames Undo unvollständig.");
            });
            Test("Einfüge-Undo entfernt Markierungen und Redo stellt sie am neuen Control wieder her",()=>
            {
                var p=StructureFixture(root,"structure-insert-flags");string key=p.InsertControl("main.xml","label");p.Editor.Hidden.Add(key);p.Editor.Locked.Add(key);p.UndoChange();Check(p.Editor.Hidden.Count==0&&p.Editor.Locked.Count==0,"Verwaiste Markierung nach Undo.");p.RedoChange();Check(p.IsLocked(p.Controls.First(c=>c.Key==key)),"Redo verliert Markierung.");p.UndoChange();string next=p.InsertControl("main.xml","button");Check(!p.IsLocked(p.Controls.First(c=>c.Key==next)),"Markierung auf anderes neues Control übertragen.");
            });
            Test("Gespeicherte Ebenenreihenfolge behält Identitäten und Markierungen beim Wiederherstellungs-Undo",()=>
            {
                var p=StructureFixture(root,"structure-project-order");p.Editor.Hidden.Add(Named(p,"Child").Key);p.ReorderControl(Named(p,"A"),"front");p.Editor.Locked.Add(Named(p,"B").Key);string file=Path.Combine(root,"structure-order.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.IsLocked(Named(loaded,"B")),"Projektzustand abweichend.");loaded.UndoChange();Check(loaded.Controls.First().Name=="A"&&loaded.IsLocked(Named(loaded,"B"))&&loaded.IsEditorHidden(Named(loaded,"Child")),"Undo verwechselt Elementidentitäten.");loaded.RedoChange();Check(loaded.Controls.Last().Name=="A"&&loaded.IsLocked(Named(loaded,"B")),"Redo verwechselt Identität.");
            });
            Test("Gespeicherte Entfernung übersetzt verbleibende Sperren beim Undo und Redo korrekt",()=>
            {
                var p=StructureFixture(root,"structure-project-delete");p.Editor.Locked.Add(Named(p,"B").Key);p.DeleteControls(new[]{Named(p,"A")});string file=Path.Combine(root,"structure-delete.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls.First().Name=="A"&&loaded.IsLocked(Named(loaded,"B"))&&!loaded.IsLocked(Named(loaded,"Child")),"Sperre auf falsches Element übertragen.");loaded.RedoChange();Check(loaded.Controls.Count==3&&loaded.IsLocked(Named(loaded,"B")),"Redo verliert Sperre.");
            });
            Test("Altes Projekt ohne Elementzuordnung lädt appendierte Controls kompatibel",()=>
            {
                var p=StructureFixture(root,"structure-legacy");p.InsertControl("main.xml","label");string file=Path.Combine(root,"structure-legacy.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","1");foreach(XmlElement node in doc.SelectNodes("/skinStudioProject/files/file"))node.RemoveAttribute("controlOrder");return doc.OuterXml;});var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(loaded.Controls.Count==4&&!loaded.Dirty,"Altes Projekt nicht kompatibel.");
            });
            Test("Projektformat 2/3 verlangt eine vollständige Elementzuordnung",()=>
            {
                var p=StructureFixture(root,"structure-missing-order");p.ReorderControl(Named(p,"A"),"front");string file=Path.Combine(root,"structure-missing.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);foreach(XmlElement node in doc.SelectNodes("/skinStudioProject/files/file"))node.RemoveAttribute("controlOrder");return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
            Test("Beschädigte Elementzuordnung wird vor Projektübernahme abgewiesen",()=>
            {
                var p=StructureFixture(root,"structure-invalid-order");p.ReorderControl(Named(p,"A"),"front");string file=Path.Combine(root,"structure-invalid.mp1skinproj");ProjectFile.Save(p,file);RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);((XmlElement)doc.SelectSingleNode("/skinStudioProject/files/file")).SetAttribute("controlOrder","0,0,2,3");return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunStructuralUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));string before=selected.Source.Text,oldKey=selected.Key;
            project.Editor.Hidden.Add(project.Controls.First(c=>c.Get("description")=="Serien").Key);project.Editor.Locked.Add(project.Controls.First(c=>c.Get("description")=="Musik").Key);
            var menu=(StudioMenu)ProtectionMenu();menu.Show(tree,new System.Drawing.Point(12,12));int command=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text=="Eine Ebene nach vorn");menu.ChooseForTest(command);Application.DoEvents();
            verify(selected.Name=="Filme"&&selected.Key!=oldKey&&project.IsEditorHidden(project.Controls.First(c=>c.Get("description")=="Serien"))&&project.IsLocked(project.Controls.First(c=>c.Get("description")=="Musik")),"Ebenenmenü hält Auswahl und Editor-Markierungen beim richtigen Element");
            undo.PerformClick();verify(selected.Name=="Filme"&&selected.Key==oldKey&&selected.Source.Text==before,"Ebenen-Undo stellt XML und ursprüngliche Auswahl wieder her");redo.PerformClick();verify(selected.Name=="Filme"&&selected.Key!=oldKey,"Ebenen-Redo wählt erneut dasselbe logische Element");
            fields["width"].Text="490";Apply();verify(selected.Get("width")=="490"&&project.Controls.First(c=>c.Get("description")=="Serien").Get("width")=="470","Eigenschaften bearbeiten nach Ebenenwechsel trifft weiterhin das ausgewählte Control");undo.PerformClick();undo.PerformClick();
            fields["posY"].Text="640";bool blocked=false;try{ReorderSelection("front");}catch(InvalidOperationException){blocked=true;}verify(blocked&&fields["posY"].Text=="640"&&!project.Dirty,"Offene Eigenschaftseingaben werden beim Ebenenwechsel nicht verworfen");
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));before=selected.Source.Text;var plan=project.InspectDeletion(SelectedControls());
            using(var dialog=new DeleteControlsDialog(plan)){verify(!dialog.RemovalEnabled,"Entfernen mit Verweisen und Include-Wirkung beginnt ohne Freigabe");dialog.ApproveForTest();verify(dialog.RemovalEnabled,"Explizite Freigaben aktivieren das Entfernen");}
            bool reviewed=false;using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<DeleteControlsDialog>().FirstOrDefault();if(dialog!=null){timer.Stop();reviewed=true;dialog.ApproveForTest();dialog.AcceptForTest();}};timer.Start();
                menu=(StudioMenu)ProtectionMenu();menu.Show(tree,new System.Drawing.Point(12,12));command=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Auswahl entfernen",StringComparison.Ordinal));menu.ChooseForTest(command);timer.Stop();
            }
            verify(reviewed&&!project.Controls.Any(c=>c.Get("description")=="Filme")&&selected.Get("description")=="Serien"&&!menu.IsDisposed,"Entfernen über Menü und Prüfdialog funktioniert und wählt das nächste Element");
            undo.PerformClick();verify(selected.Name=="Filme"&&selected.Source.Text==before&&project.Controls.First(c=>c.Get("description")=="Serien").Get("onleft")=="10","Entfernen-Undo stellt Auswahl, vollständiges XML und unveränderte Verweise wieder her");redo.PerformClick();verify(selected.Name=="Serien"&&!project.Controls.Any(c=>c.Name=="Filme"),"Entfernen-Redo stellt die Auswahl nach der Entfernung wieder her");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
