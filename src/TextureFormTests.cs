using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RunTextureUiSmoke(string directory,Action<bool,string> verify)
        {
            using(var browser=new TextureBrowser(project,"Studio/Button-dark.png"))
            {
                browser.Show();Application.DoEvents();verify(browser.MatchCount>=7&&browser.SelectionValid,"The texture browser loads demo images with previews");
                browser.Filter("Icon-");verify(browser.MatchCount==3,"The search field filters textures by file path");
                browser.Filter("no-image-with-this-name");verify(browser.MatchCount==0&&!browser.SelectionValid,"Empty search results disable image selection");
                browser.Filter("turquoise");browser.SelectRelative("Studio/Button-turquoise.png");browser.AcceptForTest();
                verify(browser.DialogResult==DialogResult.OK&&browser.SelectedPath=="Studio/Button-turquoise.png","Image selection returns a relative Media path");
                SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));fields["textureNoFocus"].Text=browser.SelectedPath;Apply();
                verify(selected.Get("textureNoFocus")=="Studio/Button-turquoise.png","Selected textures are saved on the intended control");undo.PerformClick();
                verify(!project.Dirty,"Image selection can be fully undone");
            }
            using(var browser=new TextureBrowser(project,null))
            {browser.Show();Application.DoEvents();browser.DialogResult=DialogResult.Cancel;browser.Close();verify(!project.Dirty,"Cancelling the texture browser preserves XML files");}
        }
    }
}
