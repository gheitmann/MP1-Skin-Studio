using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;

class PropertyToolsProbe
{
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);if(!ok)throw new InvalidOperationException(name);}
    static void Render(Form form,string path)
    {
        form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
        using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(path);}form.Hide();
    }
    [STAThread]static int Main(string[] args)
    {
        Directory.CreateDirectory(args[1]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            using(var form=new StudioForm(args[0]))
            {
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();form.Hide();form.Show();Application.DoEvents();
                var project=(SkinProject)typeof(StudioForm).GetField("project",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
                var selected=project.Controls.First(c=>c.Get("description")=="Movies");
                typeof(StudioForm).GetMethod("SelectControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SkinControl)},null).Invoke(form,new object[]{selected});
                typeof(StudioForm).GetMethod("RunPropertyToolsUiSmoke",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{new Action<bool,string>(Check)});
                Render(form,Path.Combine(args[1],"properties.png"));
                using(var dialog=new FontPickerDialog(project,selected.Get("font"),"Movies, music and photos · Aa 0123456789"))Render(dialog,Path.Combine(args[1],"font-picker.png"));
                project.FontNames.Clear();using(var empty=new FontPickerDialog(project,"Unknown","Sample"))Check(empty.SelectedFontName==null&&empty.PreviewDetails.Contains("No matching fonts"),"A skin without font choices opens safely with no selection");
                project.FontNames.Add("Invalid height");project.FontSizes["Invalid height"]=Single.NaN;
                using(var invalid=new FontPickerDialog(project,"Invalid height","Sample"))Check(invalid.PreviewDetails.Contains("fallback settings")&&invalid.PreviewDetails.Contains("px"),"Invalid font heights use bounded fallback settings without failing");
            }
            if(args.Length>2)
            {
                var installed=new SkinProject(args[2]);installed.OpenPage("myHome.xml");string signature=ProjectFile.XmlSignature(installed);
                using(var dialog=new FontPickerDialog(installed,installed.FontNames.FirstOrDefault(),"Movies · Music · Photos"))
                {Render(dialog,Path.Combine(args[1],"installed-fonts.png"));Check(dialog.SelectedFontName!=null&&dialog.PreviewDetails.Contains("px"),"Installed DefaultWideHD fonts load and report their preview size");}
                Check(ProjectFile.XmlSignature(installed)==signature&&!installed.Dirty,"Browsing installed fonts leaves loaded XML unchanged");
            }
            return 0;
        }
        catch(Exception ex){results.Add(ex.ToString());return 1;}
        finally{File.WriteAllLines(Path.Combine(args[1],"Results.txt"),results);}
    }
}
