using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunTextEditTests(string root)
        {
            Test("Textänderung erhält unbekanntes XML und ist bytegenau rückgängig",()=>
            {var p=Demo();var film=Named(p,"Filme");string before=film.Source.Text;Check(p.SetControlText(film,"Kino & Café <heute>",false)&&Named(p,"Filme").Get("label")=="Kino & Café <heute>"&&Named(p,"Filme").Source.Text.Contains("Kino &amp; Café &lt;heute&gt;"),"Text nicht korrekt gespeichert.");Check(Named(p,"Filme").Node.Child("editorUnknown")!=null&&p.Undo.Count==1,"XML-Erweiterung oder atomarer Schritt verloren.");p.UndoChange();Check(Named(p,"Filme").Source.Text==before,"Undo nicht exakt.");p.RedoChange();Check(Named(p,"Filme").Get("label")=="Kino & Café <heute>","Redo verliert Text.");});
            Test("Datenbindung wird nur nach expliziter Freigabe ersetzt",()=>
            {var p=Demo();var c=Named(p,"Time");var info=p.InspectTextEdit(c);Check(info.Bound&&info.NeedsApproval&&!p.SetControlText(c,info.Raw,false),"Bindung oder unveränderter Wert falsch erkannt.");Reject(()=>p.SetControlText(c,"Abend",false));Check(!p.Dirty&&p.Undo.Count==0,"Abgewiesene Änderung schreibt XML.");p.SetControlText(c,"Abend",true);Check(Named(p,"Time").Get("label")=="Abend","Freigegebener Wert nicht gesetzt.");});
            Test("Numerische Texte bleiben als mögliche Sprachressourcen-ID geschützt",()=>
            {var p=ResetFixture(root,"text-numeric");var c=p.Controls[0];p.Edit(c,Set("label","123"),"Sprach-ID");c=p.Controls[0];Check(p.InspectTextEdit(c).LanguageId,"Numerische ID nicht erkannt.");Reject(()=>p.SetControlText(c,"Neuer Text",false));p.SetControlText(c,"Neuer Text",true);Check(p.Controls[0].Get("label")=="Neuer Text","Freigabe wirkungslos.");});
            Test("Geerbter Text wird lokal überschrieben ohne Style oder Referenz zu verändern",()=>
            {string folder=Path.Combine(root,"text-inherited");Directory.CreateDirectory(folder);Write(folder,"references.xml","<controls><style Name=\"Title\"><label>#title</label></style></controls>");Write(folder,"main.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id></control></controls></window>");var p=new SkinProject(folder);p.OpenPage("main.xml");string refs=p.Source(p.Resolve("references.xml")).Text;Check(p.InspectTextEdit(p.Controls[0]).Bound,"Geerbte Bindung nicht geschützt.");Reject(()=>p.SetControlText(p.Controls[0],"Titel",false));p.SetControlText(p.Controls[0],"Titel",true);Check(p.Controls[0].Node.Child("label")!=null&&p.Source(p.Resolve("references.xml")).Text==refs,"Style verändert.");p.UndoChange();Check(p.Controls[0].Get("label")=="#title"&&p.Controls[0].Node.Child("label")==null,"Geerbter Zustand nach Undo falsch.");});
            Test("Texteditor lehnt Sperren, Gruppen, Bilder und veraltete Controls ab",()=>
            {var p=Demo();var film=Named(p,"Filme");p.Editor.Locked.Add(film.Key);Reject(()=>p.SetControlText(film,"Neu",false));p.Editor.Locked.Clear();p.SetControlText(film,"Neu",false);Reject(()=>p.SetControlText(film,"Alt",false));p.OpenPage("demo.gruppen.xml");Check(!p.InspectTextEdit(Named(p,"Panel")).Allowed&&!p.InspectTextEdit(Named(p,"Medienbereich")).Allowed,"Ungeeigneter Typ zugelassen.");});
            Test("Ungültiger oder zu langer Text verändert weder Quelle noch Historie",()=>
            {var p=Demo();var c=Named(p,"Filme");string before=c.Source.Text;Reject(()=>p.SetControlText(c,"x\u0001",false));Reject(()=>p.SetControlText(c,new string('x',32769),false));Reject(()=>p.SetControlText(c,null,false));Check(c.Source.Text==before&&p.Undo.Count==0,"Ungültiger Text hatte Nebenwirkungen.");Check(p.SetControlText(c,"",false)&&Named(p,"Filme").Get("label")=="","Leerer Text nicht möglich.");});
            Test("Doppelte und verschachtelte label-Einträge bleiben geschützt",()=>
            {var p=AuditFixture(root,"text-ambiguous","<control><type>label</type><label>A</label><label>B</label></control><control><type>label</type><label><part>C</part></label></control>");Check(p.Controls.All(c=>!p.InspectTextEdit(c).Allowed),"Mehrdeutiger Text editierbar.");foreach(var c in p.Controls)Reject(()=>p.SetControlText(c,"Neu",true));Check(!p.Dirty,"Geschütztes XML verändert.");});
            Test("Textänderung erhält umgebende Leerzeichen und Zeilenumbrüche",()=>
            {var p=Demo();p.SetControlText(Named(p,"Filme"),"  Erste Zeile\nZweite Zeile  ",false);Check(Named(p,"Filme").Get("label")=="  Erste Zeile\nZweite Zeile  ","Text wurde getrimmt oder umgebrochen.");Check(!p.SetControlText(Named(p,"Filme"),"  Erste Zeile\nZweite Zeile  ",false)&&p.Undo.Count==1,"Unveränderter Text erzeugt zusätzlichen Schritt.");});
        }
    }
    public sealed partial class StudioForm
    {
        bool DriveTextDialog(Action open,Action<TextEditDialog> action)
        {
            bool seen=false;
            using(var timer=new Timer{Interval=30})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<TextEditDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();seen=true;action(dialog);if(!dialog.IsDisposed&&dialog.DialogResult==DialogResult.None){dialog.DialogResult=DialogResult.Cancel;dialog.Close();}};
                timer.Start();open();timer.Stop();
            }
            return seen;
        }
        void RunTextEditUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");var time=project.Controls.First(c=>c.Name=="Time");
            using(var dialog=new TextEditDialog(project.InspectTextEdit(time),state))
            {
                dialog.Show(this);Application.DoEvents();verify(dialog.CanAccept&&dialog.PreviewForTest=="18:42","Textdialog zeigt gebundenen Originalwert mit aktuellen Beispieldaten und erlaubt unverändertes Schließen");
                dialog.TextForTest("Neue Uhr");verify(!dialog.CanAccept,"Geänderter gebundener Text verlangt eigene ausdrückliche Freigabe");dialog.ApproveForTest();verify(dialog.CanAccept&&dialog.PreviewForTest=="Neue Uhr","Freigabe aktiviert Übernahme und Vorschau aktualisiert sich");dialog.Close();
            }
            var film=project.Controls.First(c=>c.Name=="Filme");string before=film.Source.Text;bool seen=DriveTextDialog(()=>canvas.ExerciseTextDoubleClick(film),dialog=>{dialog.TextForTest("Kino & Café");dialog.AcceptForTest();});
            verify(seen&&selected.Name=="Filme"&&selected.Get("label")=="Kino & Café"&&project.Undo.Count==1,"Canvas-Doppelklick öffnet Texteditor und übernimmt Text als einen Undo-Schritt");undo.PerformClick();verify(selected.Source.Text==before,"Undo nach Doppelklick stellt XML einschließlich unbekannter Daten exakt wieder her");redo.PerformClick();verify(selected.Get("label")=="Kino & Café","Redo nach Textbearbeitung stellt neuen Wert wieder her");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Name=="Serien"));canvas.NativeView();canvas.Snap=false;string oldX=selected.Get("posX");canvas.ExerciseDrag(selected,1,0,false);verify(selected.Get("posX")==oldX&&!project.Dirty&&project.Undo.Count==0,"Kleine Mausbewegung beim Klicken verändert auch ohne Raster keine Position");canvas.Snap=true;canvas.FitView();
            SelectControl(project.Controls.First(c=>c.Name=="Filme"));var menu=(StudioMenu)ProtectionMenu();int item=menu.Items.Cast<ToolStripItem>().ToList().FindIndex(i=>i.Text.StartsWith("Text bearbeiten",StringComparison.Ordinal));
            seen=DriveTextDialog(()=>{menu.Show(tree,new System.Drawing.Point(12,12));menu.ChooseForTest(item);},dialog=>{dialog.TextForTest("Verworfen");dialog.DialogResult=DialogResult.Cancel;dialog.Close();});verify(seen&&!project.Dirty&&project.Undo.Count==0&&!menu.IsDisposed,"Echtes Element-Menü öffnet Texteditor; Abbrechen verändert weder XML noch Historie");
            fields["width"].Text="512";canvas.ExerciseTextDoubleClick(project.Controls.First(c=>c.Name=="Serien"));verify(selected.Name=="Filme"&&fields["width"].Text=="512"&&!project.Dirty,"Doppelklick und vorgeschaltete Mausauswahl verwerfen keine offenen Eigenschaftseingaben");fields["width"].Text="470";
            tree.SelectedNode=tree.Nodes[0];tree.Focus();seen=DriveTextDialog(()=>{var message=new Message();ProcessCmdKey(ref message,Keys.F2);},dialog=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();});verify(!seen&&!project.Dirty,"F2 auf einem Dateiknoten bearbeitet nicht versehentlich das vorherige Control");
            canvas.Focus();seen=DriveTextDialog(()=>{var message=new Message();ProcessCmdKey(ref message,Keys.F2);},dialog=>{dialog.TextForTest("Filme");dialog.AcceptForTest();});verify(seen&&!project.Dirty&&project.Undo.Count==0,"F2 auf der Arbeitsfläche öffnet Texteditor; unverändertes Übernehmen erzeugt keinen Schritt");
            time=project.Controls.First(c=>c.Name=="Time");seen=DriveTextDialog(()=>EditElementText(time),dialog=>{dialog.TextForTest("Guten Abend");dialog.ApproveForTest();dialog.AcceptForTest();});verify(seen&&selected.Get("label")=="Guten Abend"&&project.Undo.Count==1,"Gebundener Text lässt sich im Dialog ausdrücklich lokal ersetzen");undo.PerformClick();verify(selected.Get("label")=="#time","Undo stellt die ursprüngliche Datenbindung wieder her");
            project.Editor.Locked.Add(selected.Key);EditElementText(selected);verify(!project.Dirty&&status.Text.Contains("gesperrt"),"Gesperrter Text öffnet keinen schreibenden Dialog");project.Editor.Locked.Clear();
            LoadProject(DemoRoot(),"demo.gruppen.xml");var title=project.Controls.First(c=>c.Name=="Titel");project.Edit(project.Controls.First(c=>c.Name=="Medienbereich"),new System.Collections.Generic.Dictionary<string,string>{{"layout","StackLayout"}},"Layout");RefreshProject(title.Key);title=project.Controls.First(c=>c.Name=="Titel");seen=DriveTextDialog(()=>EditElementText(title),dialog=>{dialog.TextForTest("Neue Sammlung");dialog.AcceptForTest();});verify(seen&&selected.AutomaticLayout&&selected.Get("label")=="Neue Sammlung","Text bleibt innerhalb automatischer Gruppenlayouts ausdrücklich bearbeitbar");
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state)){dialog.Show(this);Application.DoEvents();dialog.TextForTest("Zeile 1\nZeile 2");dialog.CommitShortcutForTest();verify(dialog.Result.Contains("Zeile 1")&&dialog.Result.Contains("Zeile 2"),"Strg+Enter übernimmt mehrzeilige Texte ohne Trim");}
            project.SetControlText(selected,"  Zeile 1\nZeile 2  ",false);RefreshProject(selected.Key);var info=project.InspectTextEdit(selected);using(var dialog=new TextEditDialog(info,state)){dialog.Show(this);Application.DoEvents();dialog.AcceptForTest();verify(dialog.Result==info.Raw&&!project.SetControlText(selected,dialog.Result,false),"Unverändertes mehrzeiliges Texteingabefeld erhält Leerzeichen, Zeilenumbrüche und Originalwert");}
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
