using System;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    // Complete all filesystem/model work before replacing the form's active session.
    public sealed class PreparedSkinSession
    {
        public SkinProject Project;
        public PreviewState Preview;
        public string[] Pages,Themes;
        public static PreparedSkinSession Create(SkinProject project,string requested)
        {
            var pages=project.Pages();if(pages.Length==0)throw new InvalidDataException("No skin XML files found.");
            string chosen=pages.FirstOrDefault(p=>String.Equals(p,requested,StringComparison.OrdinalIgnoreCase))
                ??pages.FirstOrDefault(p=>String.Equals(p,"myHome.xml",StringComparison.OrdinalIgnoreCase))
                ??pages.FirstOrDefault(p=>String.Equals(p,"BasicHome.xml",StringComparison.OrdinalIgnoreCase))??pages[0];
            string folder=Path.Combine(project.Root,"Themes");
            var themes=Directory.Exists(folder)?Directory.GetDirectories(folder).Select(Path.GetFileName).ToArray():new string[0];
            var preview=new PreviewState(project);
            // OpenPage restores existing page metadata if this last fallible load fails.
            project.OpenPage(chosen);preview.FocusId=project.DefaultControlId;
            return new PreparedSkinSession{Project=project,Preview=preview,Pages=pages,Themes=themes};
        }
    }
    public sealed partial class SkinProject
    {
        public PreparedSkinSession PrepareTheme(string theme)
        {
            string previous=Theme;Theme=theme??"";
            try{return PreparedSkinSession.Create(this,Page);}
            catch{Theme=previous;throw;}
        }
    }
}
