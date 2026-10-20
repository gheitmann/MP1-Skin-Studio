using System;
using System.Drawing;
using System.Windows.Forms;
using MP1SkinStudio;
class PackageAcceptanceHost
{
    [STAThread]static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            using(var form=new StudioForm(args[0]))
            {form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.RunUiSmoke(args[1]);}
            return 0;
        }
        catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(args[1],"Host-Error.txt"),ex.ToString());return 1;}
    }
}
