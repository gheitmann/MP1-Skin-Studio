using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using MediaPortal.GUI.Library;
using MP1SkinStudio;
using MP1SkinStudio.LiveBridge;
class CoverageProbe
{
 class Gap {public string Type,Field;public int Count;public readonly HashSet<string> Pages=new HashSet<string>(StringComparer.OrdinalIgnoreCase);}
 [STAThread]static int Main(string[] args)
 {
  try
  {
   Directory.CreateDirectory(args[1]);GUIGraphicsContext.form=new Form{ClientSize=new Size(1920,1080)};GUIGraphicsContext.Skin=args[0];GUIGraphicsContext.SkinSize=new Size(1920,1080);GUIGraphicsContext.OverScanWidth=1920;GUIGraphicsContext.OverScanHeight=1080;
   var originals=Directory.GetFiles(args[0],"*.xml",SearchOption.AllDirectories).ToDictionary(file=>file,file=>ProjectFile.Digest(File.ReadAllBytes(file)),StringComparer.OrdinalIgnoreCase);
   var fields=(string[])typeof(RuntimeProperty).Assembly.GetType("MP1SkinStudio.Live.LiveWire").GetField("Fields").GetValue(null);
   var resolve=typeof(GUIControlFactory).GetMethod("GetControlType",BindingFlags.Static|BindingFlags.NonPublic);var support=new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);var unknown=new HashSet<string>();var gaps=new Dictionary<string,Gap>();var pages=new List<string>();int controls=0,used=0,writable=0;
   var p=new SkinProject(args[0]);var watch=Stopwatch.StartNew();string themeRoot=Path.Combine(args[0],"Themes");var themes=new[]{""}.Concat(Directory.Exists(themeRoot)?Directory.GetDirectories(themeRoot).Select(Path.GetFileName):Enumerable.Empty<string>()).ToArray();
   foreach(string theme in themes)
   {
    p=new SkinProject(args[0]){Theme=theme};foreach(string page in p.Pages())
    {
     int pageControls=0,pageUsed=0,pageWritable=0;try
     {
      p.OpenPage(page);
      foreach(var c in p.Controls)
      {
       string kind=c.Get("type");HashSet<string> keys;if(!support.TryGetValue(kind,out keys))
       {
        keys=new HashSet<string>();support[kind]=keys;
        try{var doc=new XmlDocument();doc.LoadXml("<control><type>"+kind+"</type></control>");var type=resolve.Invoke(null,new object[]{doc.DocumentElement}) as Type;if(type!=null)keys.UnionWith(RuntimeProperty.SupportedFields(type));else unknown.Add(kind);}catch{unknown.Add(kind);}
       }
       pageControls++;
       foreach(string field in fields.Where(f=>c.Node.Child(f)!=null))
       {
        pageUsed++;if(keys.Contains(field)){pageWritable++;continue;}string key=kind+"/"+field;Gap gap;if(!gaps.TryGetValue(key,out gap)){gap=new Gap{Type=kind,Field=field};gaps.Add(key,gap);}gap.Count++;gap.Pages.Add((theme.Length==0?"base":theme)+"/"+page);
       }
      }
      controls+=pageControls;used+=pageUsed;writable+=pageWritable;pages.Add((theme.Length==0?"base":theme)+"/"+page+"\t"+pageControls+"\t"+pageWritable+"\t"+pageUsed+"\tOK");
     }
     catch(Exception ex){pages.Add((theme.Length==0?"base":theme)+"/"+page+"\t0\t0\t0\tLOAD: "+ex.GetBaseException().Message.Replace('\r',' ').Replace('\n',' '));}
    }
   }
   File.WriteAllLines(Path.Combine(args[1],"Pages.tsv"),new[]{"Page\tExpanded controls\tWritable explicit fields\tExplicit editable fields\tLoad"}.Concat(pages));
   var summary=new List<string>{"Static SDK property audit; this is not a runtime/rendering compatibility percentage.","Pages/themes audited: "+pages.Count,"Expanded controls: "+controls,"Explicit editable XML fields: "+used,"Native writable fields: "+writable,"Control kinds not resolved: "+String.Join(", ",unknown.OrderBy(x=>x)),"Elapsed ms: "+watch.ElapsedMilliseconds,"Missing field groups:"};
   summary.AddRange(gaps.Values.OrderByDescending(g=>g.Count).Select(g=>g.Type+" / "+g.Field+": "+g.Count+" uses; examples "+String.Join(", ",g.Pages.OrderBy(x=>x).Take(3))));
   foreach(var file in originals)if(ProjectFile.Digest(File.ReadAllBytes(file.Key))!=file.Value)throw new Exception("Installed XML changed.");summary.Add("PASS "+originals.Count+" installed XML hashes unchanged.");
   File.WriteAllLines(Path.Combine(args[1],"Coverage.txt"),summary);foreach(string line in summary.Take(27))Console.WriteLine(line);GUIGraphicsContext.form.Dispose();return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
