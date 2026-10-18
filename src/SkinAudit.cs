using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed class SkinIssue
    {
        public string Code,ControlKey,ControlName,Property,Source,Message,Detail;
        public int Line;
        public bool Information;
        public string Location{get{return String.IsNullOrEmpty(Source)?"Page model":Source+(Line>0?":"+Line:"");}}
    }
    public sealed class SkinAuditReport
    {
        public string Page,Theme;
        public readonly List<SkinIssue> Issues=new List<SkinIssue>();
        public int Warnings{get{return Issues.Count(i=>!i.Information);}}
        public int Information{get{return Issues.Count(i=>i.Information);}}
    }
    // Read-only checks of the expanded current page, never a claim to validate
    // the complete MP1 runtime. Bindings and conditions remain in the source.
    public static class SkinAudit
    {
        static readonly string[] directions={"onleft","onright","onup","ondown"};
        static SkinIssue Issue(SkinControl control,string property,string code,string message,string detail,bool information=false)
        {
            var node=control.Node.Child(property)??control.Node;
            int line=1;for(int index=0;index<node.Start;index++)if(control.Source.Text[index]=='\n')line++;
            PropertyValue value;string origin=control.Properties.TryGetValue(property,out value)?value.Origin:"not set";
            return new SkinIssue{Code=code,ControlKey=control.Key,ControlName=control.Name,Property=property,Source=control.Source.RelativePath,Line=line,Message=message,Information=information,
                Detail=detail+"\r\n\r\nEigenschaft: "+property+"\r\nXML value: "+control.Get(property)+"\r\nResolved: "+control.Resolved(property)+"\r\nHerkunft: "+origin+"\r\nFundstelle im Control: "+control.Source.RelativePath+":"+line};
        }
        static bool Dynamic(string value){return value.IndexOf('#')>=0;}
        public static SkinAuditReport Run(SkinProject project)
        {
            if(project==null)throw new ArgumentNullException("project");
            var report=new SkinAuditReport{Page=project.Page,Theme=project.Theme};
            var occurrences=project.Controls.GroupBy(c=>c.Key).ToDictionary(g=>g.Key,g=>g.ToArray());
            var controls=occurrences.Values.Select(g=>g[0]).ToArray();
            var byId=project.LogicalControls().Select(c=>new{Control=c,Id=SkinProject.ControlId(c)}).Where(x=>x.Id.HasValue&&x.Id.Value>0).GroupBy(x=>x.Id.Value).ToDictionary(g=>g.Key,g=>g.Select(x=>x.Control).ToArray());
            foreach(var group in byId.OrderBy(g=>g.Key))
            {
                if(group.Value.Length<2||!group.Value.Any(SkinProject.Focusable))continue;
                string locations=String.Join("\r\n",group.Value.Select(c=>c.Name+" — "+c.Source.RelativePath+" ("+c.Key+")"));
                foreach(var control in group.Value.GroupBy(c=>c.Key).Select(g=>g.First()))
                    report.Issues.Add(Issue(control,"id","DUPLICATE_ID","ID "+group.Key+" occurs more than once.","At least one occurrence is focusable. Navigation targets may be ambiguous in the loaded page model. Visibility and include conditions may make occurrences mutually exclusive; this is not proof of a runtime error.\r\n\r\nOccurrences:\r\n"+locations));
            }
            var languageState=new PreviewState(project);
            var exists=new Dictionary<string,bool>(StringComparer.OrdinalIgnoreCase);
            foreach(var control in controls)
            {
                if(project.Editor.Language.Active&&(new[]{"label","fadelabel","textbox","textboxscrollup"}.Contains(control.Get("type"))||control.Get("type").Contains("button")))
                {
                    foreach(var occurrence in occurrences[control.Key].GroupBy(c=>c.Resolved("label")).Select(g=>g.First()))
                    {int languageId;string text,origin;if(languageState.TryLabelId(occurrence,occurrence.Get("label"),out languageId)&&!project.Editor.Language.TryGet(languageId,out text,out origin))report.Issues.Add(Issue(occurrence,"label","LANG_MISSING","Language text ID "+languageId+" is missing from the primary and fallback languages.","Only loaded language snapshots were checked. The XML ID remains unchanged. Additional plugin-specific language resources may exist."));}
                }
                if(SkinProject.Focusable(control))foreach(string direction in directions)
                {
                    // The same source control can be included with different define
                    // contexts. Check each distinct resolved value, not just the first.
                    foreach(var occurrence in occurrences[control.Key].GroupBy(c=>c.Resolved(direction)).Select(g=>g.First()))
                    {
                        string value=occurrence.Resolved(direction).Trim();if(value.Length==0||value=="0")continue;
                        int id;
                        if(Dynamic(value)||!Int32.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out id)||id<0)
                        {
                            report.Issues.Add(Issue(occurrence,direction,"NAV_UNCHECKED","Navigation values are not checked statically here.","Bindings, expressions and special values are not reported as missing IDs. Check the actual target in MediaPortal.",true));continue;
                        }
                        if(id==0)continue;
                        SkinControl[] targets;
                        if(!byId.TryGetValue(id,out targets))report.Issues.Add(Issue(occurrence,direction,"NAV_MISSING","Navigation target ID "+id+" is missing from the page model.","The open page and its loaded includes contain no control with this positive ID. Plugin-generated controls and special runtime rules are not simulated."));
                        else if(targets.Length>1)report.Issues.Add(Issue(occurrence,direction,"NAV_AMBIGUOUS","Navigation target ID "+id+" is ambiguous in the page model.","The target has multiple occurrences. Their visibility conditions have not been proven mutually exclusive. Compare occurrences and conditions before editing."));
                        else if(!SkinProject.Focusable(targets[0]))report.Issues.Add(Issue(occurrence,direction,"NAV_TARGET_TYPE","Target type is unsupported by navigation testing.","The target exists: "+targets[0].Name+" ("+targets[0].Get("type")+"). The editor does not yet support this type as a focusable target. This alone does not prove an MP1 error.",true));
                    }
                }
                foreach(string property in control.Properties.Keys.Where(k=>k.StartsWith("texture",StringComparison.OrdinalIgnoreCase)).OrderBy(k=>k,StringComparer.Ordinal))
                {
                    foreach(var occurrence in occurrences[control.Key].GroupBy(c=>c.Resolved(property)).Select(g=>g.First()))
                    {
                        string value=occurrence.Resolved(property).Trim();if(value.Length==0||value=="-")continue;
                        if(Dynamic(value)){report.Issues.Add(Issue(occurrence,property,"TEXTURE_UNCHECKED","Image path is dynamic; file not checked statically.","The image path cannot be determined without a runtime value. The binding is preserved.",true));continue;}
                        try
                        {
                            string path=project.Resolve("Media/"+value);bool found;if(!exists.TryGetValue(path,out found)){found=File.Exists(path);exists[path]=found;}
                            if(!found)report.Issues.Add(Issue(occurrence,property,"TEXTURE_MISSING","Image file not found: "+value,"Checked Media in the selected theme with fallback to the base skin, including hidden controls. Cache and plugin images outside the skin directory are not simulated."));
                        }
                        catch(Exception ex){report.Issues.Add(Issue(occurrence,property,"TEXTURE_PATH","Could not validate image path.","The path is unchanged. Reason: "+ex.Message,true));}
                    }
                }
            }
            foreach(string warning in project.Warnings.Distinct())report.Issues.Add(new SkinIssue{Code="LOAD_WARNING",Message=warning,Detail="Finding from the existing XML/include loader for the open page. Validation covers only the loaded page model.",ControlName="Page",Property="",Source=""});
            return report;
        }
    }
}
