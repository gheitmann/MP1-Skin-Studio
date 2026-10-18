using System;
using System.Linq;

namespace MP1SkinStudio
{
    public static class ElementSearch
    {
        static bool Contains(string value,string term){return (value??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0;}
        public static bool Matches(SkinControl control,string query,string displayText=null)
        {
            foreach(string token in (query??"").Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
            {
                int split=token.IndexOf(':');string field=split<0?"":token.Substring(0,split).ToLowerInvariant(),term=split<0?token:token.Substring(split+1);bool match;
                switch(field)
                {
                    case "id":int expected,actual;match=Int32.TryParse(term,out expected)&&Int32.TryParse(control.Get("id"),out actual)&&expected==actual;break;
                    case "typ":case "type":match=String.Equals(control.Get("type"),term,StringComparison.OrdinalIgnoreCase);break;
                    case "quelle":match=term.Length>0&&Contains(control.Source.RelativePath,term);break;
                    case "text":match=term.Length>0&&(Contains(control.Get("label"),term)||Contains(displayText,term));break;
                    case "name":match=term.Length>0&&(Contains(control.Name,term)||(LanguagePreview.UseLabelName(control)&&Contains(displayText,term)));break;
                    default:match=Contains(displayText,token)||Contains(control.Name,token)||Contains(control.Source.RelativePath,token)||control.Properties.Values.Any(p=>Contains(p.Value,token));break;
                }
                if(!match)return false;
            }
            return true;
        }
    }
}
