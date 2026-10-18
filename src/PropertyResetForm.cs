using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        Button AddPropertyResetButton(Panel row,string key)
        {
            var reset=new Button{Text="Reset",AccessibleName="Local property "+key+" reset",Tag="reset:"+key,Width=52,Height=22,Location=new Point(195,0),FlatStyle=FlatStyle.Flat,BackColor=panel,ForeColor=Color.FromArgb(130,195,233),Font=new Font("Segoe UI",8)};
            reset.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);reset.Click+=(s,e)=>ShowPropertyResetMenu(reset,key);row.Controls.Add(reset);return reset;
        }
        string ResetBlockedReason(string key)
        {
            if(project==null||!editable.Contains(key)||SelectedControls().Length==0)return "No matching selection.";
            var targets=SelectedControls();if(targets.Any(c=>project.IsLocked(c)))return "The selection contains a locked element.";
            if(fields.Any(pair=>pair.Key!=key&&originals.ContainsKey(pair.Key)&&(pair.Value.Text!=originals[pair.Key]||(mixedFields.Contains(pair.Key)&&touchedFields.Contains(pair.Key)))))
                return "Other properties have pending edits. Apply them first.";
            int locals=0;
            foreach(var control in targets)
            {
                var preview=project.PreviewPropertyReset(control,key);if(!preview.HasLocal)continue;
                if(!preview.Allowed)return preview.Reason;
                if(!unlock.Checked&&preview.LocalValue.Contains("#"))return "Local binding protected. Enable “Replace bindings locally” before removing it.";
                locals++;
            }
            return locals==0?"No local override exists in the selection.":null;
        }
        void UpdateResetTools()
        {
            foreach(Control row in properties.Controls)foreach(Control item in row.Controls)
            {
                var button=item as Button;string tag=button==null?null:button.Tag as string;if(tag==null||!tag.StartsWith("reset:",StringComparison.Ordinal))continue;
                string key=tag.Substring(6),reason=ResetBlockedReason(key);button.Enabled=reason==null;
                tips.SetToolTip(button,reason??"Remove the local override. The menu shows the style/reference value that will take effect.");
            }
        }
        void ShowPropertyResetMenu(Control anchor,string key)
        {
            var targets=SelectedControls();if(targets.Length==0)return;
            var previews=targets.Select(c=>project.PreviewPropertyReset(c,key)).ToArray();
            var menu=StudioMenus.For(this,"property-reset");menu.Items.Add("Property: "+key).Enabled=false;
            foreach(string after in previews.Select(p=>p.AfterDescription).Distinct().Take(4))menu.Items.Add("After reset: "+after.Replace("&","&&")).Enabled=false;
            if(previews.Select(p=>p.AfterDescription).Distinct().Count()>4)menu.Items.Add("More mixed values …").Enabled=false;
            menu.Items.Add(new ToolStripSeparator());string reason=ResetBlockedReason(key);
            var command=menu.Items.Add("Remove local override ("+previews.Count(p=>p.HasLocal)+" of "+targets.Length+")",null,(s,e)=>Attempt(()=>ResetSelectedProperty(key)));command.Enabled=reason==null;
            if(reason!=null)menu.Items.Add(reason).Enabled=false;
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void ResetSelectedProperty(string key)
        {
            string reason=ResetBlockedReason(key);if(reason!=null)throw new InvalidOperationException(reason);
            string identity=selected.Key;project.ResetProperty(SelectedControls(),key,unlock.Checked);RefreshProject(identity);
            ShowNotice("Local value for "+key+" removed. Check provenance and effective value on the right. The entire action is one undo step.");
        }
        void RunPropertyResetUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));
            string key=selected.Key,original=selected.Source.Text;
            verify(ResetBlockedReason("width")==null,"Local properties are available for reset");
            ResetSelectedProperty("width");
            verify(selected.Node.Child("width")==null&&fields["width"].Text==selected.Get("width")&&!selected.Properties["width"].Local,"Reset applies the reference value and shows inherited provenance");
            verify(ResetBlockedReason("width")!=null,"Already inherited properties cannot be removed again");
            undo.PerformClick();verify(selected.Source.Text==original,"One undo restores the complete local property byte-for-byte");
            fields["posY"].Text="640";bool rejected=false;try{ResetSelectedProperty("width");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&fields["posY"].Text=="640"&&!project.Dirty,"Other pending property edits block reset and remain intact");SelectControl(project.Controls.First(c=>c.Key==key));
            SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);ResetSelectedProperty("width");
            verify(SelectedControls().All(c=>c.Node.Child("width")==null)&&project.Undo.Count==1&&canvas.SelectedKeys.Count==2,"Multiple selection applies inherited widths in one undo step");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Time"));rejected=false;
            try{ResetSelectedProperty("label");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&selected.Get("label").Contains("#")&&!project.Dirty,"Local data bindings remain protected during reset");
            unlock.Checked=true;ResetSelectedProperty("label");verify(selected.Node.Child("label")==null,"Explicitly approved local bindings can be removed");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SetProtection(true,true);rejected=false;
            try{ResetSelectedProperty("width");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&!project.Dirty,"Element protection blocks local-property reset");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
