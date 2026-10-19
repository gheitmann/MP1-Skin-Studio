using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MP1SkinStudio;
class NavigationProbe
{
    static void Move(SkinProject p,PreviewState state,string direction,int expected)
    {
        var target=p.PreviewNavigationTarget(state,direction);if(SkinProject.ControlId(target)!=expected)throw new Exception(direction+" did not reach "+expected);state.SetFocus(expected);
    }
    static int Main(string[] args){try{return Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static int Run(string[] args)
    {
        Directory.CreateDirectory(args[1]);var lines=new List<string>();
        foreach(string theme in new[]{"","Christmas"})
        {
            var p=new SkinProject(args[0]){Theme=theme};p.OpenPage("BasicHome.xml");BasicHomePreview.Apply(p);var state=new PreviewState(p);
            var hashes=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));string xml=ProjectFile.XmlSignature(p);
            Move(p,state,"onright",6);Move(p,state,"ondown",7);Move(p,state,"onleft",5);Move(p,state,"onleft",3);Move(p,state,"onup",2);Move(p,state,"onright",4);Move(p,state,"onup",4);Move(p,state,"onright",6);
            string name=theme.Length==0?"Default":theme;string file=Path.Combine(args[1],name+"-Navigation.mp1skinproj");ProjectFile.Save(p,file);var restored=ProjectFile.Load(file);var prepared=PreparedSkinSession.Create(restored,restored.Page);
            if(prepared.Preview.FocusId!=6||ProjectFile.ContentSignature(restored)!=ProjectFile.ContentSignature(p))throw new Exception("Route focus did not survive reopening");
            if(p.Dirty||p.Undo.Count!=0||ProjectFile.XmlSignature(p)!=xml||hashes.Any(h=>ProjectFile.Digest(File.ReadAllBytes(h.Key))!=h.Value))throw new Exception("Navigation changed original XML/history");
            lines.Add("PASS "+name+" BasicHome: 4→6→7→5→3→2→4→4→6; focus 6 survives saved session; all "+hashes.Count+" original hashes unchanged");
        }
        var settings=new SkinProject(args[0]);settings.OpenPage("settings.xml");var preview=new PreviewState(settings);if(preview.FocusId!=3)throw new Exception("Unexpected Settings default focus");
        Move(settings,preview,"onup",20);Move(settings,preview,"ondown",3);bool rejected=false;try{settings.PreviewNavigationTarget(preview,"ondown");}catch(InvalidOperationException){rejected=true;}if(!rejected||preview.FocusId!=3)throw new Exception("Missing explicit group direction changed focus");
        if(settings.Dirty||settings.Undo.Count!=0)throw new Exception("Settings navigation edited XML");lines.Add("PASS Settings explicit edges: 3→20→3; absent ondown is rejected with focus preserved. Automatic group navigation remains unsupported. No XML edits or undo entries");
        File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);return 0;
    }
}
