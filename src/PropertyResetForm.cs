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
            var reset=new Button{Text="Reset",AccessibleName="Lokale Eigenschaft "+key+" zurücksetzen",Tag="reset:"+key,Width=52,Height=22,Location=new Point(195,0),FlatStyle=FlatStyle.Flat,BackColor=panel,ForeColor=Color.FromArgb(130,195,233),Font=new Font("Segoe UI",8)};
            reset.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);reset.Click+=(s,e)=>ShowPropertyResetMenu(reset,key);row.Controls.Add(reset);return reset;
        }
        string ResetBlockedReason(string key)
        {
            if(project==null||!editable.Contains(key)||SelectedControls().Length==0)return "Keine passende Auswahl.";
            var targets=SelectedControls();if(targets.Any(c=>project.IsLocked(c)))return "Die Auswahl enthält ein gesperrtes Element.";
            if(fields.Any(pair=>pair.Key!=key&&originals.ContainsKey(pair.Key)&&(pair.Value.Text!=originals[pair.Key]||(mixedFields.Contains(pair.Key)&&touchedFields.Contains(pair.Key)))))
                return "Andere Eigenschaften haben noch nicht übernommene Eingaben. Diese zuerst übernehmen.";
            int locals=0;
            foreach(var control in targets)
            {
                var preview=project.PreviewPropertyReset(control,key);if(!preview.HasLocal)continue;
                if(!preview.Allowed)return preview.Reason;
                if(!unlock.Checked&&preview.LocalValue.Contains("#"))return "Lokale Bindung geschützt. Zum Entfernen „Bindungen lokal ersetzen“ freigeben.";
                locals++;
            }
            return locals==0?"Keine lokale Überschreibung in der Auswahl vorhanden.":null;
        }
        void UpdateResetTools()
        {
            foreach(Control row in properties.Controls)foreach(Control item in row.Controls)
            {
                var button=item as Button;string tag=button==null?null:button.Tag as string;if(tag==null||!tag.StartsWith("reset:",StringComparison.Ordinal))continue;
                string key=tag.Substring(6),reason=ResetBlockedReason(key);button.Enabled=reason==null;
                tips.SetToolTip(button,reason??"Lokale Überschreibung entfernen. Im Menü wird der danach wirksame Style-/Referenzwert angezeigt.");
            }
        }
        void ShowPropertyResetMenu(Control anchor,string key)
        {
            var targets=SelectedControls();if(targets.Length==0)return;
            var previews=targets.Select(c=>project.PreviewPropertyReset(c,key)).ToArray();
            var menu=StudioMenus.For(this,"property-reset");menu.Items.Add("Eigenschaft: "+key).Enabled=false;
            foreach(string after in previews.Select(p=>p.AfterDescription).Distinct().Take(4))menu.Items.Add("Danach: "+after.Replace("&","&&")).Enabled=false;
            if(previews.Select(p=>p.AfterDescription).Distinct().Count()>4)menu.Items.Add("Weitere unterschiedliche Werte …").Enabled=false;
            menu.Items.Add(new ToolStripSeparator());string reason=ResetBlockedReason(key);
            var command=menu.Items.Add("Lokale Überschreibung entfernen ("+previews.Count(p=>p.HasLocal)+" von "+targets.Length+")",null,(s,e)=>Attempt(()=>ResetSelectedProperty(key)));command.Enabled=reason==null;
            if(reason!=null)menu.Items.Add(reason).Enabled=false;
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void ResetSelectedProperty(string key)
        {
            string reason=ResetBlockedReason(key);if(reason!=null)throw new InvalidOperationException(reason);
            string identity=selected.Key;project.ResetProperty(SelectedControls(),key,unlock.Checked);RefreshProject(identity);
            ShowNotice("Lokaler Wert für "+key+" entfernt. Herkunft und wirksamen Wert rechts prüfen. Die gesamte Aktion ist ein Rückgängig-Schritt.");
        }
        void RunPropertyResetUiSmoke(Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));
            string key=selected.Key,original=selected.Source.Text;
            verify(ResetBlockedReason("width")==null,"Lokale Eigenschaft ist zum Rücksetzen verfügbar");
            ResetSelectedProperty("width");
            verify(selected.Node.Child("width")==null&&fields["width"].Text==selected.Get("width")&&!selected.Properties["width"].Local,"Rücksetzen übernimmt Referenzwert und zeigt geerbte Herkunft");
            verify(ResetBlockedReason("width")!=null,"Bereits geerbte Eigenschaft kann nicht nochmals entfernt werden");
            undo.PerformClick();verify(selected.Source.Text==original,"Ein Undo stellt vollständige lokale Eigenschaft bytegenau wieder her");
            fields["posY"].Text="640";bool rejected=false;try{ResetSelectedProperty("width");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&fields["posY"].Text=="640"&&!project.Dirty,"Andere noch nicht übernommene Eingaben blockieren Rücksetzen und bleiben erhalten");SelectControl(project.Controls.First(c=>c.Key==key));
            SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"),true,false);ResetSelectedProperty("width");
            verify(SelectedControls().All(c=>c.Node.Child("width")==null)&&project.Undo.Count==1&&canvas.SelectedKeys.Count==2,"Mehrfachauswahl übernimmt geerbte Breiten in einem Undo-Schritt");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Time"));rejected=false;
            try{ResetSelectedProperty("label");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&selected.Get("label").Contains("#")&&!project.Dirty,"Lokale Datenbindung bleibt auch beim Rücksetzen geschützt");
            unlock.Checked=true;ResetSelectedProperty("label");verify(selected.Node.Child("label")==null,"Ausdrücklich freigegebene lokale Bindung kann entfernt werden");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));SetProtection(true,true);rejected=false;
            try{ResetSelectedProperty("width");}catch(InvalidOperationException){rejected=true;}
            verify(rejected&&!project.Dirty,"Elementschutz blockiert Rücksetzen lokaler Eigenschaften");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
