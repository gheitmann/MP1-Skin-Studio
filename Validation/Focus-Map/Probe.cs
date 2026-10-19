using System;
using System.IO;
using System.Linq;
using MP1SkinStudio;
class FocusMapProbe
{
    static void Expect(SkinProject p,string page,string theme,int id)
    {p.PrepareTheme(theme);var prepared=PreparedSkinSession.Create(p,page);if(prepared.Preview.FocusId!=id)throw new Exception(theme+"/"+page+" lost focus "+id);prepared.Preview.SetFocus(id);}
    static int Main(string[] args)
    {
        try
        {
            Directory.CreateDirectory(args[1]);var p=new SkinProject(args[0]);p.OpenPage("BasicHome.xml");BasicHomePreview.Apply(p);new PreviewState(p).SetFocus(7);
            p.OpenPage("settings.xml");new PreviewState(p).SetFocus(4);
            p.PrepareTheme("Christmas");new PreviewState(p).SetFocus(5);
            p.OpenPage("BasicHome.xml");BasicHomePreview.Apply(p);new PreviewState(p).SetFocus(2);
            string signature=ProjectFile.ContentSignature(p);Expect(p,"BasicHome.xml","",7);Expect(p,"settings.xml","",4);Expect(p,"settings.xml","Christmas",5);Expect(p,"BasicHome.xml","Christmas",2);
            if(ProjectFile.ContentSignature(p)!=signature)throw new Exception("Restoring existing slots changed editor signature");
            string file=Path.Combine(args[1],"Four-Focus-States.mp1skinproj");ProjectFile.Save(p,file);p=ProjectFile.Load(file);Expect(p,"BasicHome.xml","",7);Expect(p,"settings.xml","",4);Expect(p,"settings.xml","Christmas",5);Expect(p,"BasicHome.xml","Christmas",2);
            if(ProjectFile.ContentSignature(p)!=signature||p.Dirty||p.Undo.Count!=0)throw new Exception("Focus-map persistence changed XML or state");
            foreach(var source in p.Sources.Values)if(ProjectFile.Digest(File.ReadAllBytes(source.Path))!=ProjectFile.Digest(source.Preamble.Concat(source.Encoding.GetBytes(source.Original)).ToArray()))throw new Exception("Original source changed");
            var legacy=ProjectFile.Load(args[2]);if(new PreviewState(legacy).FocusId!=5)throw new Exception("Real legacy BasicHome focus not restored");ProjectFile.Save(legacy,Path.Combine(args[1],"Migrated-BasicHome.mp1skinproj"));
            string report="PASS four independent real Titan focus states: Default BasicHome=7, Settings=4; Christmas BasicHome=2, Settings=5. Repeated page/theme restoration leaves signature unchanged; save/reopen retains all slots; XML clean, no undo entries; all "+p.Sources.Count+" loaded original source hashes unchanged. Real legacy BasicHome project migrated with focus 5.";
            File.WriteAllText(Path.Combine(args[1],"Results.txt"),report);Console.WriteLine(report);return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
