using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ScopeFixture(string root,string name,string main,string part,string alternate=null)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);Write(folder,"main.xml",main);Write(folder,"part.xml",part);if(alternate!=null)Write(folder,"alternate.xml",alternate);var p=new SkinProject(folder);p.OpenPage("main.xml");return p;
        }
        static void RunConditionScopeTests(string root)
        {
            Test("Reine Define-Includes laden Werte ohne falsche fehlende-Controls-Warnung",()=>
            {var p=ScopeFixture(root,"scope-defines","<window><controls><import>part.xml</import><control><type>label</type><id>1</id><visible>string.equals(#flag,Yes)</visible></control></controls></window>","<window><define>#flag:Yes</define></window>");Check(new PreviewState(p).Visible(p.Controls[0])==true&&!p.Warnings.Any(w=>w.Contains("Keine &lt;controls&gt;")||w.Contains("Keine <controls>")),"Define-Include nicht korrekt ausgewertet.");});
            Test("Gruppenbedingungen behalten ihren Kontext trotz Defines eingebundener Kinder",()=>
            {var p=ScopeFixture(root,"scope-parent","<window><define>#flag:Yes</define><controls><control><type>group</type><id>1</id><visible>string.equals(#flag,Yes)</visible><include>part.xml</include></control></controls></window>","<window><define>#flag:No</define><controls><control><type>button</type><id>8</id><visible>string.equals(#flag,No)</visible></control></controls></window>");var child=p.Controls.First(c=>c.Get("id")=="8");Check(child.RuntimeParent!=null&&new PreviewState(p).Visible(child)==true,"Elternbedingung mit Kind-Kontext ausgewertet oder reines Gruppen-Include übersehen.");});
            Test("Include und Otherwise verwenden den vor dem Laden festgehaltenen Bedingungskontext",()=>
            {var p=ScopeFixture(root,"scope-otherwise","<window><define>#flag:Yes</define><controls><include condition='string.equals(#flag,Yes)' otherwise='alternate.xml'>part.xml</include></controls></window>","<window><define>#flag:No</define><controls><control><type>label</type><id>8</id></control></controls></window>","<window><controls><control><type>label</type><id>9</id></control></controls></window>");var state=new PreviewState(p);Check(state.Visible(p.Controls.First(c=>c.Get("id")=="8"))==true&&state.Visible(p.Controls.First(c=>c.Get("id")=="9"))==false,"Otherwise verwendet veränderten Kontext.");});
            Test("String-Gleichheit respektiert zitierte Kommas und unbekannte Werte",()=>
            {var p=Demo();var s=new PreviewState(p);Check(s.Evaluate("string.equals('A,B','A,B')",new HashSet<string>())==true&&s.Evaluate("string.equals(#notKnown,'')",new HashSet<string>())==null,"Vergleich falsch zerlegt oder unbekannter Wert geraten.");});
            Test("Boolesche Define-Ketten werden aufgelöst und Zyklen bleiben unbekannt",()=>
            {var p=ScopeFixture(root,"scope-cycle","<window><define>#a:#b</define><define>#b:#a</define><define>#good:Yes</define><controls><control><type>label</type><id>1</id><visible>#a</visible></control><control><type>label</type><id>2</id><visible>#good</visible></control></controls></window>","<window/>");var s=new PreviewState(p);Check(s.Visible(p.Controls[0])==null&&s.Visible(p.Controls[1])==true,"Define-Kette oder Zyklus falsch.");});
            Test("Control.IsVisible wertet das Ziel in dessen eigenem Define-Kontext aus",()=>
            {var p=ScopeFixture(root,"scope-target","<window><define>#flag:Outer</define><controls><control><type>label</type><id>1</id><visible>control.isvisible(8)</visible></control><import>part.xml</import></controls></window>","<window><define>#flag:Inner</define><controls><control><type>label</type><id>8</id><visible>string.equals(#flag,Inner)</visible></control></controls></window>");Check(new PreviewState(p).Visible(p.Controls[0])==true,"Zielbedingung verwendet den aufrufenden Kontext.");});
            Test("Sperren und Ausblenden von Gruppen wirken auch auf eingebundene Kinder",()=>
            {var p=ScopeFixture(root,"scope-flags","<window><controls><control><type>group</type><id>1</id><include>part.xml</include></control><import>part.xml</import></controls></window>","<window><controls><control><type>button</type><id>8</id><label>Kind</label></control></controls></window>");var group=p.Controls.First(c=>c.Get("id")=="1");var children=p.Controls.Where(c=>c.Get("id")=="8").ToArray();p.Editor.Hidden.Add(group.Key);Check(p.IsEditorHidden(children[0])&&!p.IsEditorHidden(children[1]),"Ausblendung betrifft falsche Include-Verwendung.");p.Editor.Locked.Add(group.Key);Check(p.IsLocked(children[0])&&p.IsLocked(children[1]),"Gemeinsame Quelle umgeht geschützte Verwendung.");Reject(()=>p.Edit(children[1],Set("label","Neu"),"Ändern"));});
            Test("Gesperrtes eingebundenes Kind verhindert Entfernen seiner enthaltenden Gruppe",()=>
            {var p=ScopeFixture(root,"scope-delete-lock","<window><controls><control><type>group</type><id>1</id><include>part.xml</include></control></controls></window>","<window><controls><control><type>button</type><id>8</id></control></controls></window>");p.Editor.Locked.Add(p.Controls.First(c=>c.Get("id")=="8").Key);Reject(()=>p.DeleteControls(new[]{p.Controls[0]}));Check(!p.Dirty,"Geschützte Gruppe verändert.");});
            Test("Fenster-Standardfokus wird geladen ohne von einer Labelauswahl abhängig zu sein",()=>
            {var p=Demo();p.OpenPage("demo.sprache.xml");var s=new PreviewState(p);Check(p.DefaultControlId==501&&s.FocusId==501&&s.Evaluate("control.hasfocus(501)",new HashSet<string>())==true,"Standardfokus fehlt.");});
            Test("Klammern in zitierten Vergleichswerten verändern die Bedingungsstruktur nicht",()=>
            {var state=new PreviewState(Demo());Check(state.Evaluate("[string.equals(']','x')]+true",new HashSet<string>())==false,"Zitierte Klammer falsch als Ausdrucksgrenze gelesen.");});
            Test("Textur-Platzhalter Minus wird nicht als fehlende Grafik gemeldet",()=>
            {var p=AuditFixture(root,"texture-sentinel","<control><type>button</type><id>1</id><textureFocus>-</textureFocus><textureNoFocus>-</textureNoFocus><label>Test</label></control>");Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="TEXTURE_MISSING"),"Absichtlich leere Textur als Fehler gemeldet.");});
        }
    }
}
