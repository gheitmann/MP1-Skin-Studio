using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using MP1SkinStudio;
class FindingReportProbe
{
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [STAThread]static int Main(string[] args)
    {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        Directory.CreateDirectory(args[1]);var lines=new List<string>();var originals=new Dictionary<string,string>();int snapshots=0;
        foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","myvideo.xml","SettingsGUI.xml"})
        {
            var p=new SkinProject(args[0]){Theme=theme};p.OpenPage(page);var state=new PreviewState(p);string signature=ProjectFile.ContentSignature(p);
            foreach(var source in p.Sources.Values)originals[source.Path]=ProjectFile.Digest(File.ReadAllBytes(source.Path));
            foreach(bool preview in new[]{false,true})
            {
                var report=preview?PreviewProblems.Run(p,state):SkinAudit.Run(p);string stem=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page)+(preview?"-Preview":"-Audit");
                using(var dialog=new SkinProblemsDialog(report,preview))
                {
                    dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();
                    var filter=typeof(SkinProblemsDialog).GetMethod("FilterForTest",flags);var save=typeof(SkinProblemsDialog).GetMethod("SaveReportTo",flags);
                    var button=dialog.Controls.OfType<FlowLayoutPanel>().SelectMany(panel=>panel.Controls.OfType<Button>()).Single(b=>b.Text=="Save shown report");
                    dialog.Size=dialog.MinimumSize;Application.DoEvents();Check(button.Visible&&button.Parent.ClientRectangle.Contains(button.Bounds),"Report button clipped at minimum size");
                    string query=report.Issues.Count>0?report.Issues[0].Code:"NO_MATCH";
                    foreach(string search in new[]{"",query,"NO_MATCH_735942"})foreach(bool information in new[]{false,true})
                    {
                        filter.Invoke(dialog,new object[]{search,information});var expected=FindingReport.Filter(report,search,information);Check(dialog.VisibleCount==expected.Length,"Dialog filter differs from export");
                        foreach(string extension in new[]{".txt",".html"})
                        {
                            string path=Path.Combine(args[1],stem+"-"+snapshots+extension);save.Invoke(dialog,new object[]{path});
                            Check(File.ReadAllText(path)==FindingReport.Render(report,search,information,preview,extension==".html"),"Saved dialog snapshot differs from visible filter");snapshots++;
                        }
                    }
                    filter.Invoke(dialog,new object[]{"",true});var target=report.Issues.FirstOrDefault(i=>!String.IsNullOrEmpty(i.ControlKey));
                    if(target!=null){typeof(SkinProblemsDialog).GetMethod("SelectCodeForTest",flags).Invoke(dialog,new object[]{target.Code});Check((bool)typeof(SkinProblemsDialog).GetProperty("CanJump",flags).GetValue(dialog,null),"Saving disabled finding navigation");}
                    dialog.Close();
                }
                lines.Add("PASS "+stem+": audit/preview snapshot, both formats, information/search/empty filters, minimum-size action and unchanged project.");
            }
            Check(ProjectFile.ContentSignature(p)==signature&&!p.Dirty&&p.Undo.Count==0,"Reporting mutated project state");
        }
        foreach(var source in originals)Check(ProjectFile.Digest(File.ReadAllBytes(source.Key))==source.Value,"Original skin changed");
        lines.Add("PASS "+snapshots+" saved dialog snapshots; "+originals.Count+" original XML hashes unchanged. Synthetic form workflows; no desktop input or installed-client run.");
        File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);
    }
}
