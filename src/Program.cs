using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if(args.Length>0&&args[0]=="--selftest")return SelfTests.Run(args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"MP1SkinStudio-"+Guid.NewGuid().ToString("N")));
                if(args.Length>0&&args[0]=="--generate-demo-assets"){DemoAssets.Generate(StudioForm.DemoRoot());return 0;}
                if(args.Length>2&&args[0]=="--check-skin")return SkinBatchCheck.Run(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]));
                if(args.Length>2&&args[0]=="--prepare-runtime")return RuntimeTestPackage.Run(args[1],args[2]);
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length>3&&args[0]=="--render-skin"){using(var external=new StudioForm(args[1])){external.PrepareExternalPreview(args[1],args[2],args.Length>4?args[4]:"");if(args.Length>5)external.LoadPreviewLanguages(args[5],args.Length>6?args[6]:null);if(args.Length>7&&args[7]=="hide-unresolved")external.SetUnresolvedPreview(false);external.RenderTo(args[3]);}return 0;}
                if(args.Length>1&&args[0]=="--render-textures")
                {var p=new SkinProject(StudioForm.DemoRoot());p.OpenPage("demo.xml");using(var browser=new TextureBrowser(p,null))browser.RenderTo(args[1]);return 0;}
                if(args.Length>4&&args[0]=="--preview-skin"){using(var external=new StudioForm(args[1])){external.PrepareExternalPreview(args[1],args[2],"");external.LoadPreviewLanguages(args[3],args[4]);external.SetUnresolvedPreview(false);Application.Run(external);}return 0;}
                bool projectArgument=args.Length>0&&args[0].EndsWith(".mp1skinproj",StringComparison.OrdinalIgnoreCase);string root=args.Length>0&&!args[0].StartsWith("--")&&!projectArgument?args[0]:StudioForm.DemoRoot();
                using(var form=new StudioForm(root))
                {
                    if(projectArgument)form.ResumeProjectFile(args[0]);
                    if(args.Length>1&&args[0]=="--render-collections"){form.PrepareCollectionPreview(args.Length>2?args[2]:"list");form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-collection-dialog"){form.RenderCollectionDialogTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render"){form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-navigation"){form.PrepareNavigationPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-multi"){form.PrepareMultiPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-viewport"){form.PrepareViewportPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-audit"){form.RenderAuditTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-testdata"){form.RenderPreviewSettingsTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-language"){form.PrepareLanguagePreview();form.RenderLanguageTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-localized-demo"){form.PrepareLanguagePreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-texteditor"){form.RenderTextEditorTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-groups"){form.PrepareGroupPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-search"){form.PrepareSearchPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-layers"){form.PrepareLayersPreview();form.RenderTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--render-deletion"){form.RenderDeletionTo(args[1]);return 0;}
                    if(args.Length>1&&args[0]=="--ui-smoketest"){form.RunUiSmoke(args[1]);return 0;}
                    Application.Run(form);
                }
                return 0;
            }
            catch(Exception ex)
            {
                string log=StudioErrors.Record(ex);
                if(args.Length==0||!args[0].StartsWith("--",StringComparison.Ordinal)||args[0]=="--preview-skin")MessageBox.Show(ex.Message+(log==null?"":"\n\nError details: "+log),"MP1 Skin Studio",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
