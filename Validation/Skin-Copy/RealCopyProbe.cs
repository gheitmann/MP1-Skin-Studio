using System;using System.IO;using System.Linq;using System.Threading;using MP1SkinStudio;
class RealCopyProbe
{
    static int Main(string[] args)
    {
        try
        {
            string skin=Path.GetFullPath(args[0]),parent=Path.GetFullPath(args[1]);Directory.CreateDirectory(parent);
            var hashes=Directory.GetFiles(skin,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>ProjectFile.Digest(File.ReadAllBytes(p)));
            var project=new SkinProject(skin);project.OpenPage("myHome.xml");string signature=ProjectFile.ContentSignature(project);
            string copy=SkinCopyPlan.Capture(project,false).CopyTo(parent,"Independent base",CancellationToken.None);
            if(Directory.GetFiles(copy,"*",SearchOption.AllDirectories).Length!=hashes.Count)throw new Exception("File count changed.");
            foreach(var file in hashes)
                if(ProjectFile.Digest(File.ReadAllBytes(file.Key))!=file.Value||ProjectFile.Digest(File.ReadAllBytes(Path.Combine(copy,file.Key.Substring(skin.Length+1))))!=file.Value)throw new Exception("Hash mismatch: "+file.Key);
            var loaded=new SkinProject(copy);loaded.OpenPage(project.Page);
            if(loaded.Controls.Count!=project.Controls.Count||ProjectFile.ContentSignature(project)!=signature||loaded.Dirty)throw new Exception("Session or copied page changed.");
            string text="PASS "+hashes.Count+" complete installed-skin files copied byte-for-byte.\r\nPASS all "+hashes.Count+" original file hashes unchanged.\r\nPASS copied myHome opens with "+loaded.Controls.Count+" controls and a clean session; original session intact.\r\n";
            File.WriteAllText(Path.Combine(parent,"Results.txt"),text);Console.WriteLine(text);return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
