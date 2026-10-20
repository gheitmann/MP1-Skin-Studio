using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        void RunPropertyToolsUiSmoke(Action<bool,string> verify)
        {
            string xml=ProjectFile.XmlSignature(project),key=selected.Key;
            verify(properties.Controls.OfType<Button>().Any(b=>Convert.ToString(b.Tag)=="group:Text")&&fields.Values.All(b=>Convert.ToString(b.Parent.Tag).StartsWith("property-group:",StringComparison.Ordinal)),"Applicable properties are organized under named groups");
            var header=properties.Controls.OfType<Button>().First(b=>Convert.ToString(b.Tag)=="group:Position and size");
            fields["posX"].Text="222";header.PerformClick();Application.DoEvents();
            verify(!fields["posX"].Visible&&PendingProperties()&&fields["posX"].Text=="222","Collapsing a group preserves its unapplied inputs and pending guard");
            header.PerformClick();Apply();verify(selected.Get("posX")=="222"&&project.Undo.Count==1,"A draft retained through collapsing applies as one undo step");undo.PerformClick();
            header=properties.Controls.OfType<Button>().First(b=>Convert.ToString(b.Tag)=="group:Position and size");header.PerformClick();SelectControl(project.Controls.First(c=>c.Get("description")=="Series"));
            verify(!fields["posX"].Visible,"Collapsed groups remain collapsed when selecting another element");
            header=properties.Controls.OfType<Button>().First(b=>Convert.ToString(b.Tag)=="group:Position and size");header.PerformClick();SelectControl(project.Controls.First(c=>c.Key==key));
            fields["posX"].Focus();Application.DoEvents();verify(propertyHelp.Text.Contains("skin pixels"),"Keyboard focus exposes plain-language property help");
            var picker=fields["font"].Parent.Controls.OfType<Button>().First(b=>b.Tag==null);
            using(var timer=new Timer{Interval=50})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<FontPickerDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();dialog.DialogResult=DialogResult.Cancel;dialog.Close();};timer.Start();picker.PerformClick();
            }
            verify(ProjectFile.XmlSignature(project)==xml&&!PendingProperties(),"Cancelling the font picker leaves fields and XML unchanged");
            string chosen=project.FontNames.First(n=>n!=fields["font"].Text);
            using(var timer=new Timer{Interval=50})
            {
                timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<FontPickerDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();FindFontList(dialog).SelectedItem=chosen;dialog.DialogResult=DialogResult.OK;dialog.Close();};timer.Start();picker.PerformClick();
            }
            verify(fields["font"].Text==chosen&&PendingProperties()&&ProjectFile.XmlSignature(project)==xml,"Choosing a font creates a property draft without mutating XML");Apply();undo.PerformClick();
            verify(ProjectFile.XmlSignature(project)==xml&&!project.Dirty,"Font changes use the existing apply and byte-preserving undo workflow");
            var testProject=new SkinProject(project.Root);testProject.OpenPage("demo.xml");testProject.FontNames.Clear();testProject.FontNames.Add("Missing family");testProject.FontSpecs["Missing family"]=new SkinFontSpec{Family="Studio missing font family 28941",Height=18};
            using(var dialog=new FontPickerDialog(testProject,"Missing family","Sample Aa 0123"))
            {
                dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();
                verify(dialog.PreviewDetails.Contains("Substitute for unavailable font")&&dialog.PreviewDetails.Contains("px"),"Font preview reports unavailable families, substitute name and actual pixel size");
                var filter=dialog.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<TextBox>().First(b=>b.AccessibleName=="Filter skin fonts");filter.Text="no matches";
                verify(dialog.SelectedFontName==null&&dialog.PreviewDetails.Contains("No matching fonts"),"An empty font filter clears selection without retaining an old preview");filter.Text="";
                verify(dialog.SelectedFontName=="Missing family","Clearing the filter restores available skin font choices");dialog.Close();
            }
            SelectControl(project.Controls.First(c=>c.Key==key));
        }
        static ListBox FindFontList(Control parent)
        {foreach(Control c in parent.Controls){if(c is ListBox)return (ListBox)c;var found=FindFontList(c);if(found!=null)return found;}return null;}
    }
}
