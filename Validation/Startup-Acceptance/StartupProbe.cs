using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MP1SkinStudio;
class StartupAcceptanceProbe
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static object Call(StudioForm form,string name,params object[] args)
    {try{return typeof(StudioForm).GetMethod(name,flags).Invoke(form,args);}catch(TargetInvocationException ex){throw ex.InnerException;}}
    static T Field<T>(StudioForm form,string name){return (T)typeof(StudioForm).GetField(name,flags).GetValue(form);}
    static void Manifest(string path,Func<string,string> edit)
    {
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {var entry=zip.GetEntry("project.xml");string text;using(var reader=new StreamReader(entry.Open()))text=reader.ReadToEnd();entry.Delete();using(var writer=new StreamWriter(zip.CreateEntry("project.xml").Open(),new UTF8Encoding(false)))writer.Write(edit(text));}
    }
    [STAThread]static int Main(string[] args)
    {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        string demo=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);var lines=new List<string>();
        var demoHashes=Directory.GetFiles(demo,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>ProjectFile.Digest(File.ReadAllBytes(p)));
        foreach(string scenario in new[]{"healthy Unicode path","conflict cancel","conflict matching backup","conflict wrong backup","missing root backup","missing root cancel","corrupt snapshot","missing project"})
        {
            string fixture=Path.Combine(output,scenario);Directory.CreateDirectory(fixture);string source=Path.Combine(fixture,"Source"),backup=Path.Combine(fixture,"Backup");Directory.CreateDirectory(source);Directory.CreateDirectory(backup);
            string xml="<window><defaultcontrol>10</defaultcontrol><controls><control><type>button</type><id>10</id><posX>50</posX><width>120</width><height>40</height><label>Original</label></control></controls></window>";
            foreach(string root in new[]{source,backup})File.WriteAllText(Path.Combine(root,"main.xml"),xml,new UTF8Encoding(false));
            var p=new SkinProject(source);p.OpenPage("main.xml");p.Edit(p.Controls[0],new Dictionary<string,string>{{"label","Recovered"}},"Change label");string file=Path.Combine(fixture,"Saved work Ä & Ö.mp1skinproj");ProjectFile.Save(p,file);
            bool conflict=scenario.StartsWith("conflict"),missingRoot=scenario.StartsWith("missing root");if(conflict)File.AppendAllText(Path.Combine(source,"main.xml"),"<!--external-->");if(missingRoot)Directory.Move(source,source+"-moved");
            if(scenario=="corrupt snapshot")Manifest(file,t=>t.Replace("editedSha256=\"","editedSha256=\"bad"));if(scenario=="missing project")file=Path.Combine(fixture,"missing.mp1skinproj");
            byte[] archive=File.Exists(file)?File.ReadAllBytes(file):null;string sourceFile=Path.Combine(missingRoot?source+"-moved":source,"main.xml");byte[] sourceBefore=File.ReadAllBytes(sourceFile),backupBefore=File.ReadAllBytes(Path.Combine(backup,"main.xml"));
            bool succeeds=scenario=="healthy Unicode path"||scenario.EndsWith("matching backup")||scenario=="missing root backup";int expectedPrompts=conflict||missingRoot?1:0,expectedErrors=scenario=="conflict wrong backup"||scenario=="corrupt snapshot"||scenario=="missing project"?1:0;
            using(var form=new StudioForm(demo))
            {
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);var active=Field<SkinProject>(form,"project");string signature=ProjectFile.ContentSignature(active);int prompts=0,errors=0;Exception error=null;
                Func<string,string> choose=reason=>{Check(form.Visible&&form.IsHandleCreated,"Recovery dialog ran before its owner was visible");prompts++;return scenario.EndsWith("cancel")?null:scenario=="conflict wrong backup"?source:backup;};
                Action<Exception> report=e=>{Check(form.Visible&&form.IsHandleCreated,"Error dialog ran before its owner was visible");errors++;error=e;};
                Call(form,"QueueStartupProject",file,choose,report);Check(Field<SkinProject>(form,"project")==active&&prompts==0&&errors==0,"Startup project ran before Show");
                form.Show();bool drained=false;form.BeginInvoke(new Action(()=>drained=true));var timer=Stopwatch.StartNew();while(!drained&&timer.ElapsedMilliseconds<30000)Application.DoEvents();Check(drained,"Startup queue did not finish");
                var loaded=Field<SkinProject>(form,"project");Check(prompts==expectedPrompts&&errors==expectedErrors,"Unexpected chooser/error count: "+scenario);
                if(succeeds)
                {
                    Check(loaded!=active&&loaded.Controls[0].Get("label")=="Recovered"&&loaded.Undo.Count==1,"Saved changes were not restored");
                    Check((bool)Call(form,"SessionNeedsSaving")== (loaded.Root==backup),"Wrong clean/relocated baseline");
                    string zip=Path.Combine(fixture,"export.zip");loaded.Export(zip);using(var archiveZip=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archiveZip.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==loaded.Controls[0].Source.Text,"Startup export differs");
                    loaded.UndoChange();Check(!loaded.Dirty,"Startup restoration Undo failed");loaded.RedoChange();Check(loaded.Dirty,"Startup restoration Redo failed");
                    Call(form,"SaveSession",file);Check(!(bool)Call(form,"SessionNeedsSaving"),"Startup project was not saved cleanly");
                }
                else
                {
                    Check(loaded==active&&ProjectFile.ContentSignature(active)==signature&&!((bool)Call(form,"SessionNeedsSaving")),"Canceled/failed startup damaged the demo");
                    string notice=Field<Label>(form,"status").Text;Check(notice.Contains("current skin is still available"),"Startup recovery notice is missing");
                    if(scenario=="conflict wrong backup")Check(error is ProjectSourceConflictException,"Wrong backup lost typed rejection");
                    if(archive!=null)Check(File.ReadAllBytes(file).SequenceEqual(archive),"Canceled/failed startup changed archive");
                    // A failed startup can still open a valid saved project through the public API.
                    string healthy=Path.Combine(fixture,"Retry.mp1skinproj");var retry=new SkinProject(backup);retry.OpenPage("main.xml");ProjectFile.Save(retry,healthy);form.ResumeProjectFile(healthy);Check(Field<SkinProject>(form,"project").Root==backup,"Editor cannot open another project after startup failure");
                }
                form.Hide();form.Show();Application.DoEvents();Check(prompts==expectedPrompts&&errors==expectedErrors,"Showing editor twice repeated startup action");
            }
            Check(File.ReadAllBytes(sourceFile).SequenceEqual(sourceBefore)&&File.ReadAllBytes(Path.Combine(backup,"main.xml")).SequenceEqual(backupBefore),"Startup modified a skin");
            lines.Add("PASS "+scenario+": deferred owner visibility; bounded chooser/errors; preserved fallback or restored edits/export/Undo/Redo/save; no repeated startup; source/backup bytes unchanged.");
        }
        Check(demoHashes.All(p=>ProjectFile.Digest(File.ReadAllBytes(p.Key))==p.Value),"Packaged demo changed");
        lines.Add("PASS 8 synthetic Windows startup workflows; "+demoHashes.Count+" demo source/assets unchanged. Native dialog outcomes are injected; no desktop input or installed-client run.");
        File.WriteAllLines(Path.Combine(output,"Results.txt"),lines);foreach(string line in lines)Console.WriteLine(line);
    }
}
