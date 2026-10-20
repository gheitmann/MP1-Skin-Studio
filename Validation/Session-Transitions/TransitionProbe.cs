using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MP1SkinStudio;
class SessionTransitionProbe
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static List<string> results=new List<string>();static int serial;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static object Call(StudioForm f,string name,params object[] args)
    {try{return typeof(StudioForm).GetMethod(name,flags).Invoke(f,args);}catch(TargetInvocationException ex){throw ex.InnerException;}}
    static T Field<T>(StudioForm f,string name){return (T)typeof(StudioForm).GetField(name,flags).GetValue(f);}
    static void Test(string name,Action test){test();results.Add("PASS "+name);Console.WriteLine(results.Last());}
    static StudioForm Fixture(string demo,string output,bool dirty)
    {
        string root=Path.Combine(output,"Fixture-"+(++serial));Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,"main.xml"),"<window><defaultcontrol>10</defaultcontrol><controls><control><type>button</type><id>10</id><posX>50</posX><width>120</width><height>40</height><label>Test</label></control></controls></window>");
        var f=new StudioForm(demo);f.LoadProject(root,"main.xml");f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show();Application.DoEvents();Call(f,"SaveSession",Path.Combine(root,"Saved.mp1skinproj"));
        if(dirty){var p=Field<SkinProject>(f,"project");p.Edit(p.Controls[0],new Dictionary<string,string>{{"posX","75"}},"Move");Call(f,"RefreshProject",new object[]{null});}
        return f;
    }
    static bool Transition(StudioForm f,Func<DialogResult> choose,Func<bool> save,Action<Exception> error)
    {return (bool)Call(f,"ConfirmSessionTransition",choose,save,error);}
    static bool NeedsSaving(StudioForm f){return (bool)Call(f,"SessionNeedsSaving");}
    static string Saved(StudioForm f){return Field<string>(f,"currentProjectFile");}
    static Button ButtonFor(UnsavedSessionDialog d,DialogResult result)
    {return d.Controls.OfType<FlowLayoutPanel>().SelectMany(p=>p.Controls.OfType<Button>()).Single(b=>b.DialogResult==result);}
    static void ChooseDialog(StudioForm owner,DialogResult answer,Action action)
    {
        int hits=0,ticks=0;using(var timer=new Timer{Interval=30})
        {
            timer.Tick+=(s,e)=>
            {
                ticks++;var dialog=Application.OpenForms.OfType<UnsavedSessionDialog>().SingleOrDefault();
                if(dialog==null){if(ticks>1000){timer.Stop();throw new Exception("Decision dialog did not open");}return;}
                Check(dialog.Owner==owner,"Decision dialog lost its owner");Check(dialog.AcceptButton==ButtonFor(dialog,DialogResult.Cancel)&&dialog.CancelButton==ButtonFor(dialog,DialogResult.Cancel),"Enter/Escape are not safe cancel defaults");
                foreach(var button in new[]{ButtonFor(dialog,DialogResult.Yes),ButtonFor(dialog,DialogResult.No),ButtonFor(dialog,DialogResult.Cancel)})Check(button.Parent.ClientRectangle.Contains(button.Bounds),"Decision action is clipped");
                hits++;timer.Stop();ButtonFor(dialog,answer).PerformClick();
            };
            timer.Start();action();timer.Stop();Check(hits==1,"Expected one owned decision dialog");
        }
    }
    [STAThread]static int Main(string[] args)
    {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        string demo=args[0],output=args[1];Directory.CreateDirectory(output);var hashes=Directory.GetFiles(demo,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>ProjectFile.Digest(File.ReadAllBytes(p)));
        Test("Clean sessions leave without asking or saving",()=>{using(var f=Fixture(demo,output,false))Check(Transition(f,()=>{throw new Exception("Unneeded decision");},()=>{throw new Exception("Unneeded save");},e=>{throw e;}),"Clean session blocked");});
        Test("Save succeeds only after the actual project is clean and persisted",()=>
        {using(var f=Fixture(demo,output,true)){Check(Transition(f,()=>DialogResult.Yes,()=> (bool)Call(f,"SaveEditorProject",false),e=>{throw e;}),"Successful save blocked");Check(!NeedsSaving(f)&&ProjectFile.Load(Saved(f)).Controls[0].Get("posX")=="75","Edits were not saved");}});
        Test("Canceled Save As preserves active project and unsaved work",()=>
        {using(var f=Fixture(demo,output,true)){var p=Field<SkinProject>(f,"project");string signature=ProjectFile.ContentSignature(p);Check(!Transition(f,()=>DialogResult.Yes,()=>false,e=>{throw e;})&&NeedsSaving(f)&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==1,"Canceled save lost state");}});
        Test("Locked save preserves old archive and blocks leaving",()=>
        {using(var f=Fixture(demo,output,true)){string file=Saved(f);byte[] before=File.ReadAllBytes(file);int errors=0;using(var locked=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None))Check(!Transition(f,()=>DialogResult.Yes,()=> (bool)Call(f,"SaveEditorProject",false),e=>{Check(e is IOException,"Unexpected save failure");errors++;}),"Failed save permitted leaving");Check(errors==1&&NeedsSaving(f)&&File.ReadAllBytes(file).SequenceEqual(before)&&Field<SkinProject>(f,"project").Undo.Count==1,"Failed save damaged project");}});
        Test("Discard permits a transition without prematurely modifying active work",()=>
        {using(var f=Fixture(demo,output,true)){var p=Field<SkinProject>(f,"project");string signature=ProjectFile.ContentSignature(p);Check(Transition(f,()=>DialogResult.No,()=>{throw new Exception("Discard saved");},e=>{throw e;})&&ProjectFile.ContentSignature(p)==signature&&NeedsSaving(f),"Discard changed active state before replacement");Check(ProjectFile.Load(Saved(f)).Controls[0].Get("posX")=="50","Discard changed archive");}});
        Test("Cancel preserves project history and does not save",()=>
        {using(var f=Fixture(demo,output,true)){var p=Field<SkinProject>(f,"project");string signature=ProjectFile.ContentSignature(p);Check(!Transition(f,()=>DialogResult.Cancel,()=>{throw new Exception("Cancel saved");},e=>{throw e;})&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==1,"Cancel changed state");}});
        Test("Pending property drafts cannot be silently saved",()=>
        {using(var f=Fixture(demo,output,false)){var fields=Field<Dictionary<string,TextBox>>(f,"fields");fields["posX"].Text="93";var p=Field<SkinProject>(f,"project");Check(!Transition(f,()=>DialogResult.Yes,()=>{throw new Exception("Pending draft was saved");},e=>{throw e;})&&fields["posX"].Text=="93"&&!p.Dirty&&NeedsSaving(f),"Pending draft was applied or lost");}});
        Test("An affirmative save result cannot hide remaining unsaved editor state",()=>
        {using(var f=Fixture(demo,output,true))Check(!Transition(f,()=>DialogResult.Yes,()=>true,e=>{throw e;})&&NeedsSaving(f),"Unclean save result permitted leaving");});
        Test("Preview-only state is retained by Save before leaving",()=>
        {using(var f=Fixture(demo,output,false)){var p=Field<SkinProject>(f,"project");p.Editor.ShowUnresolved=!p.Editor.ShowUnresolved;bool expected=p.Editor.ShowUnresolved;Check(!p.Dirty&&NeedsSaving(f),"Expected editor-only changes");Check(Transition(f,()=>DialogResult.Yes,()=> (bool)Call(f,"SaveEditorProject",false),e=>{throw e;})&&ProjectFile.Load(Saved(f)).Editor.ShowUnresolved==expected,"Preview state was not saved");}});
        foreach(bool close in new[]{false,true})foreach(DialogResult answer in new[]{DialogResult.Yes,DialogResult.No,DialogResult.Cancel})
        {
            bool closing=close;DialogResult selected=answer;
            Test((closing?"Real FormClosing":"Real session switch")+" / "+selected,()=>
            {
                using(var f=Fixture(demo,output,true))
                {
                    string file=Saved(f);var p=Field<SkinProject>(f,"project");string signature=ProjectFile.ContentSignature(p);bool permitted=false;
                    ChooseDialog(f,selected,()=>{if(closing){f.Close();permitted=f.IsDisposed;}else permitted=(bool)Call(f,"CanSwitch");});
                    Check(permitted==(selected!=DialogResult.Cancel),"Wrong close/switch result");
                    Check(ProjectFile.Load(file).Controls[0].Get("posX")== (selected==DialogResult.Yes?"75":"50"),"Wrong persisted decision");
                    if(selected==DialogResult.Cancel)Check(!f.IsDisposed&&NeedsSaving(f)&&ProjectFile.ContentSignature(p)==signature,"Canceled event damaged session");
                    if(!closing&&selected==DialogResult.No)Check(NeedsSaving(f)&&ProjectFile.ContentSignature(p)==signature,"Switch discard destroyed active state before load");
                }
            });
        }
        Test("Pending session switching stays blocked without opening a decision dialog",()=>
        {using(var f=Fixture(demo,output,false)){Field<Dictionary<string,TextBox>>(f,"fields")["posX"].Text="92";Check(!(bool)Call(f,"CanSwitch")&&!Application.OpenForms.OfType<UnsavedSessionDialog>().Any(),"Pending session opened a decision");}});
        Test("Pending close dialog disables Save, explains inputs and can keep editing",()=>
        {using(var f=Fixture(demo,output,false)){Field<Dictionary<string,TextBox>>(f,"fields")["posX"].Text="94";using(var timer=new Timer{Interval=30}){int seen=0;timer.Tick+=(s,e)=>{var d=Application.OpenForms.OfType<UnsavedSessionDialog>().SingleOrDefault();if(d==null)return;seen++;timer.Stop();Check(!ButtonFor(d,DialogResult.Yes).Enabled&&d.Controls.OfType<Label>().Any(l=>l.Text.Contains("pending property")),"Pending save guard is missing");ButtonFor(d,DialogResult.Cancel).PerformClick();};timer.Start();f.Close();timer.Stop();Check(seen==1&&!f.IsDisposed&&Field<Dictionary<string,TextBox>>(f,"fields")["posX"].Text=="94","Pending close lost draft");}}});
        Test("Clean FormClosing produces no dialog",()=>{using(var f=Fixture(demo,output,false)){f.Close();Check(f.IsDisposed,"Clean close blocked");}});
        Check(hashes.All(p=>ProjectFile.Digest(File.ReadAllBytes(p.Key))==p.Value),"Bundled source/assets changed");
        results.Add(results.Count+" passed, 0 failed; "+hashes.Count+" bundled source/asset hashes unchanged. Synthetic form events and owned dialog button events; no desktop input.");File.WriteAllLines(Path.Combine(output,"Results.txt"),results);
    }
}
