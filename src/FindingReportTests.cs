using System;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunFindingReportTests(string root)
        {
            Test("Saved findings preserve include locations and exclude filtered information",()=>
            {
                var p=AuditFixture(root,"finding-report","<import>part.xml</import>");
                Write(p.Root,"part.xml","<window>\n<controls><control><type>button</type><id>10</id>\n<onright>999</onright><textureFocus>#unknown</textureFocus></control></controls></window>");p.OpenPage("main.xml");
                var report=SkinAudit.Run(p);string before=ProjectFile.ContentSignature(p),original=File.ReadAllText(Path.Combine(p.Root,"part.xml"));
                string target=Path.Combine(root,"finding-report.txt");FindingReport.Save(report,"NAV_MISSING",false,false,target);string text=File.ReadAllText(target);
                Check(text.Contains("part.xml:3")&&text.Contains("999")&&text.Contains("Shown: 1 of ")&&!text.Contains(". TEXTURE_UNCHECKED")&&text.Contains(FindingReport.Scope),"Report lost filter, scope or include location.");
                Check(ProjectFile.ContentSignature(p)==before&&!p.Dirty&&p.Undo.Count==0&&File.ReadAllText(Path.Combine(p.Root,"part.xml"))==original,"Reporting changed XML or editor state.");
            });
            Test("HTML finding reports encode all XML-derived fields as text",()=>
            {
                var report=new SkinAuditReport{Page="<script>page</script>",Theme="<img src=x>"};
                report.Issues.Add(new SkinIssue{Code="<code>",ControlName="<svg onload=x>",Source="<source>",Property="<property>",Message="<iframe>",Detail="A & B\n<script>alert('x')</script>"});
                string text=FindingReport.Render(report,"",true,true,true);
                Check(!text.Contains("<script>")&&!text.Contains("<svg ")&&!text.Contains("<iframe>")&&!text.Contains("<img src=")&&text.Contains("&lt;script&gt;")&&text.Contains("A &amp; B")&&text.Contains("Unverified preview"),"Source markup became active HTML.");
            });
            Test("An empty saved filter explains its scope instead of claiming compatibility",()=>
            {
                var report=new SkinAuditReport{Page="main.xml"};report.Issues.Add(new SkinIssue{Information=true,Code="UNVERIFIED",Detail="unknown"});
                foreach(bool html in new[]{false,true}){string text=FindingReport.Render(report,"",false,false,html);Check(text.Contains("Shown: 0 of 1")&&text.Contains("No findings match this filter")&&text.Contains("does not establish runtime compatibility")&&text.Contains("not refreshed"),"Empty snapshot was misleading.");}
            });
            Test("Failed report replacement retains the old report and removes temporary output",()=>
            {
                string target=Path.Combine(root,"locked-finding.txt");File.WriteAllText(target,"previous report");
                using(var locked=new FileStream(target,FileMode.Open,FileAccess.Read,FileShare.Read))Reject(()=>FindingReport.Save(new SkinAuditReport(),"",true,false,target));
                Check(File.ReadAllText(target)=="previous report"&&!Directory.GetFiles(root,"locked-finding.txt.*.tmp").Any(),"Failed report save damaged the old file or left temporary output.");
            });
            Test("Report saving rejects skin and project destinations before writing",()=>
            {
                foreach(string extension in new[]{".xml",".mp1skinproj",".zip"}){string target=Path.Combine(root,"preserve-report"+extension);File.WriteAllText(target,"original");Reject(()=>FindingReport.Save(new SkinAuditReport(),"",true,false,target));Check(File.ReadAllText(target)=="original","Report overwrote a source/project destination.");}
            });
        }
    }
}
