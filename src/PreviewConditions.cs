using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    public sealed class VisibilityTerm
    {public string Expression;public Dictionary<string,string> Defines;}
    public sealed partial class PreviewState
    {
        internal static VisibilityTerm[] AddTerm(VisibilityTerm[] terms,string expression,Dictionary<string,string> defines)
        {var list=new List<VisibilityTerm>(terms??new VisibilityTerm[0]);if(!String.IsNullOrWhiteSpace(expression))list.Add(new VisibilityTerm{Expression=expression,Defines=new Dictionary<string,string>(defines)});return list.ToArray();}
        bool? VisibleFor(SkinControl control,HashSet<string> active)
        {
            bool? result=EvaluateInternal(control.Get("visible"),active,control.Defines);
            if(control.Resolved("Visibility")=="Hidden"||control.Resolved("Visibility")=="Collapsed")result=false;
            if(!CollectionSettings.Active(Project,control))result=false;
            if(control.VisibilityTerms==null)return And(result,EvaluateInternal(control.IncludeCondition,active,control.Defines));
            foreach(var term in control.VisibilityTerms??new VisibilityTerm[0])result=And(result,EvaluateInternal(term.Expression,active,term.Defines));return result;
        }
        bool Operand(string expression,Dictionary<string,string> defines,out string value)
        {
            string current=expression.Trim();var seen=new HashSet<string>();
            if(current.Length>=2&&((current[0]=='\''&&current[current.Length-1]=='\'')||(current[0]=='"'&&current[current.Length-1]=='"'))){value=current.Substring(1,current.Length-2);return true;}
            while(current.StartsWith("#",StringComparison.Ordinal))
            {
                if(!Regex.IsMatch(current,@"^#[A-Za-z_][A-Za-z0-9_.]*$")){value=null;return false;}
                if(Project.Editor.Preview.Values.TryGetValue(current,out value))return true;
                string next;if(defines!=null&&defines.TryGetValue(current,out next)){if(!seen.Add(current)){value=null;return false;}current=next;continue;}
                if(Values.TryGetValue(current,out value))return true;value=null;return false;
            }
            value=current;return true;
        }
        public bool? Evaluate(string expression,HashSet<string> active){return EvaluateInternal(expression,active,null);}
        bool? EvaluateInternal(string expr,HashSet<string> active,Dictionary<string,string> defines)
        {
            if(String.IsNullOrWhiteSpace(expr))return true;expr=expr.Trim();
            if(expr.Length>2&&((expr[0]=='['&&expr[expr.Length-1]==']')||(expr[0]=='('&&expr[expr.Length-1]==')'))&&Encloses(expr))return EvaluateInternal(expr.Substring(1,expr.Length-2),active,defines);
            var parts=Split(expr,'|');if(parts.Count>1){bool unknown=false;foreach(var part in parts){var value=EvaluateInternal(part,active,defines);if(value==true)return true;if(value==null)unknown=true;}return unknown?(bool?)null:false;}
            parts=Split(expr,'+');if(parts.Count>1){bool? result=true;foreach(var part in parts)result=And(result,EvaluateInternal(part,active,defines));return result;}
            if(expr.StartsWith("!")){var value=EvaluateInternal(expr.Substring(1),active,defines);return value==null?null:!value;}
            if(expr.Equals("yes",StringComparison.OrdinalIgnoreCase)||expr.Equals("true",StringComparison.OrdinalIgnoreCase))return true;
            if(expr.Equals("no",StringComparison.OrdinalIgnoreCase)||expr.Equals("false",StringComparison.OrdinalIgnoreCase))return false;
            if(Regex.IsMatch(expr,@"^#[A-Za-z_][A-Za-z0-9_.]*$"))
            {string value,key="define:"+expr;if(!Operand(expr,defines,out value)||value==expr||!active.Add(key))return null;try{return EvaluateInternal(value,active,defines);}finally{active.Remove(key);}}
            if(expr.StartsWith("facadeview.",StringComparison.OrdinalIgnoreCase)&&CollectionSettings.Modes.Contains(expr.Substring(11).ToLowerInvariant()))return CollectionSettings.EffectiveMode(Project)==expr.Substring(11).ToLowerInvariant();
            var match=Regex.Match(expr,@"^(window\.isvisible|control\.hasfocus|control\.isvisible|plugin\.isenabled|string\.equals)\((.*)\)$",RegexOptions.IgnoreCase);if(!match.Success)return null;
            string fun=match.Groups[1].Value.ToLowerInvariant(),arg=match.Groups[2].Value.Trim();int id;
            if(fun=="plugin.isenabled")return Plugins.Contains(arg.Trim('\'', '"'));
            if(fun=="string.equals")
            {var args=Split(arg,',');string left,right;if(args.Count!=2||!Operand(args[0],defines,out left)||!Operand(args[1],defines,out right))return null;return String.Equals(left,right,StringComparison.OrdinalIgnoreCase);}
            if(!Int32.TryParse(arg,out id))return null;if(fun=="window.isvisible")return Project.WindowId==id;if(fun=="control.hasfocus")return FocusId==id;
            string activeKey="visible:"+id;if(!active.Add(activeKey))return null;
            try{var controls=Project.LogicalControls().Where(c=>SkinProject.ControlId(c)==id).ToArray();if(controls.Length!=1)return null;return VisibleFor(controls[0],active);}finally{active.Remove(activeKey);}
        }
    }
}
