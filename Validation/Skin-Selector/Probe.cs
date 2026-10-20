using System;using System.IO;using System.Linq;using System.Drawing;using System.Reflection;using System.Windows.Forms;using System.Collections.Generic;using MP1SkinStudio;
class SkinSelectorProbe
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static List<string> results=new List<string>();static string demo,skin,output;
    static T Field<T>(object owner,string name){return (T)owner.GetType().GetField(name,flags).GetValue(owner);}
    static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethods(flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException error){throw error.InnerException;}}
    static void Check(bool ok,string name){if(!ok){File.WriteAllLines(Path.Combine(output,"Progress.txt"),results);throw new Exception(name);}results.Add("PASS "+name);File.WriteAllLines(Path.Combine(output,"Progress.txt"),results);}
    static IEnumerable<Control> Desc(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Desc(child))yield return nested;}}
    static StudioForm Open(){var f=new StudioForm(demo){StartPosition=FormStartPosition.Manual,Location=new Point(-20000,-20000)};f.Show();f.Hide();f.Show();f.Activate();Application.DoEvents();return f;}
    static void Choose(StudioForm form,string root){var choice=Field<ChoiceBox>(form,"skinPicker");int index=choice.Items.FindIndex(o=>String.Equals(((SkinChoice)o).Root,root,StringComparison.OrdinalIgnoreCase));if(index<0)throw new Exception("Missing skin "+root);choice.SelectedIndex=index;Application.DoEvents();}
    static DarkScrollBar Bar(Control content,bool vertical){var host=(DarkScrollHost)content.Parent;host.RefreshBars();return host.Controls.OfType<DarkScrollBar>().Single(b=>Field<bool>(b,"vertical")==vertical);}
    static void Scroll(Control content,bool vertical,int code,int position=0){Call(content.Parent,"ScrollContent",vertical,code,position);Application.DoEvents();}
    static void Mouse(DarkScrollBar bar,string name,Point at){Call(bar,name,new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));}
    static void Render(StudioForm form,string name){foreach(var host in Desc(form).OfType<DarkScrollHost>())host.RefreshBars();form.RenderTo(Path.Combine(output,name+".png"));}
    static void GuardDialog(Action action,DialogResult result,Action<bool> done)
    {
        bool seen=false;using(var timer=new Timer{Interval=30})
        {timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<UnsavedSessionDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();seen=true;dialog.DialogResult=result;dialog.Close();};timer.Start();action();timer.Stop();}done(seen);
    }
    [STAThread]static void Main(string[] args)
    {
        Application.EnableVisualStyles();demo=args[0];skin=args[1];output=args[2];Directory.CreateDirectory(output);
        using(var f=Open())
        {
            var picker=Field<ChoiceBox>(f,"skinPicker");string installed=Directory.GetParent(skin).FullName;
            var expected=Directory.GetDirectories(installed).Where(p=>File.Exists(Path.Combine(p,"references.xml"))&&Directory.GetFiles(p,"*.xml").Any(x=>!new[]{"references.xml","fonts.xml","skin.xml","theme.xml"}.Contains(Path.GetFileName(x).ToLowerInvariant()))).ToArray();
            Check(expected.All(skinRoot=>picker.Items.Cast<SkinChoice>().Any(c=>String.Equals(c.Root,skinRoot,StringComparison.OrdinalIgnoreCase)))&&picker.Items.Cast<SkinChoice>().Any(c=>String.Equals(c.Root,demo,StringComparison.OrdinalIgnoreCase)),"Dropdown discovers every installed skin and the bundled demo");
            results.Add("DISCOVERED "+String.Join(" | ",picker.Items.Select(o=>o.ToString())));
            Choose(f,skin);Check(Field<SkinProject>(f,"project").Root==skin&&((SkinChoice)picker.SelectedItem).Root==skin,"Choosing DefaultWideHD opens it and keeps the selector synchronized");
            var main=Desc(f).OfType<TableLayoutPanel>().Single(p=>p.ColumnCount==3);Check(main.ColumnStyles[0].Width==319&&Field<TextBox>(f,"pageSearch").Width>270&&Field<TextBox>(f,"elementSearch").Width>240,"The full left column grows from 245 to 319 pixels and its filters expand with it");
            string page=Field<SkinProject>(f,"project").Page;var fields=Field<Dictionary<string,TextBox>>(f,"fields");string original=fields["width"].Text;fields["width"].Text="321";
            Choose(f,demo);Check(Field<SkinProject>(f,"project").Root==skin&&((SkinChoice)picker.SelectedItem).Root==skin&&fields["width"].Text=="321","A pending property edit blocks skin switching and restores the dropdown without losing the draft");fields["width"].Text=original;
            var list=Field<PageListBox>(f,"page");var tree=Field<TreeView>(f,"tree");var properties=Field<FlowLayoutPanel>(f,"properties");var diagnostics=Field<TextBox>(f,"diagnostics");
            var root=Field<SkinProject>(f,"project");string xml=ProjectFile.XmlSignature(root);var selected=Field<SkinControl>(f,"selected").Key;
            fields["width"].Text="321";fields["width"].Focus();Application.DoEvents();list.TopIndex=20;var vertical=Bar(list,true);int before=list.TopIndex;
            Mouse(vertical,"OnMouseDown",new Point(vertical.Width/2,vertical.Height-5));Mouse(vertical,"OnMouseUp",new Point(vertical.Width/2,vertical.Height-5));results.Add("ARROW before="+before+" after="+list.TopIndex+" page="+root.Page+" focus="+fields["width"].Focused+" draft="+fields["width"].Text);
            Check(list.TopIndex>before&&root.Page==page&&fields["width"].Focused&&fields["width"].Text=="321","Dark page scrollbar arrows scroll without selecting XML or stealing the property draft/focus");
            list.TopIndex=20;vertical=Bar(list,true);before=list.TopIndex;Call(vertical,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,vertical.Width/2,vertical.Height/2,-120));
            Check(list.TopIndex>before&&root.Page==page&&fields["width"].Focused,"The mouse wheel over dark scrollbar chrome scrolls the same page list without changing selection");
            Scroll(list,true,4,0);vertical=Bar(list,true);var thumb=(Rectangle)vertical.GetType().GetProperty("Thumb",flags).GetValue(vertical,null);var start=new Point(thumb.X+thumb.Width/2,thumb.Y+thumb.Height/2);var end=new Point(start.X,vertical.Height-22);Mouse(vertical,"OnMouseDown",start);Mouse(vertical,"OnMouseMove",end);Mouse(vertical,"OnMouseUp",end);
            Check(list.TopIndex>100&&root.Page==page&&fields["width"].Text=="321","Dragging the dark page thumb reaches later files while preserving active XML and draft");
            int originalExtent=list.HorizontalExtent;list.HorizontalExtent=1000;Application.DoEvents();Scroll(list,false,4,400);
            Check(Bar(list,false).ScrollPosition>0&&root.Page==page,"The horizontal page scrollbar reaches long filenames without choosing another XML");list.HorizontalExtent=originalExtent;
            var selectedNode=tree.SelectedNode;Scroll(tree,true,4,0);var first=tree.TopNode;Scroll(tree,true,4,Math.Max(1,Bar(tree,true).ScrollMaximum/2));
            Check(tree.TopNode!=first&&tree.SelectedNode==selectedNode,"The element-tree vertical thumb changes its viewport while retaining selection");
            var horizontal=Bar(tree,false);if(horizontal.Visible&&horizontal.ScrollMaximum>0){Scroll(tree,false,4,horizontal.ScrollMaximum);Check(Bar(tree,false).ScrollPosition>0&&tree.SelectedNode==selectedNode,"The element-tree horizontal scrollbar reaches long names without selecting another element");}
            int maximum=Bar(properties,true).ScrollMaximum;Scroll(properties,true,4,Math.Min(300,maximum));
            Check(properties.AutoScrollPosition.Y<0&&fields["width"].Text=="321"&&fields["width"].Focused,"The dark property thumb scrolls fields and preserves their values and focus");Scroll(properties,true,4,0);
            string information=diagnostics.Text;diagnostics.Text=String.Join(Environment.NewLine,Enumerable.Range(0,100).Select(i=>"Preview information line "+i));Application.DoEvents();Scroll(diagnostics,true,4,20);
            Check(Bar(diagnostics,true).ScrollPosition>0,"The dark information scrollbar scrolls the native text content");diagnostics.Text=information;
            Check(new Control[]{list,tree,properties,diagnostics}.All(content=>content.Parent is DarkScrollHost)&&Desc(f).OfType<DarkScrollBar>().Where(b=>b.Visible).All(b=>b.BackColor==Color.FromArgb(20,26,35)),"All four main content regions retain matching dark scrollbar chrome with the added toolbox");
            Check(Desc(f).OfType<DarkScrollHost>().All(host=>host.Controls.Cast<Control>().Where(c=>c!=host.Content).All(c=>host.Controls.GetChildIndex(c)<host.Controls.GetChildIndex(host.Content))),"Scrollbar overlays stay in front of native content windows in every host");
            Check(ProjectFile.XmlSignature(root)==xml&&!root.Dirty&&Field<SkinControl>(f,"selected").Key==selected,"Scrollbar input and wider layout never edit XML or change selected controls");fields["width"].Text=original;
            tree.SelectedNode.EnsureVisible();Scroll(tree,false,4,0);list.TopIndex=list.SelectedIndex;
            Render(f,"Default");f.Size=f.MinimumSize;Application.DoEvents();
            Check(picker.Parent.ClientRectangle.Contains(picker.Bounds)&&Field<TextBox>(f,"pageSearch").Parent.ClientRectangle.Contains(Field<TextBox>(f,"pageSearch").Bounds),"Skin and filename selectors remain inside the wider sidebar at minimum size");
            var tools=Desc(f).OfType<Button>().Where(b=>new[]{"Sample Data","Language","Collection"}.Contains(b.Text)).ToArray();
            Check(tools.Length==3&&tools.All(b=>b.Parent.ClientRectangle.Contains(b.Bounds)),"Preview tools wrap at minimum width instead of being clipped by the wider sidebar");Render(f,"Minimum");f.Size=new Size(1520,960);Application.DoEvents();
            root.Editor.Zoom=2;string signature=ProjectFile.ContentSignature(root);
            GuardDialog(()=>Choose(f,demo),DialogResult.Cancel,seen=>Check(seen&&Field<SkinProject>(f,"project")==root&&ProjectFile.ContentSignature(root)==signature&&((SkinChoice)picker.SelectedItem).Root==skin,"Cancelling the existing unsaved-session dialog preserves the current skin and metadata"));
            GuardDialog(()=>Choose(f,demo),DialogResult.No,seen=>Check(seen&&Field<SkinProject>(f,"project").Root==demo,"Discard confirmation switches skins through the existing protected session transition"));
            var menu=(ContextMenuStrip)Call(picker,"OpenChoicesForTest");Check(menu.Items.Count==picker.Items.Count+2&&menu.Items[picker.Items.Count] is ToolStripSeparator&&menu.Items[menu.Items.Count-1].Text=="Select different Skin Folder …","Skin dropdown lists discovered skins then a separated folder-selection action");int choose=picker.Items.FindIndex(o=>((SkinChoice)o).Root==skin);Call(menu,"ChooseForTest",choose);Application.DoEvents();Check(Field<SkinProject>(f,"project").Root==skin,"Choosing an actual dropdown menu item loads its skin");
            int selectedIndex=picker.SelectedIndex;menu=(ContextMenuStrip)Call(picker,"OpenChoicesForTest");Check(menu.Items.Count==picker.Items.Count+2&&picker.SelectedIndex==selectedIndex&&picker.Items.All(o=>o is SkinChoice),"Reopening adds one folder action without turning it into a selected skin");menu.Close();
            var themePicker=Field<ChoiceBox>(f,"theme");menu=(ContextMenuStrip)Call(themePicker,"OpenChoicesForTest");Check(menu.Items.Count==themePicker.Items.Count&&!menu.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Select different Skin Folder …"),"Theme choices keep their existing entries without the skin-folder action");menu.Close();
        }
        using(var f=Open())
        {
            string folder=Path.Combine(output,"Skins");string first=Path.Combine(folder,"First");Directory.CreateDirectory(first);File.WriteAllText(Path.Combine(first,"references.xml"),"<window/>");File.WriteAllText(Path.Combine(first,"main.xml"),"<window><controls/></window>");f.LoadProject(first,"main.xml");
            string added=Path.Combine(folder,"Added");Directory.CreateDirectory(added);File.WriteAllText(Path.Combine(added,"references.xml"),"<window/>");File.WriteAllText(Path.Combine(added,"main.xml"),"<window><controls/></window>");var picker=Field<ChoiceBox>(f,"skinPicker");var menu=(ContextMenuStrip)Call(picker,"OpenChoicesForTest");Check(picker.Items.Cast<SkinChoice>().Any(c=>c.Root==added),"Opening the dropdown refreshes newly added sibling skins automatically");menu.Close();
            string broken=Path.Combine(folder,"Broken");Directory.CreateDirectory(broken);File.WriteAllText(Path.Combine(broken,"references.xml"),"<broken");File.WriteAllText(Path.Combine(broken,"main.xml"),"<window/>");Call(f,"RefreshSkinChoices");var previous=Field<SkinProject>(f,"project");
            typeof(StudioForm).GetField("loading",flags).SetValue(f,true);picker.SelectedIndex=picker.Items.FindIndex(o=>((SkinChoice)o).Root==broken);typeof(StudioForm).GetField("loading",flags).SetValue(f,false);bool rejected=false;try{Call(f,"ChooseSkin");}catch{rejected=true;}
            Check(rejected&&Field<SkinProject>(f,"project")==previous&&((SkinChoice)picker.SelectedItem).Root==first,"An invalid discovered skin leaves the previous project intact and restores the selector");
        }
        results.Add(results.Count(r=>r.StartsWith("PASS "))+" passed, 0 failed. Synthetic forms/menu/mouse events; no desktop input.");File.WriteAllLines(Path.Combine(output,"Results.txt"),results);Console.WriteLine(results.Last());
    }
}
