using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MP1SkinStudio;

public static class StringConditionProbe
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS: "+name);}
    public static int Main(string[] args)
    {
        try
        {
            var p=new SkinProject(Path.GetFullPath(args[0]));p.OpenPage("myvideo.xml");
            p.Editor.Preview.Values["#scrolling.up"]="YES";p.Editor.Preview.Values["#scrolling.down"]="no";
            p.Editor.Preview.Values["#condition.sample"]="  A,B Movie  ";p.Editor.Collection.Mode="list";
            string xml=ProjectFile.XmlSignature(p);var s=new PreviewState(p);
            Check(s.Evaluate("string.contains(#condition.sample,'b MOV')",new HashSet<string>())==true,"Contains resolves known samples and compares without case sensitivity");
            Check(s.Evaluate("string.starts(#condition.sample,' a,b')",new HashSet<string>())==true,"Starts trims operands and handles quoted commas");
            Check(s.Evaluate("string.contains('Movie','')",new HashSet<string>())==false&&s.Evaluate("string.starts('','')",new HashSet<string>())==true,"Empty search operands follow native equality semantics");
            Check(s.Evaluate("string.equals(' A ','a')",new HashSet<string>())==true,"Equality trims operands as MP1 does");
            Check(s.Evaluate("!string.contains(#unknown,'yes')",new HashSet<string>())==null,"Negating a missing property remains unknown");
            Check(s.Evaluate("string.contains(#(unsupported()),yes)",new HashSet<string>())==null&&s.Evaluate("string.starts(a,b,c)",new HashSet<string>())==null,"Unsupported expressions and invalid argument counts remain unknown");
            var popup=p.Controls.Single(c=>c.Get("description")=="listscroller bg");
            Check(s.Visible(popup)==true,"Real Titan list scrolling condition becomes visible with explicit sample state");
            p.Editor.Preview.Values["#scrolling.up"]="no";s=new PreviewState(p);
            Check(s.Visible(popup)==false,"Real Titan scrolling popup hides when both known scroll flags are no");
            p.Editor.Preview.Values.Remove("#scrolling.up");s=new PreviewState(p);
            Check(s.Visible(popup)==null,"Real Titan scrolling popup stays unresolved when a required scroll flag is missing");
            Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Condition evaluation and sample changes preserve XML and history");
            Console.WriteLine(checks+" condition checks passed.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
    }
}
