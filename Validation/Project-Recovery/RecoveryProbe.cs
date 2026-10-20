using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class ProjectRecoveryProbe
{
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    static object Call(StudioForm f,string method,params object[] args)
    {try{return typeof(StudioForm).GetMethod(method,flags).Invoke(f,args);}catch(TargetInvocationException ex){throw ex.InnerException;}}
    static T Field<T>(StudioForm f,string name){return (T)typeof(StudioForm).GetField(name,flags).GetValue(f);}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Dictionary<string,string> Hashes(string root)
    {return Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(root.Length),p=>ProjectFile.Digest(File.ReadAllBytes(p)),StringComparer.OrdinalIgnoreCase);}
    static void Match(string root,Dictionary<string,string> hashes)
    {var actual=Hashes(root);Check(actual.Count==hashes.Count&&hashes.All(p=>actual.ContainsKey(p.Key)&&actual[p.Key]==p.Value),"Full skin preservation failed: "+root);}
    static void Copy(string source,string target)
    {
        Directory.CreateDirectory(target);
        foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
        {string dest=Path.Combine(target,file.Substring(source.Length).TrimStart('\\','/'));Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(file,dest);}
    }
    [STAThread]static int Main(string[] args)
    {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        string original=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
        string source=Path.Combine(output,"Changed-Skin"),backup=Path.Combine(output,"Matching-Backup");Copy(original,source);Copy(original,backup);
        var originalHashes=Hashes(original);var backupHashes=Hashes(backup);var lines=new List<string>();int workflows=0,payloads=0;
        foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","myvideo.xml","SettingsGUI.xml"})
        {
            var p=new SkinProject(source){Theme=theme};p.OpenPage(page);
            var controls=p.Controls.Where(c=>c.Get("type")!="group"&&!c.IsBound("width")&&c.Number("width",0)>0&&p.Controls.Count(x=>x.Key==c.Key)==1).GroupBy(c=>c.Source).Take(3).Select(g=>g.First()).ToArray();
            Check(controls.Length==3,"Expected three editable source files");
            p.EditMany(controls.Select(c=>new ControlEdit(c,new Dictionary<string,string>{{"width",(c.Number("width",0)+9).ToString(CultureInfo.InvariantCulture)}})),"Recover three files");
            string stem=(theme.Length==0?"Default":theme)+"-"+Path.GetFileNameWithoutExtension(page),file=Path.Combine(output,stem+".mp1skinproj");ProjectFile.Save(p,file);byte[] saved=File.ReadAllBytes(file);
            string drift=controls[0].Source.Path;byte[] before=File.ReadAllBytes(drift);File.AppendAllText(drift,"<!--external recovery test-->");byte[] external=File.ReadAllBytes(drift);
            try
            {
                using(var form=new StudioForm(args[2]))
                {
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
                    var active=Field<SkinProject>(form,"project");var fields=Field<Dictionary<string,TextBox>>(form,"fields");string draft=fields["width"].Text;fields["width"].Text="613";
                    string signature=ProjectFile.ContentSignature(active);int prompts=0;
                    bool opened=(bool)Call(form,"OpenSessionWithRecovery",file,new Func<string,string>(reason=>{prompts++;Check(reason.Contains(controls[0].Source.RelativePath),"Missing conflict path");return null;}));
                    Check(!opened&&prompts==1&&Field<SkinProject>(form,"project")==active&&fields["width"].Text=="613"&&ProjectFile.ContentSignature(active)==signature,"Cancel damaged the active session/draft");
                    bool rejected=false;try{Call(form,"OpenSessionWithRecovery",file,new Func<string,string>(reason=>source));}catch(ProjectSourceConflictException){rejected=true;}
                    Check(rejected&&Field<SkinProject>(form,"project")==active&&fields["width"].Text=="613"&&ProjectFile.ContentSignature(active)==signature,"Wrong backup replaced active work");
                    fields["width"].Text=draft;prompts=0;
                    Check((bool)Call(form,"OpenSessionWithRecovery",file,new Func<string,string>(reason=>{prompts++;return backup;}))&&prompts==1,"Matching backup did not install");
                    var loaded=Field<SkinProject>(form,"project");
                    Check(loaded.Root==backup&&loaded.Page==page&&loaded.Theme==theme&&loaded.Sources.Values.Count(s=>s.Changed)==3&&loaded.Undo.Count==1,"Recovered session is incomplete");
                    Check((bool)Call(form,"SessionNeedsSaving")&&form.Text.EndsWith("*"),"Changed location is not marked unsaved");
                    string zip=Path.Combine(output,stem+".zip");loaded.Export(zip);
                    using(var archive=ZipFile.OpenRead(zip))
                    {
                        var changed=loaded.Sources.Values.Where(s=>s.Changed).ToArray();Check(archive.Entries.Count==changed.Length+1,"Unexpected export content");
                        foreach(var xml in changed)using(var input=archive.GetEntry(xml.RelativePath).Open())using(var memory=new MemoryStream())
                        {input.CopyTo(memory);Check(memory.ToArray().SequenceEqual(xml.Bytes()),"Recovered payload differs");payloads++;}
                    }
                    var undo=Field<Button>(form,"undo");var redo=Field<Button>(form,"redo");undo.PerformClick();Check(!loaded.Dirty&&(bool)Call(form,"SessionNeedsSaving"),"Undo lost relocation marker");redo.PerformClick();Check(loaded.Dirty,"Recovered Redo failed");
                    // A locked destination must preserve the pending location and original project.
                    using(var locked=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None))
                    {bool failed=false;try{Call(form,"SaveSession",file);}catch(IOException){failed=true;}Check(failed&&(bool)Call(form,"SessionNeedsSaving"),"Failed save cleared relocation marker");}
                    Check(File.ReadAllBytes(file).SequenceEqual(saved),"Failed save changed saved project");Call(form,"SaveSession",file);
                    Check(!(bool)Call(form,"SessionNeedsSaving")&&!form.Text.EndsWith("*")&&ProjectFile.Inspect(file).Root==backup,"Save did not remember/clear new location");
                    Call(form,"OpenSessionWithRecovery",file,new Func<string,string>(reason=>{throw new Exception("Repeated folder request after successful save");}));
                    Check(Field<SkinProject>(form,"project").Root==backup&&!(bool)Call(form,"SessionNeedsSaving"),"Direct reopen was not clean");
                    // Replacing the recovered session resets the location flag.
                    Call(form,"LoadProject",args[2],"demo.xml");Check(!(bool)Call(form,"SessionNeedsSaving"),"Relocation marker leaked to another skin");
                    form.Close();
                }
                Check(File.ReadAllBytes(drift).SequenceEqual(external),"Recovery overwrote the changed source");
            }
            finally{File.WriteAllBytes(drift,before);}
            lines.Add("PASS "+stem+": canceled/wrong backup preserves active project and property draft; matching full copy restores three XML files; location marker, exact export, Undo/Redo, failed/successful save and clean direct reopen.");workflows++;
        }
        Match(original,originalHashes);Match(source,originalHashes);Match(backup,backupHashes);
        lines.Add("PASS "+workflows+" synthetic Windows-form recovery workflows; "+payloads+" exact exported payload checks; all "+originalHashes.Count+" files unchanged in original baseline and both full skin copies after fixture restoration. No desktop input or installed-client run.");
        File.WriteAllLines(Path.Combine(output,"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);
    }
}

