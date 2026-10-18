using System;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject AuditFixture(string root,string name,string body)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);
            Write(folder,"references.xml","<controls><skin><width>1920</width><height>1080</height></skin></controls>");
            Write(folder,"main.xml","<window><controls>"+body+"</controls></window>");
            var p=new SkinProject(folder);p.OpenPage("main.xml");return p;
        }
        static void RunSkinAuditTests(string root)
        {
            Test("Seitenprüfung findet numerisch identische Fokus-IDs und mehrdeutige Navigation",()=>
            {
                var p=AuditFixture(root,"audit-ids","<control><type>button</type><id>10</id><onright>010</onright></control><control><type>button</type><id>010</id></control>");
                var report=SkinAudit.Run(p);Check(report.Issues.Count(i=>i.Code=="DUPLICATE_ID")==2&&report.Issues.Any(i=>i.Code=="NAV_AMBIGUOUS"),"IDs oder Verweis nicht erkannt.");
                Check(report.Issues.First(i=>i.Code=="DUPLICATE_ID").Detail.Contains("Bedingungen"),"Bedingte Vorkommen wurden als sicherer Laufzeitfehler ausgegeben.");
            });
            Test("Wiederholte ID 0 und nicht geladene Seiten erzeugen keinen Fokus-ID-Konflikt",()=>
            {
                var p=AuditFixture(root,"audit-zero","<control><type>label</type><id>0</id></control><control><type>label</type><id>0</id></control><control><type>button</type><id>10</id><onright>00</onright></control>");
                Write(p.Root,"other.xml","<window><controls><control><type>button</type><id>10</id></control></controls></window>");
                Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="DUPLICATE_ID"||i.Code=="NAV_MISSING"),"Null-ID oder andere Seite falsch geprüft.");
            });
            Test("Fehlendes Ziel wird mit echter Include-Zeile und Eigenschaft gemeldet",()=>
            {
                var p=AuditFixture(root,"audit-lines","<import>part.xml</import>");
                string part="<window>\r\n<controls>\r\n<control>\r\n<type>button</type><id>10</id>\r\n<onright>987</onright>\r\n</control></controls></window>";Write(p.Root,"part.xml",part);p.OpenPage("main.xml");
                var issue=SkinAudit.Run(p).Issues.Single(i=>i.Code=="NAV_MISSING");
                Check(issue.Source=="part.xml"&&issue.Line==5&&issue.ControlKey==p.Controls[0].Key&&issue.Property=="onright"&&issue.Detail.Contains("987"),"Quellzuordnung fehlerhaft.");
            });
            Test("Dynamische und besondere Navigationswerte werden als ungeprüft behandelt",()=>
            {
                var p=AuditFixture(root,"audit-dynamic-nav","<control><type>button</type><id>10</id><onright>#plugin.target</onright><onleft>-1</onleft><onup>xyz</onup><ondown>0</ondown></control>");
                var report=SkinAudit.Run(p);Check(report.Issues.Count(i=>i.Code=="NAV_UNCHECKED"&&i.Information)==3&&!report.Issues.Any(i=>i.Code=="NAV_MISSING"),"Ungeprüfte Werte wurden als fehlendes Ziel ausgegeben.");
            });
            Test("Vorhandenes Ziel mit unbekanntem Fokustyp bleibt ein Hinweis",()=>
            {
                var p=AuditFixture(root,"audit-type","<control><type>button</type><id>10</id><onright>20</onright></control><control><type>pluginwidget</type><id>20</id></control>");
                var report=SkinAudit.Run(p);Check(report.Issues.Any(i=>i.Code=="NAV_TARGET_TYPE"&&i.Information)&&!report.Issues.Any(i=>i.Code=="NAV_MISSING"),"Editorbegrenzung wurde als fehlendes Control ausgegeben.");
            });
            Test("Bildprüfung verwendet Theme vor Basis und meldet auch ausgeblendete Bilder",()=>
            {
                var p=AuditFixture(root,"audit-theme","<control><type>image</type><id>0</id><texture>base.png</texture></control><control><type>image</type><id>0</id><texture>theme.png</texture></control><control><type>image</type><id>0</id><visible>no</visible><texture>missing.png</texture></control>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));Write(Path.Combine(p.Root,"Media"),"base.png","existence-only");
                string themeMedia=Path.Combine(p.Root,"Themes","Test","Media");Directory.CreateDirectory(themeMedia);Write(themeMedia,"theme.png","existence-only");p.Theme="Test";p.OpenPage("main.xml");
                p.Editor.Hidden.Add(p.Controls[2].Key);var report=SkinAudit.Run(p);
                Check(report.Issues.Count(i=>i.Code=="TEXTURE_MISSING")==1&&report.Issues.Single(i=>i.Code=="TEXTURE_MISSING").Message.Contains("missing.png"),"Theme-/Basisauflösung oder versteckte Bilder falsch geprüft.");
            });
            Test("Auflösbare Defines werden geprüft; unbekannte Bildbindungen bleiben Hinweise",()=>
            {
                var p=AuditFixture(root,"audit-defines","<control><type>image</type><texture>#asset</texture></control><control><type>image</type><texture>#plugin.cover</texture></control>");
                string path=Path.Combine(p.Root,"main.xml");Write(p.Root,"main.xml",File.ReadAllText(path).Replace("<window>","<window><define>#asset:missing.png</define>"));p.Sources.Clear();p.OpenPage("main.xml");
                var report=SkinAudit.Run(p);Check(report.Issues.Count(i=>i.Code=="TEXTURE_MISSING")==1&&report.Issues.Count(i=>i.Code=="TEXTURE_UNCHECKED"&&i.Information)==1&&p.Controls[0].Get("texture")=="#asset","Bindung verändert oder falsch klassifiziert.");
            });
            Test("Mehrfach eingebundene Quelle wird in jedem unterschiedlichen Define-Kontext geprüft",()=>
            {
                var p=AuditFixture(root,"audit-contexts","<import>first.xml</import><import>part.xml</import><import>second.xml</import><import>part.xml</import>");
                Write(p.Root,"first.xml","<window><define>#asset:exists.png</define><controls/></window>");Write(p.Root,"second.xml","<window><define>#asset:missing.png</define><controls/></window>");Write(p.Root,"part.xml","<window><controls><control><type>image</type><texture>#asset</texture></control></controls></window>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));Write(Path.Combine(p.Root,"Media"),"exists.png","existence-only");p.OpenPage("main.xml");
                var report=SkinAudit.Run(p);Check(report.Issues.Count(i=>i.Code=="TEXTURE_MISSING")==1&&report.Issues.Single(i=>i.Code=="TEXTURE_MISSING").Detail.Contains("Aufgelöst: missing.png"),"Zweiter Include-Kontext verloren.");
            });
            Test("Prüfung verändert weder XML noch Projekt-, Sperr- und Undo-Zustand",()=>
            {
                var p=AuditFixture(root,"audit-readonly","<control><type>button</type><id>10</id><onright>999</onright><textureNoFocus>missing.png</textureNoFocus></control>");p.Editor.Locked.Add(p.Controls[0].Key);p.Editor.Zoom=2;
                string signature=ProjectFile.ContentSignature(p),text=p.Controls[0].Source.Text;int sources=p.Sources.Count;
                SkinAudit.Run(p);SkinAudit.Run(p);Check(signature==ProjectFile.ContentSignature(p)&&p.Controls[0].Source.Text==text&&p.Sources.Count==sources&&!p.Dirty&&p.Undo.Count==0&&p.Redo.Count==0,"Prüfung hat Bearbeitungszustand verändert.");
            });
            Test("Nicht geladene Includes erscheinen als Seitenfund ohne erfundenen Control-Verweis",()=>
            {
                var p=AuditFixture(root,"audit-load","<import>missing.xml</import>");var issue=SkinAudit.Run(p).Issues.First(i=>i.Code=="LOAD_WARNING");
                Check(String.IsNullOrEmpty(issue.ControlKey)&&issue.Message.Contains("missing.xml")&&issue.Location=="Seitenmodell","Laderwarnung falsch zugeordnet.");
            });
        }
    }
}
