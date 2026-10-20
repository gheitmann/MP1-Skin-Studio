using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    // Read-only explanations of the current explicit preview, not runtime errors.
    public static class PreviewProblems
    {
        static readonly HashSet<string> functions=new HashSet<string>(new[]{"window.isvisible","control.hasfocus","control.isvisible","control.hastext","control.hasthumb","plugin.isenabled","system.idletime","string.equals","string.contains","string.starts","not","and","or","L","iif","eq","neq","string.trim","string.ltrim","string.rtrim","string.replace","string.length","cint","add","sub"},StringComparer.OrdinalIgnoreCase);
        static string Unquoted(string text)
        {
            var value=new StringBuilder();char quote='\0';
            foreach(char c in text){if(quote!='\0'){if(c==quote)quote='\0';value.Append(' ');}else if(c=='\''||c=='"'){quote=c;value.Append(' ');}else value.Append(c);}return value.ToString();
        }
        static string Short(string text){return text.Length<=8192?text:text.Substring(0,8192)+" [truncated]";}
        static void Explain(PreviewState state,string expression,Dictionary<string,string> defines,Dictionary<int,SkinControl[]> ids,HashSet<string> notes,HashSet<int> active,int depth)
        {
            if(String.IsNullOrWhiteSpace(expression))return;
            if(depth>8){notes.Add("Control dependency inspection reached its depth limit.");return;}
            if(expression.Length>8192)notes.Add("Condition/text exceeds the 8192-character preview evaluation limit.");
            string scan=Unquoted(Short(expression));
            foreach(Match match in Regex.Matches(scan,@"#[A-Za-z_][A-Za-z0-9_.]*"))
            {
                string value;if(!state.TryOperand(match.Value,defines,out value))
                {string binding;notes.Add("Missing sample or unresolved define: "+match.Value+(defines!=null&&defines.TryGetValue(match.Value,out binding)?" (define: "+Short(binding)+")":"")+". Configure explicit data in Sample Data.");}
            }
            foreach(Match match in Regex.Matches(scan,@"\b([A-Za-z][A-Za-z0-9_.]*)\s*\("))
            {
                string name=match.Groups[1].Value;
                if(!functions.Contains(name))notes.Add("Unsupported function: "+name+(name.Equals("Control.IsVisble",StringComparison.OrdinalIgnoreCase)?". Check the spelling of Control.IsVisible; the source is preserved.":". Its runtime behavior is not inferred."));
                if(name.Equals("system.idletime",StringComparison.OrdinalIgnoreCase)&&!state.Project.Editor.Preview.Values.ContainsKey(IdlePreview.Key))notes.Add("Idle time has no explicit sample: "+IdlePreview.Key+". Use Idle-time preview or Sample Data.");
            }
            foreach(string condition in PlaybackPreview.Keys)
                if(scan.IndexOf(condition.Substring(9),StringComparison.OrdinalIgnoreCase)>=0&&!state.Project.Editor.Preview.Values.ContainsKey(condition))notes.Add("Playback has no explicit sample: "+condition+".");
            foreach(Match match in Regex.Matches(scan,@"\bcontrol\.(isvisible|hastext|hasthumb)\(\s*([0-9]+)\s*\)",RegexOptions.IgnoreCase))
            {
                int id;if(!Int32.TryParse(match.Groups[2].Value,out id))continue;SkinControl[] targets;
                if(!ids.TryGetValue(id,out targets)){if(!match.Groups[1].Value.Equals("isvisible",StringComparison.OrdinalIgnoreCase))notes.Add("Control "+id+" is absent; its runtime content cannot be verified.");continue;}
                if(targets.Length!=1){notes.Add("Control ID "+id+" is ambiguous in the expanded page ("+targets.Length+" occurrences). Conditional runtime selection is not inferred.");continue;}
                if(!active.Add(id)){notes.Add("Cyclic control dependency involving ID "+id+".");continue;}
                try
                {
                    var target=targets[0];string kind=match.Groups[1].Value.ToLowerInvariant();
                    if(kind=="isvisible")
                    {
                        if(state.EvaluateScoped(target.Get("visible"),target.Defines)==null)Explain(state,target.Get("visible"),target.Defines,ids,notes,active,depth+1);
                        foreach(var term in target.VisibilityTerms??new VisibilityTerm[0])if(state.EvaluateScoped(term.Expression,term.Defines)==null)Explain(state,term.Expression,term.Defines,ids,notes,active,depth+1);
                    }
                    else
                    {
                        string raw=kind=="hasthumb"?state.ImageTexture(target):target.Get("label");Explain(state,raw,target.Defines,ids,notes,active,depth+1);
                        string resolved;if(kind=="hasthumb"&&state.TryText(target,raw,out resolved)&&(Path.IsPathRooted(resolved)||resolved.Replace('\\','/').Split('/').Contains("..")))notes.Add("Control "+id+" uses runtime/cache artwork outside the skin Media directory; that cache is not simulated.");
                    }
                }
                finally{active.Remove(id);}
            }
        }
        static SkinIssue Issue(SkinControl control,string property,string code,string value,IEnumerable<string> notes,Dictionary<SkinSource,int[]> lineCache)
        {
            var node=control.Node.Child(property)??control.Node;int[] breaks;
            if(!lineCache.TryGetValue(control.Source,out breaks))
            {var positions=new List<int>();for(int i=0;i<control.Source.Text.Length;i++)if(control.Source.Text[i]=='\n')positions.Add(i);breaks=positions.ToArray();lineCache.Add(control.Source,breaks);}
            int position=Array.BinarySearch(breaks,node.Start),line=(position>=0?position:~position)+1;
            string details=String.Join("\r\n",notes.OrderBy(n=>n,StringComparer.OrdinalIgnoreCase));
            if(details.Length==0)details="This dependency is cyclic, exceeds preview limits, or uses syntax/content not supported by the bounded preview. No runtime value was guessed.";
            return new SkinIssue{Code=code,ControlKey=control.Key,ControlName=control.Name,Property=property,Source=control.Source.RelativePath,Line=line,Information=true,
                Message=code=="PREVIEW_CONDITION"?"Visibility is unverified in the current sample state.":code=="PREVIEW_IMAGE_LAYOUT"?"Image fitting or alignment is unverified; an approximate fit is shown.":code=="PREVIEW_GEOMETRY"?"Image position uses an unverified property binding; the XML fallback is shown.":code=="PREVIEW_ARTWORK"?"Artwork is unavailable or unverified in the current sample state.":"Text is unverified in the current sample state.",
                Detail="Expression / text:\r\n"+Short(value)+"\r\n\r\nInputs and dependencies to inspect:\r\n"+details+"\r\n\r\nElement source: "+control.Source.RelativePath+":"+line+". Inherited expressions are listed above; their include locations may differ. Original XML is unchanged. An unverified preview is not proof of a MediaPortal error."};
        }
        public static SkinAuditReport Run(SkinProject project,PreviewState state)
        {
            if(project==null||state==null||state.Project!=project)throw new ArgumentException("Preview diagnostics require the matching project and preview state.");
            var report=new SkinAuditReport{Page=project.Page,Theme=project.Theme};var seen=new HashSet<string>();var lineCache=new Dictionary<SkinSource,int[]>();
            var ids=project.LogicalControls().Select(c=>new{Control=c,Id=SkinProject.ControlId(c)}).Where(p=>p.Id.HasValue).GroupBy(p=>p.Id.Value).ToDictionary(g=>g.Key,g=>g.Select(p=>p.Control).ToArray());
            foreach(var control in project.Controls)
            {
                bool? visible=state.Visible(control);var expressions=new List<KeyValuePair<string,Dictionary<string,string>>>();string property="visible",code="PREVIEW_CONDITION";
                if(visible==null)
                {
                    if(state.EvaluateScoped(control.Get("visible"),control.Defines)==null)expressions.Add(new KeyValuePair<string,Dictionary<string,string>>(control.Get("visible"),control.Defines));
                    if(expressions.Count==0)property="Include condition";
                    foreach(var term in control.VisibilityTerms??new VisibilityTerm[0])if(state.EvaluateScoped(term.Expression,term.Defines)==null)expressions.Add(new KeyValuePair<string,Dictionary<string,string>>(term.Expression,term.Defines));
                }
                else if(visible==true&&(new[]{"label","fadelabel","textbox","textboxscrollup"}.Contains(control.Get("type"))||control.Get("type").Contains("button"))&&(control.Get("label").Length>8192||!state.ResolvedLabel(control,control.Get("label"))))
                {property="label";code="PREVIEW_TEXT";expressions.Add(new KeyValuePair<string,Dictionary<string,string>>(control.Get("label"),control.Defines));}
                else if(visible==true&&control.Get("type")=="image")
                {
                    string texture=state.ImageTexture(control),resolved;
                    if(state.TryText(control,texture,out resolved)&&(resolved.Trim().Length==0||resolved.Trim()=="-"))continue;
                    if(state.ArtworkAvailable(control)==true)continue;
                    property="texture";code="PREVIEW_ARTWORK";expressions.Add(new KeyValuePair<string,Dictionary<string,string>>(texture,control.Defines));
                }
                else continue;
                var notes=new HashSet<string>();foreach(var expression in expressions)Explain(state,expression.Key,expression.Value,ids,notes,new HashSet<int>(),0);
                if(code=="PREVIEW_TEXT")
                {int id;string value,origin;if(project.Editor.Language.Active&&state.TryLabelId(control,control.Get("label"),out id)&&!project.Editor.Language.TryGet(id,out value,out origin))notes.Add("Language ID "+id+" is absent from the loaded primary and fallback packs.");}
                if(code=="PREVIEW_ARTWORK")
                {
                    string sample;bool explicitSample=project.Editor.Preview.Artwork.TryGetValue(control.Get("texture").Trim(),out sample);
                    notes.Add(state.ArtworkAvailable(control)==false?"No readable image exists at this skin Media path.":"The image binding, runtime/cache path or file cannot be verified by the local preview.");
                    notes.Add(explicitSample?"An explicit artwork sample is selected but unavailable. Choose another skin image, empty artwork, or restore the original expression in Sample Artwork.":"Use Sample Artwork to choose an explicit skin image or empty artwork for this expression. Original XML is preserved.");
                }
                var issue=Issue(control,property,code,String.Join("\r\nAND ",expressions.Select(e=>e.Key)),notes,lineCache);
                if(seen.Add(control.Key+"\n"+code+"\n"+issue.Detail))report.Issues.Add(issue);
                if(report.Issues.Count>=1024){report.Issues.Add(new SkinIssue{Code="PREVIEW_LIMIT",Information=true,Message="The preview report is limited to 1024 element findings.",Detail="Inspect a smaller page or resolve explicit samples before checking again."});break;}
            }
            if(report.Issues.Any(i=>i.Code=="PREVIEW_LIMIT"))return report;
            foreach(var control in project.Controls.Where(c=>c.Get("type")=="image"&&state.Visible(c)==true&&!ImagePresentation.Supported(c)))
            {
                var notes=new[]{"Only static image proportions, crop flags and alignment are supported. Unknown values use an approximate fit; original XML remains unchanged.","Use yes/no for keepaspectratio, zoom and zoomfromtop; left/center/right for align and top/middle/bottom for valign. Inherited centered behavior follows source order."};
                string raw=String.Join("; ",new[]{"keepaspectratio","zoom","zoomfromtop"}.Select(k=>k+"="+control.Get(k)).Concat(control.ImageAlignmentRules.Select(p=>p.Key+"="+p.Value)));
                report.Issues.Add(Issue(control,"Image presentation","PREVIEW_IMAGE_LAYOUT",raw,notes,lineCache));
                if(report.Issues.Count>=1024){report.Issues.Add(new SkinIssue{Code="PREVIEW_LIMIT",Information=true,Message="The preview report is limited to 1024 element findings.",Detail="Inspect a smaller page or resolve explicit samples before checking again."});return report;}
            }
            foreach(var control in project.Controls.Where(c=>c.Get("type")=="image"&&state.Visible(c)==true))foreach(string key in new[]{"posX","posY"})
            {
                PropertyValue property;float position;if(!control.Properties.TryGetValue(key,out property)||property.Expression.Length==0||state.TryImagePosition(control,key,out position))continue;
                var notes=new HashSet<string>();Explain(state,property.Expression,control.Defines,ids,notes,new HashSet<int>(),0);notes.Add("The property attribute cannot be resolved to a supported integer image position (0 to 100000). The fixed XML position is retained as a fallback; the binding remains protected.");notes.Add("Binding origin: "+(property.ExpressionOrigin.Length>0?property.ExpressionOrigin:property.Origin)+". Static filmstrip thumbnail coordinates are derived only for the supported facade; Sample Data can explicitly override them.");
                report.Issues.Add(Issue(control,key,"PREVIEW_GEOMETRY",property.Expression,notes,lineCache));
                if(report.Issues.Count>=1024){report.Issues.Add(new SkinIssue{Code="PREVIEW_LIMIT",Information=true,Message="The preview report is limited to 1024 element findings.",Detail="Inspect a smaller page or resolve explicit samples before checking again."});return report;}
            }
            foreach(var control in project.Controls.Where(c=>c.Get("type")=="checkmark"&&state.Visible(c)==true))
            {
                int width,height;if(CheckmarkPresentation.Layout(control,out width,out height)&&CheckmarkPresentation.Selected(control,state).HasValue&&state.Label(control,control.Get("label")).Length<=8192)continue;
                var notes=new[]{"Checkmark selection requires a known true/false result; unknown selection does not display a guessed mark. Configure Sample Data for runtime expressions.","markWidth/markHeight require static integers from 0 to 8192; align supports left/right. Activation, runtime toggles and click actions are not executed."};
                report.Issues.Add(Issue(control,"selected","PREVIEW_CHECKMARK",String.Join("; ",new[]{"selected","markWidth","markHeight","align"}.Select(k=>k+"="+control.Get(k))),notes,lineCache));
                if(report.Issues.Count>=1024){report.Issues.Add(new SkinIssue{Code="PREVIEW_LIMIT",Information=true,Message="The preview report is limited to 1024 element findings.",Detail="Inspect a smaller page before checking again."});return report;}
            }
            return report;
        }
    }
    public sealed partial class StudioForm
    {
        void ShowPreviewProblems()
        {
            using(var dialog=new SkinProblemsDialog(PreviewProblems.Run(project,state),true))
                if(dialog.ShowDialog(this)==System.Windows.Forms.DialogResult.OK)JumpToIssue(dialog.TargetKey,dialog.TargetProperty);
        }
    }
}
