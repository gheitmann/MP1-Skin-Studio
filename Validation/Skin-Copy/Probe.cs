using System;using System.IO;using System.Linq;using System.Reflection;using System.Drawing;using System.Collections.Generic;using System.Threading;using System.Windows.Forms;using MP1SkinStudio;
class SkinCopyProbe
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static List<string> results=new List<string>();static string output,demo;
    static T Field<T>(object o,string n){return (T)o.GetType().GetField(n,flags).GetValue(o);}
    static object Call(object o,string n,params object[] a){try{return o.GetType().GetMethods(flags).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);}catch(TargetInvocationException e){throw e.InnerException;}}
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);File.WriteAllLines(Path.Combine(output,"Progress.txt"),results);}
    static IEnumerable<Control> Desc(Control p){foreach(Control c in p.Controls){yield return c;foreach(var n in Desc(c))yield return n;}}
    static void CopyThroughDialog(StudioForm form,string name,bool open,bool include,bool discard)
    {
        bool started=false,seen=false;var clock=System.Diagnostics.Stopwatch.StartNew();
        using(var timer=new System.Windows.Forms.Timer{Interval=20})
        {
            timer.Tick+=(s,e)=>
            {
                if(clock.ElapsedMilliseconds>30000){timer.Stop();foreach(Form dialog in Application.OpenForms.Cast<Form>().ToArray().Where(f=>f!=form))dialog.Close();throw new Exception("Copy dialog timeout");}
                var copy=Application.OpenForms.OfType<SkinCopyDialog>().FirstOrDefault();
                if(copy!=null&&!started)
                {
                    seen=true;started=true;Field<TextBox>(copy,"name").Text=name;Field<TextBox>(copy,"parent").Text=output;Field<CheckBox>(copy,"open").Checked=open;Field<CheckBox>(copy,"include").Checked=include;Field<Button>(copy,"create").PerformClick();
                }
                var guard=Application.OpenForms.OfType<UnsavedSessionDialog>().FirstOrDefault();if(guard!=null){guard.DialogResult=discard?DialogResult.No:DialogResult.Cancel;guard.Close();}
            };
            timer.Start();Desc(form).OfType<Button>().Single(b=>b.Text=="Copy skin …").PerformClick();timer.Stop();
        }
        Check(seen&&Directory.Exists(Path.Combine(output,name)),"The actual Copy skin button creates a complete copy through the asynchronous dialog: "+name);
    }
    [STAThread]static void Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();demo=args[0];output=args[1];Directory.CreateDirectory(output);
            using(var form=new StudioForm(demo){StartPosition=FormStartPosition.Manual,Location=new Point(-20000,-20000)})
            {
                form.Show();Application.DoEvents();var original=Field<SkinProject>(form,"project");var fields=Field<Dictionary<string,TextBox>>(form,"fields");string width=fields["width"].Text;fields["width"].Text="321";
                Call(form,"ShowSkinCopy");Check(!Application.OpenForms.OfType<SkinCopyDialog>().Any()&&Field<SkinProject>(form,"project")==original&&fields["width"].Text=="321","Pending properties block copying without discarding the draft");fields["width"].Text=width;
                var selected=Field<SkinControl>(form,"selected");original.Edit(selected,new Dictionary<string,string>{{"width","321"}},"Copy fixture");Call(form,"RefreshProject",selected.Key);string signature=ProjectFile.ContentSignature(original);
                CopyThroughDialog(form,"Edited sibling",false,true,false);
                var copied=new SkinProject(Path.Combine(output,"Edited sibling"));copied.OpenPage(original.Page);
                Check(copied.Controls.Any(c=>c.Get("id")==selected.Get("id")&&c.Get("width")=="321")&&ProjectFile.ContentSignature(original)==signature&&Field<SkinProject>(form,"project")==original,"Applied XML appears in the new skin while the dirty original session remains intact");
                var picker=Field<ChoiceBox>(form,"skinPicker");Check(picker.Items.Cast<SkinChoice>().Any(c=>c.Root==copied.Root),"An external copy appears in the skin dropdown even when it is not opened");
                CopyThroughDialog(form,"Keep original open",true,true,false);Check(Field<SkinProject>(form,"project")==original&&ProjectFile.ContentSignature(original)==signature,"Cancelling the existing session guard keeps the original open and retains the completed copy");
                CopyThroughDialog(form,"Open new base",true,false,true);Check(Field<SkinProject>(form,"project").Root==Path.Combine(output,"Open new base")&&!Field<SkinProject>(form,"project").Dirty,"A disk-only base opens after the existing discard guard with a clean independent session");
                Check(Desc(form).OfType<Button>().Single(b=>b.Text=="Copy skin …").Parent.ClientRectangle.Contains(Desc(form).OfType<Button>().Single(b=>b.Text=="Copy skin …").Bounds),"Copy skin fits beside the SKIN heading without narrowing its dropdown");
            }
            using(var dialog=new SkinCopyDialog(new SkinProject(demo)))
            {
                dialog.Show();Application.DoEvents();Field<TextBox>(dialog,"parent").Text=output;Field<TextBox>(dialog,"name").Text="CON";Field<Button>(dialog,"create").PerformClick();Application.DoEvents();
                Check(!Field<bool>(dialog,"busy")&&Field<Button>(dialog,"create").Enabled&&Field<Label>(dialog,"status").Text.Contains("reserved"),"Invalid destination names leave an editable dialog with a clear error");dialog.Close();
            }
            var disk=new SkinProject(demo);disk.OpenPage("demo.xml");using(var dialog=new SkinCopyDialog(disk))
            {
                dialog.Show();Application.DoEvents();Field<TextBox>(dialog,"parent").Text=output;Field<TextBox>(dialog,"name").Text="Cancelled copy";Field<Button>(dialog,"create").PerformClick();Field<Button>(dialog,"cancel").PerformClick();
                var clock=System.Diagnostics.Stopwatch.StartNew();while(Field<bool>(dialog,"busy")&&clock.ElapsedMilliseconds<30000){Application.DoEvents();Thread.Sleep(5);}
                Check(!Field<bool>(dialog,"busy")&&dialog.CreatedRoot==null&&!Directory.Exists(Path.Combine(output,"Cancelled copy"))&&!Directory.GetDirectories(output,".skinstudio-copy-*").Any(),"Cancel cooperatively stops the worker and removes the unfinished copy");dialog.Close();
            }
            results.Add(results.Count+" passed, 0 failed. Synthetic forms/menu/dialog events; no desktop input.");File.WriteAllLines(Path.Combine(output,"Results.txt"),results);Console.WriteLine(results.Last());
        }
        catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
    }
}
