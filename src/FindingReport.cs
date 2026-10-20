using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;

namespace MP1SkinStudio
{
    public static class FindingReport
    {
        public const string Scope="Only the open page and loaded includes are checked. This is not a full MP1 runtime test. Bindings are preserved.";
        public static SkinIssue[] Filter(SkinAuditReport report,string query,bool information)
        {
            query=(query??"").Trim();
            return report.Issues.Where(i=>(information||!i.Information)&&(query.Length==0||String.Join(" ",new[]{i.ControlName,i.Property,i.Location,i.Message,i.Code,i.Detail}).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        }
        static string Value(string value){return value??"";}
        static string Html(string value){return SecurityElement.Escape(Value(value));}
        public static string Render(SkinAuditReport report,string query,bool information,bool preview,bool html)
        {
            if(report==null)throw new ArgumentNullException("report");
            var issues=Filter(report,query,information);string title=preview?"Unverified preview":"Page audit";
            string[] context={"Page: "+Value(report.Page),"Theme: "+(String.IsNullOrEmpty(report.Theme)?"Default skin":report.Theme),"Search: "+(String.IsNullOrWhiteSpace(query)?"(none)":query.Trim()),"Include informational findings: "+(information?"yes":"no"),"Shown: "+issues.Length+" of "+report.Issues.Count+" findings ("+report.Warnings+" audit; "+report.Information+" informational).",Scope,"This saved snapshot is not refreshed when the project changes. Run the check again after edits."};
            var text=new StringBuilder();
            if(html)
            {
                text.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>").Append(Html(title)).Append(" — MP1 Skin Studio</title><style>body{font:16px system-ui,sans-serif;max-width:1000px;margin:32px auto;padding:0 20px;color:#202936;background:#f5f7fa}article{background:white;border:1px solid #ccd4df;border-radius:8px;padding:18px;margin:18px 0}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:inherit}h2{font-size:19px}p{overflow-wrap:anywhere}.context{color:#435267}</style></head><body><h1>").Append(Html(title)).Append(" — MP1 Skin Studio</h1><div class=\"context\">");
                foreach(string line in context)text.Append("<p>").Append(Html(line)).Append("</p>");text.Append("</div>");
            }
            else{text.AppendLine(title+" — MP1 Skin Studio");foreach(string line in context)text.AppendLine(line);}
            if(issues.Length==0)text.Append(html?"<p>No findings match this filter. This does not establish runtime compatibility.</p>":"\r\nNo findings match this filter. This does not establish runtime compatibility.\r\n");
            for(int i=0;i<issues.Length;i++)
            {
                var issue=issues[i];string heading=(i+1)+". "+Value(issue.Code)+" · "+(issue.Information?"Information":"Audit finding");
                string[] lines={"Element: "+(String.IsNullOrEmpty(issue.ControlName)?"Page":issue.ControlName),"Property: "+Value(issue.Property),"Source: "+issue.Location,"Finding: "+Value(issue.Message)};
                if(html){text.Append("<article><h2>").Append(Html(heading)).Append("</h2>");foreach(string line in lines)text.Append("<p>").Append(Html(line)).Append("</p>");text.Append("<pre>").Append(Html(issue.Detail)).Append("</pre></article>");}
                else{text.AppendLine().AppendLine(heading);foreach(string line in lines)text.AppendLine(line);text.AppendLine().AppendLine(Value(issue.Detail));}
            }
            if(html)text.Append("</body></html>");return text.ToString();
        }
        public static void Save(SkinAuditReport report,string query,bool information,bool preview,string target)
        {
            string extension=Path.GetExtension(target);bool html=extension.Equals(".html",StringComparison.OrdinalIgnoreCase);
            if(!html&&!extension.Equals(".txt",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Save finding reports as .txt or .html.");
            string content=Render(report,query,information,preview,html),temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,content,new UTF8Encoding(false));if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
