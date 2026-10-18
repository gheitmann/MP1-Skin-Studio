using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class PreviewSettingsDialog : Form
    {
        readonly SkinProject project;
        readonly DataGridView values=new DataGridView();
        readonly CheckedListBox plugins=new CheckedListBox();
        readonly TextBox pluginName=new TextBox();
        readonly ChoiceBox presets=new ChoiceBox();
        public PreviewSettings Result{get;private set;}
        public PreviewSettingsDialog(SkinProject skin)
        {
            project=skin;Text="Sample Data and Plugin States — MP1 Skin Studio";Size=new Size(1050,740);MinimumSize=new Size(900,620);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(224,235,245);
            var top=new Panel{Dock=DockStyle.Top,Height=100};top.Controls.Add(new Label{Text="SAMPLE VALUES FOR PREVIEWS",Location=new Point(14,11),Width=900,Height=25,Font=new Font(Font,FontStyle.Bold)});
            top.Controls.Add(new Label{Text="Preset",Location=new Point(14,51),Width=64,Height=27});presets.SetBounds(85,45,230,29);presets.BackColor=Color.FromArgb(16,23,32);presets.ForeColor=ForeColor;presets.Items.AddRange(new object[]{"Current state","Baseline data","Long title","Weather","News"});presets.SelectedIndex=0;presets.SelectedIndexChanged+=(s,e)=>Populate(presets.SelectedIndex==0?project.Editor.Preview:PreviewSettings.Preset(presets.SelectedItem.ToString()));top.Controls.Add(presets);
            top.Controls.Add(new Label{Text="Changes affect the preview after Apply and are saved in the editor project.",Location=new Point(332,47),Width=650,Height=43});
            var tabs=new TabControl{Dock=DockStyle.Fill};var dataTab=new TabPage("Sample data"){BackColor=BackColor,ForeColor=ForeColor};var pluginTab=new TabPage("Plugin states"){BackColor=BackColor,ForeColor=ForeColor};tabs.TabPages.Add(dataTab);tabs.TabPages.Add(pluginTab);
            var dataNote=new Label{Text="Edit variables and sample values; add custom rows. “— unverified —” means no sample value is set. Removed rows use baseline values again. Empty values show empty text.",Dock=DockStyle.Top,Height=57,Padding=new Padding(10,6,8,0)};
            values.Dock=DockStyle.Fill;values.AllowUserToAddRows=true;values.AllowUserToDeleteRows=true;values.AllowUserToResizeRows=false;values.RowHeadersWidth=30;values.BackgroundColor=Color.FromArgb(18,24,33);values.BorderStyle=BorderStyle.None;values.AutoGenerateColumns=false;values.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;values.SelectionMode=DataGridViewSelectionMode.CellSelect;
            values.EnableHeadersVisualStyles=false;values.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(36,48,64);values.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;values.DefaultCellStyle.BackColor=Color.FromArgb(20,27,37);values.DefaultCellStyle.ForeColor=ForeColor;values.DefaultCellStyle.SelectionBackColor=Color.FromArgb(25,89,135);values.DefaultCellStyle.SelectionForeColor=Color.White;values.GridColor=Color.FromArgb(55,71,91);values.RowTemplate.Height=29;
            values.Columns.Add(new DataGridViewTextBoxColumn{Name="Variable",HeaderText="Variable",FillWeight=38});values.Columns.Add(new DataGridViewTextBoxColumn{Name="Value",HeaderText="Sample value",FillWeight=62});values.Columns[1].DefaultCellStyle.NullValue="— unverified —";dataTab.Controls.Add(values);dataTab.Controls.Add(dataNote);
            var pluginNote=new Label{Text="Simulate active plugins. Names from conditions on the open page are prefilled. Unchecked names are treated as disabled. Unknown conditions remain unverified.",Dock=DockStyle.Top,Height=62,Padding=new Padding(10,6,8,0)};
            plugins.Dock=DockStyle.Fill;plugins.CheckOnClick=true;plugins.BackColor=Color.FromArgb(20,27,37);plugins.ForeColor=ForeColor;plugins.BorderStyle=BorderStyle.None;plugins.IntegralHeight=false;
            var addRow=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=47,Padding=new Padding(10,7,0,0)};pluginName.Width=430;pluginName.BackColor=Color.FromArgb(16,23,32);pluginName.ForeColor=ForeColor;addRow.Controls.Add(pluginName);var add=new Button{Text="Add",Width=112,Height=28,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor};add.Click+=(s,e)=>{string name=pluginName.Text.Trim();if(name.Length==0)return;var check=new PreviewSettings();check.Plugins.Add(name);try{check.Validate();AddPlugin(name,true);pluginName.Clear();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Plugin name",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};addRow.Controls.Add(add);
            pluginTab.Controls.Add(plugins);pluginTab.Controls.Add(addRow);pluginTab.Controls.Add(pluginNote);
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=53,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,8)};var accept=new Button{Text="Apply",Width=130,Height=33,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,BackColor=Color.FromArgb(25,112,170)};accept.Click+=(s,e)=>{try{AcceptSettings();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Validate sample data",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};bottom.Controls.Add(accept);var cancel=new Button{Text="Cancel",Width=112,Height=33,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);CancelButton=cancel;
            Controls.Add(tabs);Controls.Add(bottom);Controls.Add(top);Populate(project.Editor.Preview);
        }
        void AddPlugin(string name,bool active)
        {
            for(int index=0;index<plugins.Items.Count;index++)if(String.Equals(plugins.Items[index].ToString(),name,StringComparison.OrdinalIgnoreCase)){plugins.SetItemChecked(index,active);return;}
            plugins.Items.Add(name,active);
        }
        void Populate(PreviewSettings settings)
        {
            var current=PreviewSettings.BaseValues();foreach(var pair in settings.Values)current[pair.Key]=pair.Value;
            var keys=new HashSet<string>(current.Keys,StringComparer.OrdinalIgnoreCase);
            foreach(var control in project.Controls)foreach(string raw in control.Properties.Values.Select(p=>p.Value).Concat(new[]{control.IncludeCondition}))
                foreach(Match match in Regex.Matches(raw??"",@"#[A-Za-z_][A-Za-z0-9_.]*"))keys.Add(match.Value);
            values.Rows.Clear();foreach(string key in keys.OrderBy(k=>k,StringComparer.OrdinalIgnoreCase)){string value;values.Rows.Add(key,current.TryGetValue(key,out value)?value:null);}
            plugins.Items.Clear();foreach(string name in PreviewSettings.KnownPlugins(project))AddPlugin(name,settings.Plugins.Contains(name));foreach(string name in settings.Plugins)AddPlugin(name,true);
        }
        PreviewSettings Collect()
        {
            values.EndEdit();var settings=new PreviewSettings();var baseline=PreviewSettings.BaseValues();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(DataGridViewRow row in values.Rows)
            {
                if(row.IsNewRow)continue;string key=Convert.ToString(row.Cells[0].Value).Trim();if(key.Length==0)continue;if(!seen.Add(key))throw new InvalidOperationException("Duplicate variable: "+key);
                if(row.Cells[1].Value==null)continue;string value=Convert.ToString(row.Cells[1].Value),original;
                if(!baseline.TryGetValue(key,out original)||value!=original)settings.Values.Add(key,value);
            }
            foreach(var item in plugins.CheckedItems)settings.Plugins.Add(item.ToString());settings.Validate();return settings;
        }
        void AcceptSettings(){Result=Collect();DialogResult=DialogResult.OK;Close();}
        internal void PresetForTest(string name){presets.SelectedIndex=presets.FindStringExact(name);}
        internal StudioMenu OpenPresetMenuForTest(){return presets.OpenChoicesForTest();}
        internal void SetValueForTest(string key,string value)
        {foreach(DataGridViewRow row in values.Rows)if(!row.IsNewRow&&String.Equals(Convert.ToString(row.Cells[0].Value),key,StringComparison.OrdinalIgnoreCase)){row.Cells[1].Value=value;return;}values.Rows.Add(key,value);}
        internal void AcceptForTest(){AcceptSettings();}
        internal PreviewSettings CollectForTest(){return Collect();}
        public void RenderTo(string path)
        {Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
    }
    public sealed partial class StudioForm
    {
        void ShowPreviewSettings()
        {if(project==null)return;using(var dialog=new PreviewSettingsDialog(project))if(dialog.ShowDialog(this)==DialogResult.OK)ApplyPreviewSettings(dialog.Result);}
        void ApplyPreviewSettings(PreviewSettings settings)
        {
            project.Editor.Preview.SetFrom(settings);int focus=state.FocusId;state=new PreviewState(project){FocusId=focus};canvas.State=state;canvas.ResetAssets();
            UpdatePreviewTree();canvas.Invalidate();UpdateDiagnostics();UpdateProjectCaption();ShowNotice("Preview state applied: "+settings.Values.Count+" custom sample values, "+settings.Plugins.Count+" active plugin states. Save the editor project.");
        }
        void UpdatePreviewTree()
        {
            foreach(TreeNode leaf in TreeControlNodes())
            {var control=leaf.Tag as SkinControl;if(control==null)continue;bool? visible=state.Visible(control);leaf.Text=EditorMark(control)+(visible==false?"○ ":visible==null?"? ":"")+(LayoutTools.IsContainer(control)?"[Group] ":"")+DisplayName(control);leaf.ForeColor=leaf.Name=="search-context"?Color.FromArgb(141,177,211):visible==false?Color.FromArgb(108,125,145):ink;}
        }
        public void RenderPreviewSettingsTo(string path){using(var dialog=new PreviewSettingsDialog(project))dialog.RenderTo(path);}
    }
}
