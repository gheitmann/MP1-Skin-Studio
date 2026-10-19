using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MP1SkinStudio;
class Probe
{
    static int passed;
    static void Equal(string actual,string expected,string name){if(actual!=expected)throw new Exception(name+": expected "+expected+", got "+actual);Console.WriteLine("PASS "+name);passed++;}
    static int Main(string[] args)
    {
        string root=args[0];Directory.CreateDirectory(root);string file=Path.Combine(root,"main.xml");
        File.WriteAllText(file,"<window><controls><control><type>label</type><id>0</id><label>#(string.trim(#title))</label></control></controls></window>");
        var p=new SkinProject(root);var source=SkinSource.FromText(File.ReadAllText(file),"main.xml");var c=new SkinControl{Source=source,Node=source.Elements.First(n=>n.Name=="control"),Defines=new Dictionary<string,string>()};p.Controls.Add(c);string original=c.Source.Text;
        p.Editor.Preview.Values["#title"]="  A, film (2026)  ";p.Editor.Preview.Values["#literal"]="Title #date";
        c.Defines["#local"]="#alias";c.Defines["#alias"]="Local";var state=new PreviewState(p);
        Equal(state.Text(c,"#(string.trim(#title))"),"A, film (2026)","Trim a known sample binding");
        Equal(state.Text(c,"#(string.replace(string.trim(#title), 'film', 'movie'))"),"A, movie (2026)","Nested functions and comma-bearing values");
        Equal(state.Text(c,"#(iif(eq('A','a'),'match','other'))"),"match","Typed comparison and conditional");
        Equal(state.Text(c,"#(iif(eq(1,'1'),'wrong','typed'))"),"typed","Numeric and string literals stay distinct");
        Equal(state.Text(c,"#(string.length('a,b(c)'))"),"6","Quoted commas and parentheses");
        Equal(state.Text(c,"#(string.trim('..title..','.'))"),"title","Custom trim characters");
        Equal(state.Text(c,"#(string.ltrim(' x ')) / #(string.rtrim(' x '))"),"x  /  x","Multiple independent expressions");
        Equal(state.Text(c,"#(string.starts('ABC','a'))"),"true","Expression string comparison ignores case");
        Equal(state.Text(c,"#(string.equals(' a ','a'))"),"false","Expression comparison preserves spaces");
        Equal(state.Text(c,"#(string.trim(#missing))"),"#(string.trim(#missing))","Missing runtime value stays unresolved");
        Equal(state.Text(c,"#(unknown(#title))"),"#(unknown(#title))","Unsupported expression is preserved atomically");
        Equal(state.Text(c,"#(string.trim(#title)"),"#(string.trim(#title)","Malformed expression stays intact");
        Equal(state.Text(c,"#(string.trim('a',))"),"#(string.trim('a',))","Invalid argument is rejected");
        Equal(state.Text(c,"#literal / #title.extra"),"Title #date / #title.extra","Inserted values are not recursively evaluated");
        Equal(state.Text(c,"prefix #local suffix"),"prefix Local suffix","Embedded scoped define chain resolves");
        Equal(state.Text(c,"#(string.trim(#local))"),"Local","Expression uses scoped define chain");
        c.Defines["#cycle"]="#cycle";Equal(state.Text(c,"#(string.trim(#cycle))"),"#(string.trim(#cycle))","Define cycles remain unresolved");
        string deep="'x'";for(int i=0;i<40;i++)deep="string.trim("+deep+")";deep="#("+deep+")";Equal(state.Text(c,deep),deep,"Nesting limit preserves expression");
        Equal(state.Text(c,"#title / #title.extra / #date"),"  A, film (2026)   / #title.extra / Saturday, October 17","Existing token boundaries stay intact");
        c.Defines["#header.label"]="Local heading";Equal(state.Text(c,"#header.label"),"Local heading","Scoped define overrides built-in sample");
        p.Editor.Preview.Values["#header.label"]="Override";Equal(state.Text(c,"#header.label"),"Override","Explicit sample overrides scoped define");
        Equal(state.Text(c,"#(string.replace('abc','','x'))"),"abc","Empty replacement search preserves text");
        string large="#(string.replace('"+new string('x',100)+"','x','"+new string('y',100)+"'))";Equal(state.Text(c,large),large,"Expansion limit preserves expression");
        Equal(c.Source.Text,original,"Preview evaluation preserves XML");
        if(p.Dirty||p.Undo.Count!=0)throw new Exception("Preview introduced edits");Console.WriteLine("PASS no XML edits or undo entries");passed++;
        Console.WriteLine(passed+" checks passed");return 0;
    }
}
