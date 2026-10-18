using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static class SkinBatchCheck
    {
        static string Cell(string value){return "\""+(value??"").Replace("\"","\"\"").Replace("\r"," ").Replace("\n"," ")+"\"";}
        public static int Run(string root,string output)
        {
            Directory.CreateDirectory(output);var rows=new List<string>{"Kind,Theme,File,Controls,Warnings,Result,Detail"};int parsed=0,parseFailed=0,pages=0,pageFailed=0,edits=0,editFailed=0,sessions=0;
            var files=Directory.GetFiles(root,"*.xml",SearchOption.AllDirectories);var original=files.ToDictionary(p=>p,p=>ProjectFile.Digest(File.ReadAllBytes(p)),StringComparer.OrdinalIgnoreCase);
            foreach(string file in files)
            {
                string relative=file.Substring(root.TrimEnd('\\').Length+1).Replace('\\','/');
                try{var source=SkinSource.Read(file,relative);if(ProjectFile.Digest(source.Bytes())!=original[file])throw new InvalidDataException("Unmodified XML is not byte-identical.");parsed++;}
                catch(Exception ex){parseFailed++;rows.Add("XML,,"+Cell(relative)+",,,FAIL,"+Cell(ex.Message));}
            }
            var themes=new List<string>{""};string themeRoot=Path.Combine(root,"Themes");if(Directory.Exists(themeRoot))themes.AddRange(Directory.GetDirectories(themeRoot).Select(Path.GetFileName));
            foreach(string theme in themes)
            {
                var project=new SkinProject(root){Theme=theme};
                foreach(string page in project.Pages())
                {
                    int count=0,warnings=0;string result="PASS",detail="";
                    try
                    {
                        project.OpenPage(page);count=project.Controls.Count;warnings=project.Warnings.Distinct().Count();pages++;
                        detail=String.Join(" | ",project.Warnings.Distinct());
                        int pos;var control=project.Controls.FirstOrDefault(c=>!LayoutTools.IsContainer(c)&&!c.AutomaticLayout&&!c.IsBound("posX")&&Int32.TryParse(c.Get("posX"),out pos)&&pos<Int32.MaxValue);
                        if(control!=null)
                        {
                            string key=control.Key,before=control.Source.Text;int value=Int32.Parse(control.Get("posX"),CultureInfo.InvariantCulture);project.Edit(control,new Dictionary<string,string>{{"posX",(value+1).ToString(CultureInfo.InvariantCulture)}},"Compatibility probe");
                            var edited=project.Controls.First(c=>c.Key==key);if(!edited.Source.Changed||edited.Get("posX")!=(value+1).ToString(CultureInfo.InvariantCulture))throw new InvalidOperationException("Edit probe had no effect.");
                            if(new[]{"BasicHome.xml","myHome.xml","myvideo.xml","mytvguide.xml","mytvhomeServer.xml"}.Contains(page,StringComparer.OrdinalIgnoreCase))
                            {
                                string snapshot=Path.Combine(output,"_probe.mp1skinproj"),zip=Path.Combine(output,"_probe.zip");ProjectFile.Save(project,snapshot);var loaded=ProjectFile.Load(snapshot);if(ProjectFile.XmlSignature(loaded)!=ProjectFile.XmlSignature(project))throw new InvalidOperationException("Project probe changes XML.");project.Export(zip);sessions++;
                            }
                            project.UndoChange();if(project.Sources.Values.Any(s=>s.Changed)||project.Controls.First(c=>c.Key==key).Source.Text!=before)throw new InvalidOperationException("Undo is incomplete.");edits++;
                        }
                    }
                    catch(Exception ex){result="FAIL";detail=ex.Message;if(count>0)editFailed++;else pageFailed++;project=new SkinProject(root){Theme=theme};}
                    rows.Add("PAGE,"+Cell(theme)+","+Cell(page)+","+count+","+warnings+","+result+","+Cell(detail));
                }
            }
            int changed=original.Count(pair=>!File.Exists(pair.Key)||ProjectFile.Digest(File.ReadAllBytes(pair.Key))!=pair.Value);
            File.WriteAllLines(Path.Combine(output,"skin-check.csv"),rows,new UTF8Encoding(false));
            var summary=new[]{"MP1 Skin Studio - complete XML loading and editing probe","Skin: "+Path.GetFileName(root),"XML files loaded byte-identically: "+parsed,"XML errors: "+parseFailed,"Themes including default: "+themes.Count,"Page variants loaded: "+pages,"Page loading errors: "+pageFailed,"Edit + exact undo: "+edits,"Editing errors: "+editFailed,"Project/export probes: "+sessions,"Modified original files: "+changed,"","Warnings are model findings, not fully proven MP1 runtime errors.","Rendering, plugins and navigation in MediaPortal have not been fully validated."};File.WriteAllLines(Path.Combine(output,"skin-check.txt"),summary,new UTF8Encoding(false));
            return parseFailed+pageFailed+editFailed+changed==0?0:1;
        }
    }
    public sealed partial class StudioForm
    {
        public void PrepareExternalPreview(string root,string page,string selectedTheme)
        {InstallProject(new SkinProject(root){Theme=selectedTheme??""},page,null);}
    }
}
