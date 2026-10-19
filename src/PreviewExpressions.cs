using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MP1SkinStudio
{
    // A bounded preview-only subset of MP1 GUIExpressionManager/GUIFunctions.
    // Unknown inputs are retained instead of assuming an empty runtime property.
    public sealed partial class PreviewState
    {
        bool ExpressionValue(string expression,Dictionary<string,string> defines,int depth,out object value)
        {
            value=null;if(depth>32||expression.Length>8192)return false;
            string s=expression.Trim();
            if(s.Length>=2&&s[0]=='\''&&s[s.Length-1]=='\'')
            {string body=s.Substring(1,s.Length-2);if(body.IndexOf('\'')>=0)return false;value=body;return true;}
            if(Regex.IsMatch(s,@"^#[A-Za-z_][A-Za-z0-9_.]*$"))
            {string result;if(!Operand(s,defines,out result))return false;value=result;return true;}
            int integer;if(Int32.TryParse(s,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out integer)){value=integer;return true;}
            var call=Regex.Match(s,@"^([a-zA-Z][a-zA-Z0-9_.]*)\((.*)\)$",RegexOptions.Singleline);if(!call.Success)return false;
            string name=call.Groups[1].Value;var parts=ExpressionArguments(call.Groups[2].Value);if(parts==null||parts.Count>16)return false;
            var args=new List<object>();foreach(string part in parts){object arg;if(!ExpressionValue(part,defines,depth+1,out arg))return false;args.Add(arg);}
            Func<int,string> str=i=>args[i] as string;
            if(name=="iif"&&args.Count==3){bool condition;if(args[0] is bool)condition=(bool)args[0];else if(!Boolean.TryParse(str(0),out condition))return false;value=condition?args[1]:args[2];return true;}
            if((name=="eq"||name=="neq")&&args.Count==2)
            {bool equal=args[0] is string&&args[1] is string?CultureInfo.InvariantCulture.CompareInfo.Compare(str(0),str(1),CompareOptions.IgnoreCase)==0:Object.Equals(args[0],args[1]);value=name=="eq"?equal:!equal;return true;}
            if((name=="string.equals"||name=="string.contains"||name=="string.starts")&&args.Count==2&&str(0)!=null&&str(1)!=null)
            {var compare=CultureInfo.InvariantCulture.CompareInfo;int index=compare.IndexOf(str(0),str(1),CompareOptions.IgnoreCase);value=(name=="string.equals"?compare.Compare(str(0),str(1),CompareOptions.IgnoreCase)==0:name=="string.starts"?index==0:index>=0)?"true":"false";return true;}
            if((name=="string.trim"||name=="string.ltrim"||name=="string.rtrim")&&(args.Count==1||args.Count==2)&&str(0)!=null&&(args.Count==1||str(1)!=null))
            {char[] chars=args.Count==2?str(1).ToCharArray():null;value=name=="string.trim"?str(0).Trim(chars):name=="string.ltrim"?str(0).TrimStart(chars):str(0).TrimEnd(chars);return true;}
            if(name=="string.replace"&&args.Count==3&&str(0)!=null&&str(1)!=null&&str(2)!=null)
            {if(str(1).Length==0){value=str(0);return true;}long count=(str(0).Length-str(0).Replace(str(1),"").Length)/str(1).Length;if(str(0).Length+count*(str(2).Length-str(1).Length)>8192)return false;value=str(0).Replace(str(1),str(2));return true;}
            if(name=="string.length"&&args.Count==1&&str(0)!=null){value=str(0).Length;return true;}
            return false;
        }
        static List<string> ExpressionArguments(string text)
        {
            var result=new List<string>();int start=0,depth=0;bool quote=false;
            for(int i=0;i<text.Length;i++)
            {char c=text[i];if(c=='\''){quote=!quote;continue;}if(quote)continue;if(c=='(')depth++;if(c==')'&&--depth<0)return null;if(c==','&&depth==0){result.Add(text.Substring(start,i-start));start=i+1;}}
            if(quote||depth!=0)return null;if(text.Trim().Length>0)result.Add(text.Substring(start));return result;
        }
        string ExpressionText(SkinControl control,string text)
        {
            var result=new StringBuilder();
            for(int i=0;i<text.Length;)
            {
                if(i+1<text.Length&&text[i]=='#'&&text[i+1]=='(')
                {
                    int start=i,depth=1,j=i+2;bool quote=false;
                    for(;j<text.Length;j++){char c=text[j];if(c=='\'')quote=!quote;if(quote)continue;if(c=='(')depth++;if(c==')'&&--depth==0)break;}
                    if(j==text.Length){result.Append(text.Substring(start));break;}
                    object value;string raw=text.Substring(start,j-start+1);
                    result.Append(ExpressionValue(text.Substring(start+2,j-start-2),control.Defines,0,out value)?Convert.ToString(value,CultureInfo.CurrentCulture):raw);i=j+1;continue;
                }
                if(text[i]=='#')
                {
                    var token=Regex.Match(text.Substring(i),@"^#[A-Za-z_][A-Za-z0-9_.]*");
                    if(token.Success){string value;result.Append(Operand(token.Value,control.Defines,out value)?value:token.Value);i+=token.Length;continue;}
                }
                result.Append(text[i++]);
            }
            return result.ToString();
        }
    }
}
