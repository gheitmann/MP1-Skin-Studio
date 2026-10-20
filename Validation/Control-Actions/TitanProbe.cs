using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using MP1SkinStudio;
class ControlActionsTitanProbe
{
    static int Main(string[] args){try{return Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static int Run(string[] args)
    {
        Directory.CreateDirectory(args[1]);var lines=new List<string>();int workflows=0,routes=0,rejected=0,automatic=0,explicitRoutes=0;var originals=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"SettingsGUI.xml","SettingsOSD.xml","SettingsSlideShow.xml","TVOSD.xml","videoOSD.AudioMenu.xml"})
        {
            var p=new SkinProject(args[0]){Theme=theme};p.OpenPage(page);var state=new PreviewState(p);string before=ProjectFile.XmlSignature(p),signature=ProjectFile.ContentSignature(p);SkinControl edit=null;int finalFocus=0,success=0,blocked=0;
            foreach(var source in p.Sources.Values)if(!originals.ContainsKey(source.Path))originals.Add(source.Path,ProjectFile.Digest(File.ReadAllBytes(source.Path)));
            foreach(var c in p.Controls.Where(c=>PreviewControlActions.Limited(c)).ToArray())
            {
                int? id=SkinProject.ControlId(c);if(!id.HasValue||id<=1)continue;
                foreach(string direction in new[]{"onleft","onright","onup","ondown"})
                {
                    string mapping=c.Resolved(direction).Trim();bool auto=mapping.Length==0||mapping=="0";
                    state.FocusId=id.Value;
                    try
                    {
                        var target=p.PreviewNavigationTarget(state,direction);if(state.FocusId!=id.Value)throw new Exception("Focus probe leaked");
                        success++;if(auto)automatic++;else explicitRoutes++;if(edit==null&&SkinProject.ControlId(target)!=id){edit=c;finalFocus=SkinProject.ControlId(target).Value;}
                    }
                    catch(InvalidOperationException ex){blocked++;if(blocked==1)lines.Add("GUARD "+(theme.Length==0?"Default":theme)+" "+page+": "+ex.Message);}
                }
            }
            if(before!=ProjectFile.XmlSignature(p)||signature!=ProjectFile.ContentSignature(p)||p.Dirty||p.Undo.Count>0)throw new Exception("Navigation inspection changed XML/state");
            routes+=success;rejected+=blocked;
            if(edit==null){edit=p.LogicalControls().FirstOrDefault(c=>PreviewControlActions.Limited(c)&&SkinProject.ControlId(c)>1&&p.LogicalControls().Count(x=>SkinProject.ControlId(x)==SkinProject.ControlId(c))==1);if(edit!=null)finalFocus=SkinProject.ControlId(edit).Value;}
            if(edit==null){lines.Add("BOUNDED "+(theme.Length==0?"Default":theme)+" "+page+": "+success+" accepted, "+blocked+" guarded routes; no editable auto route");continue;}
            finalFocus=SkinProject.ControlId(edit).Value;state.SetFocus(finalFocus);string key=edit.Key;string originalText=edit.Source.Text;
            p.Edit(edit,new Dictionary<string,string>{{"width",((int)edit.Number("width",100)+8).ToString(System.Globalization.CultureInfo.InvariantCulture)}},"Review grid button width");string after=p.Controls.First(c=>c.Key==key).Source.Text;
            string name=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page),file=Path.Combine(args[1],name+".mp1skinproj"),zip=Path.Combine(args[1],name+".zip");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var prepared=PreparedSkinSession.Create(loaded,loaded.Page);
            if(prepared.Preview.FocusId!=finalFocus||ProjectFile.ContentSignature(p)!=ProjectFile.ContentSignature(loaded))throw new Exception("Save/reopen lost grid focus/state");
            loaded.Export(zip);using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry(edit.Source.RelativePath).Open()))if(reader.ReadToEnd()!=after)throw new Exception("Export changed XML");
            loaded.UndoChange();if(loaded.Controls.First(c=>c.Key==key).Source.Text!=originalText)throw new Exception("Restored undo failed");loaded.RedoChange();if(loaded.Controls.First(c=>c.Key==key).Source.Text!=after)throw new Exception("Restored redo failed");
            p.UndoChange();if(p.Controls.First(c=>c.Key==key).Source.Text!=originalText)throw new Exception("Normal undo failed");p.RedoChange();if(p.Controls.First(c=>c.Key==key).Source.Text!=after)throw new Exception("Normal redo failed");
            foreach(var source in p.Sources.Values)if(ProjectFile.Digest(File.ReadAllBytes(source.Path))!=originals[source.Path])throw new Exception("Original XML changed during workflow");
            lines.Add("PASS "+name+": "+success+" accepted routes, "+blocked+" guarded; width edit/save/reopen/exact export/normal and restored Undo/Redo; focus "+finalFocus);workflows++;
        }
        foreach(var pair in originals)if(ProjectFile.Digest(File.ReadAllBytes(pair.Key))!=pair.Value)throw new Exception("Original hash changed");
        lines.Add("SUMMARY "+workflows+" complete real Titan workflows; "+routes+" accepted routes ("+automatic+" automatic, "+explicitRoutes+" explicit); "+rejected+" guarded routes; "+originals.Count+" distinct loaded original XML hashes unchanged.");
        File.WriteAllLines(Path.Combine(args[1],"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);
        if(workflows==0)throw new Exception("No real Titan grid workflow was accepted");return 0;
    }
}
