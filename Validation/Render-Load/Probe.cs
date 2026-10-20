using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;

class RenderLoadProbe
{
    static T Field<T>(object owner,string name){return (T)owner.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(owner);}
    static void Measure(string name,int count,Action action)
    {
        var times=new double[count];for(int i=0;i<count;i++){var watch=Stopwatch.StartNew();action();times[i]=watch.Elapsed.TotalMilliseconds;}
        Array.Sort(times);Console.WriteLine(name+" median_ms="+times[count/2].ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" max_ms="+times.Last().ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
    }
    static string Mapping(SkinSource source)
    {
        var text=new StringBuilder(ProjectFile.Digest(source.Bytes()));
        foreach(var e in source.Elements)text.Append('\n').Append(e.Name).Append(':').Append(e.Start).Append(':').Append(e.OpenEnd).Append(':').Append(e.CloseStart).Append(':').Append(e.End).Append(':').Append(e.ControlOrdinal).Append(':').Append(e.Element.OuterXml);
        return ProjectFile.Digest(Encoding.UTF8.GetBytes(text.ToString()));
    }
    [STAThread]static void Main(string[] args)
    {
        Application.EnableVisualStyles();Directory.CreateDirectory(args[1]);
        foreach(string page in new[]{"myHome.xml","BasicHome.xml","MyMinesweeper.xml"})
        {
            Console.WriteLine("PAGE "+page);SkinProject project=null;
            Measure("fresh model",3,()=>{project=new SkinProject(args[0]);project.OpenPage(page);});
            Console.WriteLine("sources="+project.Sources.Count+" controls="+project.Controls.Count);
            Measure("cached-source model",7,()=>project.OpenPage(page));
            using(var form=new StudioForm(args[0]))
            {
                Measure("fresh form load",3,()=>form.LoadProject(args[0],page));
                var canvas=Field<SkinCanvas>(form,"canvas");var state=Field<PreviewState>(form,"state");project=canvas.Project;
                canvas.Size=new Size(1000,700);
                using(var image=new Bitmap(canvas.Width,canvas.Height))
                {
                    canvas.DrawToBitmap(image,canvas.ClientRectangle);
                    Measure("warm canvas paint",7,()=>canvas.DrawToBitmap(image,canvas.ClientRectangle));
                    image.Save(Path.Combine(args[1],page+".png"));
                    // Compare independent root results with repeated canvas passes.
                    var before=project.Controls.Select(state.Visible).ToArray();
                    state.FocusId=999;project.Editor.Preview.Values["#preview.player.playing"]="true";
                    var changed=project.Controls.Select(state.Visible).ToArray();canvas.DrawToBitmap(image,canvas.ClientRectangle);
                    if(!changed.SequenceEqual(project.Controls.Select(state.Visible)))throw new Exception("Paint changed condition results.");
                    image.Save(Path.Combine(args[1],page+"-changed.png"));
                    project.Editor.PreviewMode="all";canvas.DrawToBitmap(image,canvas.ClientRectangle);
                    image.Save(Path.Combine(args[1],page+"-all.png"));
                    Console.WriteLine("PASS repeated paint and changed focus/sample visibility results");
                }
            }
        }
        var files=Directory.GetFiles(args[0],"*.xml",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray();
        var maps=files.Select(path=>Path.GetFullPath(path).Substring(Path.GetFullPath(args[0]).Length+1)+" "+Mapping(SkinSource.Read(path,Path.GetFileName(path)))).ToArray();
        File.WriteAllLines(Path.Combine(args[1],"Source-Mappings.txt"),maps);
        Console.WriteLine("PASS mapped "+files.Length+" XML files for independent baseline comparison");
    }
}
