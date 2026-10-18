using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    // Produces a reversible export probe; never installs to ProgramData or edits the source skin.
    public static class RuntimeTestPackage
    {
        public static int Run(string root,string output)
        {
            root=Path.GetFullPath(root);output=Path.GetFullPath(output);
            if(output.Equals(root,StringComparison.OrdinalIgnoreCase)||output.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose test output outside the original skin directory.");
            if(Directory.Exists(output)&&Directory.GetFileSystemEntries(output).Length>0)throw new InvalidOperationException("Use an empty output directory for the runtime probe.");
            Directory.CreateDirectory(output);var project=new SkinProject(root);project.OpenPage("settings.xml");
            var group=project.Controls.FirstOrDefault(c=>c.Get("type")=="group"&&c.Get("description")=="buttons");StackPreview.Rule rule;
            if(group==null||!StackPreview.TryRead(group.Resolved("layout"),out rule))throw new InvalidDataException("The expected Titan settings group was not found.");
            var before=project.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)),StringComparer.OrdinalIgnoreCase);
            string old=group.Get("layout");project.SetStackLayout(group,"StackLayout("+(rule.Gap+8)+", "+(rule.Horizontal?"Horizontal":"Vertical")+", "+(rule.Collapse?"true":"false")+")",false);
            string zip=Path.Combine(output,"Titan-Settings-Test-Export.zip");project.Export(zip);ProjectFile.Save(project,Path.Combine(output,"Titan-Settings.mp1skinproj"));
            using(var archive=ZipFile.OpenRead(zip))
            {
                if(archive.Entries.Count!=2||archive.GetEntry("settings.xml")==null)throw new InvalidDataException("Export contains unexpected files.");
                using(var reader=new StreamReader(archive.GetEntry("settings.xml").Open()))if(reader.ReadToEnd()!=project.Controls.First(c=>c.Key==group.Key).Source.Text)throw new InvalidDataException("Export and edited XML do not match.");
            }
            project.UndoChange();if(project.Dirty||project.Controls.First(c=>c.Key==group.Key).Get("layout")!=old)throw new InvalidDataException("The export probe cannot be undone exactly.");
            if(before.Any(p=>ProjectFile.Digest(File.ReadAllBytes(p.Key))!=p.Value))throw new InvalidDataException("The original skin was modified.");
            var lines=new[]{"MP1 Skin Studio - prepared practical runtime test","","Automatically validated:","PASS  Loaded Titan settings.xml and increased group spacing by 8 pixels.","PASS  Export contains only settings.xml and export instructions.","PASS  Exported XML exactly matches the editor state.","PASS  Saved the editor project and fully undid the change.","PASS  No loaded original file was modified.","","NOT YET PERFORMED: full client/Direct3D image comparison.","","Manual steps after fully configuring MediaPortal 1:","1. Copy the Titan skin directory and name the copy SkinStudio-Test.","2. Replace only settings.xml in the copy using Titan-Settings-Test-Export.zip.","3. Select the copied skin in MP1 or start with /windowed /skin=SkinStudio-Test.","4. Open Settings: buttons must have 8 additional skin pixels of spacing.","5. Compare text, focus and navigation with the editor image.","6. Switch back to the original Titan skin; original files remain intact.","","Important: no TV server is required for this probe.","The local file copy is not a fully registered system installation.","The official setup and Configuration.exe require administrator privileges.","Treat the isolated copy as functional only after initial configuration is complete."};
            File.WriteAllLines(Path.Combine(output,"Runtime-Validation.txt"),lines,new UTF8Encoding(false));return 0;
        }
    }
}
