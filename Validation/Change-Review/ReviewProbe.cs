using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using MP1SkinStudio;
class ReviewProbe
{
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static List<string> results=new List<string>();static int number;
    static T Field<T>(object owner,string name){return (T)owner.GetType().GetField(name,flags).GetValue(owner);}
    static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethod(name,flags).Invoke(owner,args);}catch(TargetInvocationException ex){throw ex.InnerException;}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Test(string name,Action test){test();results.Add("PASS "+name);}
    static SkinProject Fixture(string output)
    {
        string root=Path.Combine(output,"Fixture-"+(++number));Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,"main.xml"),"<window><controls><control><type>button</type><id>10</id><posX>10</posX></control><import>part.xml</import></controls></window>");
        File.WriteAllText(Path.Combine(root,"part.xml"),"<window><controls><control><type>label</type><id>20</id><label>Before</label></control></controls></window>");
        var p=new SkinProject(root);p.OpenPage("main.xml");p.Edit(p.Controls[0],new Dictionary<string,string>{{"posX","25"}},"Move");p.Edit(p.Controls[1],new Dictionary<string,string>{{"label","After"}},"Label");return p;
    }
    static ChangeReviewDialog Show(SkinProject p,bool pending=false)
    {var d=new ChangeReviewDialog(()=>ChangeReview.Capture(p),pending);d.StartPosition=FormStartPosition.Manual;d.Location=new Point(-20000,-20000);d.Show();Application.DoEvents();return d;}
    static Button ActionButton(ChangeReviewDialog d,string text){return d.Controls.OfType<FlowLayoutPanel>().SelectMany(c=>c.Controls.OfType<Button>()).Single(b=>b.Text==text);}
    static void OwnedDialog(StudioForm f,Action<ChangeReviewDialog> inspect,Action open)
    {
        int seen=0;using(var timer=new Timer{Interval=30}){timer.Tick+=(s,e)=>{var d=Application.OpenForms.OfType<ChangeReviewDialog>().SingleOrDefault();if(d==null)return;timer.Stop();seen++;Check(d.Owner==f,"Wrong dialog owner");inspect(d);ActionButton(d,"Close").PerformClick();};timer.Start();open();timer.Stop();Check(seen==1,"Expected one real modal review");}
    }
    [STAThread]static int Main(string[] args)
    {try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    static void Run(string[] args)
    {
        string demo=args[0],output=args[1];Directory.CreateDirectory(output);var hashes=Directory.GetFiles(demo,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>ProjectFile.Digest(File.ReadAllBytes(p)));
        Test("File list, metadata, source checks and read-only panes show all session changes",()=>
        {var p=Fixture(output);string sig=ProjectFile.ContentSignature(p);using(var d=Show(p)){var list=Field<ListView>(d,"list");Check(list.Items.Count==2&&Field<TextBox>(d,"details").ReadOnly,"Incomplete list/metadata");foreach(ListViewItem item in list.Items){item.Selected=true;Application.DoEvents();var file=(ReviewedXmlChange)item.Tag;Check(Field<TextBox>(d,"details").Text.Contains(file.OriginalHash)&&Field<TextBox>(d,"details").Text.Contains(file.EditedHash),"Wrong file details");Check(Field<TextBox>(Field<XmlReviewPane>(d,"original"),"content").ReadOnly&&Field<TextBox>(Field<XmlReviewPane>(d,"edited"),"content").ReadOnly,"Editable XML panes");}Check(sig==ProjectFile.ContentSignature(p)&&p.Undo.Count==2,"Review altered work");using(var bitmap=new Bitmap(d.Width,d.Height)){d.DrawToBitmap(bitmap,new Rectangle(0,0,d.Width,d.Height));bitmap.Save(Path.Combine(output,"Review.png"));}}});
        Test("File filter handles no matches and restores complete list",()=>
        {var p=Fixture(output);using(var d=Show(p)){var search=Field<TextBox>(d,"search");search.Text="PART.XML";Application.DoEvents();Check(Field<ListView>(d,"list").Items.Count==1&&Field<TextBox>(d,"details").Text.Contains("part.xml"),"Case filter lost details");search.Text="*";Application.DoEvents();Check(Field<ListView>(d,"list").Items.Count==0&&Field<Label>(d,"summary").Text.Contains("0 shown")&&Field<TextBox>(Field<XmlReviewPane>(d,"edited"),"content").Text=="","Empty filter retained stale XML");search.Clear();Application.DoEvents();Check(Field<ListView>(d,"list").Items.Count==2,"Clear failed");}});
        Test("Source refresh reports drift and restoration without changing saved work",()=>
        {var p=Fixture(output);string sig=ProjectFile.ContentSignature(p),path=Path.Combine(p.Root,"part.xml");byte[] before=File.ReadAllBytes(path);using(var d=Show(p)){File.WriteAllText(path,"changed");ActionButton(d,"Refresh source checks").PerformClick();Application.DoEvents();Check(Field<Label>(d,"summary").Text.Contains("1 source conflict")&&Field<ListView>(d,"list").Items.Cast<ListViewItem>().Any(i=>i.SubItems[3].Text=="Source changed"),"Source drift not refreshed");File.WriteAllBytes(path,before);ActionButton(d,"Refresh source checks").PerformClick();Check(Field<Label>(d,"summary").Text.Contains("0 source conflict")&&ProjectFile.ContentSignature(p)==sig,"Refresh mutated work");}});
        Test("Undo then refresh removes unchanged files and clears obsolete details",()=>
        {var p=Fixture(output);using(var d=Show(p)){p.UndoChange();p.UndoChange();ActionButton(d,"Refresh source checks").PerformClick();Check(Field<ListView>(d,"list").Items.Count==0&&Field<TextBox>(d,"details").Text.Contains("No modified XML")&&Field<Label>(d,"summary").Text.Contains("Preview-only"),"Undo left stale files");}});
        Test("Large XML panes preserve every character through bounded sections",()=>
        {string text=new String('a',60000)+new String('b',60000)+new String('c',22017);using(var pane=new XmlReviewPane("Test")){pane.Size=new Size(560,400);Call(pane,"SetSource",text,120003);Check(Field<TextBox>(pane,"content").Text==text.Substring(120000)&&Field<TextBox>(pane,"content").SelectionStart==3,"First difference did not select correct section");var chunks=new List<string>();Call(pane,"ShowSection",0,0);chunks.Add(Field<TextBox>(pane,"content").Text);while(Field<Button>(pane,"next").Enabled){Field<Button>(pane,"next").PerformClick();chunks.Add(Field<TextBox>(pane,"content").Text);}Check(String.Concat(chunks)==text&&chunks.All(s=>s.Length<=60000),"Sections dropped text");Field<Button>(pane,"previous").PerformClick();Check(Field<TextBox>(pane,"content").Text==new String('b',60000),"Previous section wrong");}});
        Test("Exact section boundaries and empty XML do not create missing or extra pages",()=>
        {using(var pane=new XmlReviewPane("Test")){foreach(int length in new[]{0,1,59999,60000,60001,120000}){string text=new String('x',length);Call(pane,"SetSource",text,length);Check(Field<TextBox>(pane,"content").Text.Length==(length==0?0:(length%60000==0?60000:length%60000))&&!Field<Button>(pane,"next").Enabled,"Section boundary wrong: "+length);}}});
        Test("First difference action returns both panes to the selected file change",()=>
        {var p=Fixture(output);using(var d=Show(p)){var pane=Field<XmlReviewPane>(d,"edited");Field<TextBox>(pane,"content").Select(0,0);ActionButton(d,"First difference").PerformClick();var file=(ReviewedXmlChange)Field<ListView>(d,"list").SelectedItems[0].Tag;Check(Field<TextBox>(pane,"content").SelectionStart==file.FirstDifference,"First difference action failed");}});
        Test("Owned production review preserves pending fields, history, selection and save marker",()=>
        {var p=Fixture(output);using(var f=new StudioForm(demo)){f.LoadProject(p.Root,"main.xml");f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show();Application.DoEvents();var project=Field<SkinProject>(f,"project");project.Edit(project.Controls[0],new Dictionary<string,string>{{"posX","25"}},"Move");Call(f,"RefreshProject",new object[]{null});var fields=Field<Dictionary<string,TextBox>>(f,"fields");fields["posX"].Text="88";string sig=ProjectFile.ContentSignature(project),key=Field<SkinControl>(f,"selected").Key,title=f.Text;OwnedDialog(f,d=>Check(d.Controls.OfType<Label>().Any(l=>l.Text.Contains("Pending property inputs are excluded"))&&Field<ListView>(d,"list").Items.Count==1,"Draft scope not explained"),()=>Call(f,"ShowChangeReview"));Check(fields["posX"].Text=="88"&&ProjectFile.ContentSignature(project)==sig&&project.Undo.Count==1&&Field<SkinControl>(f,"selected").Key==key&&f.Text==title,"Review altered drafts/session");}});
        Test("Ctrl Shift E routes through the production editor to its owned modal review",()=>
        {using(var f=new StudioForm(demo)){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show();Application.DoEvents();OwnedDialog(f,d=>Check(Field<ListView>(d,"list").Items.Count==0,"Clean project had XML changes"),()=>Check((bool)Call(f,"ProcessCmdKey",new Message(),Keys.Control|Keys.Shift|Keys.E),"Shortcut not handled"));}});
        Test("Element menu exposes review and dispatches its real dialog callback",()=>
        {using(var f=new StudioForm(demo)){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show();Application.DoEvents();var anchor=Field<Button>(f,"protectButton");Call(f,"ShowProtectionMenu",anchor);var owners=typeof(StudioMenus).GetMethod("PeekForTest",BindingFlags.Static|BindingFlags.NonPublic);var menu=(StudioMenu)owners.Invoke(null,new object[]{f,"protection"});var item=menu.Items.Cast<ToolStripItem>().Single(i=>i.Text.StartsWith("Review XML changes"));menu.Close();OwnedDialog(f,d=>Check(d.Visible,"Menu dialog missing"),()=>item.PerformClick());}});
        Check(hashes.All(p=>ProjectFile.Digest(File.ReadAllBytes(p.Key))==p.Value),"Bundled assets modified");results.Add(results.Count+" passed, 0 failed; "+hashes.Count+" demo hashes unchanged. Synthetic form/menu/key events; no desktop input.");File.WriteAllLines(Path.Combine(output,"Results.txt"),results);foreach(string line in results)Console.WriteLine(line);
    }
}
