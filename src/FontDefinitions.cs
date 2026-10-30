using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using MP1SkinStudio.Live;

namespace MP1SkinStudio
{
    public sealed partial class SkinSource
    {
        internal void SetDefinitionValues(ElementSpan element,IDictionary<string,string> values)
        {
            var patches=new List<Tuple<int,int,string>>();string additions="",newline=Text.Contains("\r\n")?"\r\n":"\n";
            foreach(var pair in values)
            {
                var matches=element.Children.Where(n=>n.Name==pair.Key).ToArray();if(matches.Length>1)throw new InvalidOperationException("Ambiguous font property: "+pair.Key);
                string escaped=SecurityElement.Escape(pair.Value);if(matches.Length==0)additions+=newline+"    <"+pair.Key+">"+escaped+"</"+pair.Key+">";
                else
                {
                    var node=matches[0];if(node.Children.Count>0||node.Element.ChildNodes.Cast<System.Xml.XmlNode>().Any(n=>n.NodeType!=System.Xml.XmlNodeType.Text&&n.NodeType!=System.Xml.XmlNodeType.Whitespace))throw new InvalidOperationException("A complex font property is protected: "+pair.Key);
                    string replacement=escaped;if(node.Empty){string opening=Text.Substring(node.Start,node.End-node.Start);replacement=opening.Substring(0,opening.LastIndexOf('/'))+">"+escaped+"</"+pair.Key+">";}patches.Add(Tuple.Create(node.Empty?node.Start:node.OpenEnd,node.Empty?node.End:node.CloseStart,replacement));
                }
            }
            if(additions.Length>0){if(element.Empty)throw new InvalidOperationException("An empty definition cannot be expanded.");patches.Add(Tuple.Create(element.CloseStart,element.CloseStart,additions+newline+"  "));}
            foreach(var patch in patches.OrderByDescending(p=>p.Item1))Text=Text.Substring(0,patch.Item1)+patch.Item3+Text.Substring(patch.Item2);
        }
    }
    public sealed partial class SkinProject
    {
        void CheckFontEdit(string name)
        {
            if(String.IsNullOrWhiteSpace(name)||name.Length>100||name.StartsWith("@",StringComparison.Ordinal)||name.StartsWith("mp1studiofont_",StringComparison.OrdinalIgnoreCase)||name.Any(c=>Char.IsControl(c)||"<>/\\:".Contains(c)))throw new InvalidOperationException("Enter a short, unique font name.");
            SkinFontSpec spec;if(FontSpecs.TryGetValue(name,out spec)&&Controls.Any(c=>{SkinFontSpec used;return FontSpecs.TryGetValue(c.Resolved("font"),out used)&&Object.ReferenceEquals(spec,used)&&IsLocked(c);}))throw new InvalidOperationException("Unlock the affected text elements before changing this font definition.");
        }
        public void SetFontDefinition(string name,string family,int height,bool bold,bool italic)
        {
            CheckFontEdit(name);int style=(bold?1:0)|(italic?2:0);new LiveFontDefinition{Family=family,Height=height,Style=style}.Validate();
            using(var fontFamily=new FontFamily(family))if(!fontFamily.IsStyleAvailable((FontStyle)style))throw new InvalidOperationException("Choose a style available in this installed font family.");
            SetFontRecord("font",name,new Dictionary<string,string>{{"filename",family},{"height",height.ToString(CultureInfo.InvariantCulture)},{"bold",bold?"yes":"no"},{"italic",italic?"yes":"no"}});
        }
        public void SetFontAlias(string name,string target)
        {
            CheckFontEdit(name);if(!FontSpecs.ContainsKey(target)||name==target)throw new InvalidOperationException("Choose an existing font as the alias target.");
            var source=Source(Resolve("fonts.xml"));var aliases=source.Root.Children.Where(n=>n.Name=="alias").ToDictionary(n=>n.Value("name"),n=>n.Value("fontname"));aliases[name]=target;
            string current=name;var seen=new HashSet<string>();while(aliases.ContainsKey(current)){if(!seen.Add(current))throw new InvalidOperationException("Font aliases cannot form a cycle.");current=aliases[current];}
            SetFontRecord("alias",name,new Dictionary<string,string>{{"fontname",target}});
        }
        void SetFontRecord(string kind,string name,Dictionary<string,string> values)
        {
            var source=Source(Resolve("fonts.xml"));if(source.Root.Name!="fonts")throw new InvalidOperationException("This skin has no supported fonts.xml definition list.");
            var nodes=source.Root.Children.Where(n=>(n.Name=="font"||n.Name=="alias")&&n.Value("name")==name).ToArray();if(nodes.Length>1||nodes.Length==1&&nodes[0].Name!=kind)throw new InvalidOperationException("The font name is duplicated or belongs to a different kind of definition.");
            Transaction(new[]{source},()=>
            {
                if(nodes.Length==1)source.SetDefinitionValues(nodes[0],values);
                else
                {
                    string newline=source.Text.Contains("\r\n")?"\r\n":"\n";string xml=newline+"  <"+kind+"><name>"+SecurityElement.Escape(name)+"</name>"+String.Concat(values.Select(p=>"<"+p.Key+">"+SecurityElement.Escape(p.Value)+"</"+p.Key+">"))+"</"+kind+">"+newline;
                    if(source.Root.Empty){string opening=source.Text.Substring(source.Root.Start,source.Root.End-source.Root.Start);source.Text=source.Text.Substring(0,source.Root.Start)+opening.Substring(0,opening.LastIndexOf('/'))+">"+xml+"</fonts>"+source.Text.Substring(source.Root.End);}else source.Text=source.Text.Insert(source.Root.CloseStart,xml);
                }
            },"Edit font definition "+name);
        }
    }
    public sealed class FontDefinitionsDialog:Form
    {
        readonly SkinProject project;readonly ChoiceBox names=new ChoiceBox(),families=new ChoiceBox(),aliases=new ChoiceBox();readonly TextBox name=new TextBox(),filter=new TextBox();readonly NumericUpDown height=new NumericUpDown();readonly CheckBox bold=new CheckBox(),italic=new CheckBox();readonly Label status=new Label();readonly string[] installed;
        bool aliasMode,selecting;public bool Applied {get;private set;}
        public FontDefinitionsDialog(SkinProject skin,string selected)
        {
            project=skin;Text="Skin font definitions";ClientSize=new Size(720,420);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);Font=new Font("Segoe UI",9.5f);AutoScaleMode=AutoScaleMode.None;
            var available=FontFamily.Families;try{installed=available.Select(f=>f.Name).Distinct().OrderBy(s=>s).ToArray();}finally{foreach(var family in available)family.Dispose();}
            Controls.Add(new Label{Text="Edit fonts.xml without opening XML. Changes affect every use of the definition.\r\nConnected MP previews supported changes with private fonts; Disconnect restores its originals.",Bounds=new Rectangle(16,12,688,48)});
            names.SetBounds(16,70,365,28);names.AccessibleName="Skin font definition";names.Items.AddRange(project.FontNames.Distinct().Cast<object>());names.SelectedIndex=Math.Max(0,project.FontNames.IndexOf(selected));Controls.Add(names);
            var create=new Button{Text="New font",Bounds=new Rectangle(393,69,145,30)};create.Click+=(s,e)=>New(false);Controls.Add(create);var newAlias=new Button{Text="New alias",Bounds=new Rectangle(548,69,156,30)};newAlias.Click+=(s,e)=>New(true);Controls.Add(newAlias);
            AddLabel("Name",16,112);name.SetBounds(16,137,688,26);name.ReadOnly=true;name.AccessibleName="Font definition name";Controls.Add(name);
            AddLabel("Filter installed families",16,179);filter.SetBounds(16,204,310,26);filter.AccessibleName="Filter installed font families";Controls.Add(filter);families.SetBounds(340,204,364,28);families.AccessibleName="Installed font family";Controls.Add(families);
            height.Minimum=1;height.Maximum=96;height.Value=18;height.SetBounds(16,274,130,28);height.AccessibleName="Skin font height";Controls.Add(height);AddLabel("Skin height (576-line scale)",16,248);bold.Text="Bold";bold.SetBounds(170,274,100,28);italic.Text="Italic";italic.SetBounds(280,274,100,28);Controls.Add(bold);Controls.Add(italic);
            aliases.SetBounds(16,274,688,28);aliases.AccessibleName="Font alias target";aliases.Items.AddRange(project.FontNames.Cast<object>());Controls.Add(aliases);
            status.SetBounds(16,314,688,52);Controls.Add(status);var apply=new Button{Text="Apply definition",Bounds=new Rectangle(393,375,145,32)};apply.Click+=(s,e)=>{try{if(aliasMode)project.SetFontAlias(name.Text,aliases.SelectedItem as string);else project.SetFontDefinition(name.Text,families.SelectedItem as string,(int)height.Value,bold.Checked,italic.Checked);Applied=true;DialogResult=DialogResult.OK;}catch(Exception ex){status.Text=ex.Message;}};Controls.Add(apply);var cancel=new Button{Text="Cancel",Bounds=new Rectangle(548,375,156,32),DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;
            foreach(var box in new TextBox[]{name,filter}){box.BackColor=Color.FromArgb(20,26,35);box.ForeColor=ForeColor;}height.BackColor=Color.FromArgb(20,26,35);height.ForeColor=ForeColor;
            foreach(var button in Controls.OfType<Button>()){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(35,48,64);button.ForeColor=ForeColor;}
            filter.TextChanged+=(s,e)=>FilterFamilies();names.SelectedIndexChanged+=(s,e)=>SelectDefinition();SelectDefinition();
        }
        void AddLabel(string text,int x,int y){Controls.Add(new Label{Text=text,Bounds=new Rectangle(x,y,330,23)});}
        void FilterFamilies(){string previous=families.SelectedItem as string;families.Items.Clear();families.Items.AddRange(installed.Where(s=>s.IndexOf(filter.Text,StringComparison.OrdinalIgnoreCase)>=0).Cast<object>());families.SelectedIndex=Math.Max(0,families.FindStringExact(previous));}
        void Mode(bool alias){aliasMode=alias;aliases.Visible=alias;height.Visible=bold.Visible=italic.Visible=!alias;filter.Enabled=families.Enabled=!alias;}
        void SelectDefinition()
        {
            if(selecting)return;string selected=names.SelectedItem as string;if(selected==null)return;name.Text=selected;name.ReadOnly=true;var source=project.Source(project.Resolve("fonts.xml"));var node=source.Root.Children.FirstOrDefault(n=>n.Name=="alias"&&n.Value("name")==selected);Mode(node!=null);status.Text=node==null?"Existing font: all references and aliases using it will follow this change.":"Alias: choose its target font. Cycles are rejected.";
            if(node!=null)aliases.SelectedIndex=aliases.FindStringExact(node.Value("fontname"));SkinFontSpec spec;if(project.FontSpecs.TryGetValue(selected,out spec)){filter.Clear();FilterFamilies();if(!families.Items.Contains(spec.Family))families.Items.Add(spec.Family);families.SelectedIndex=families.FindStringExact(spec.Family);height.Value=Math.Max(1,Math.Min(96,(decimal)spec.Height));bold.Checked=(spec.Style&FontStyle.Bold)!=0;italic.Checked=(spec.Style&FontStyle.Italic)!=0;}
        }
        void New(bool alias){selecting=true;names.SelectedIndex=-1;selecting=false;name.ReadOnly=false;name.Text=alias?"NewAlias":"NewFont";Mode(alias);if(alias)aliases.SelectedIndex=0;status.Text="Creates a new definition. Choose its name in element properties to assign it.";}
    }
    public sealed partial class StudioForm
    {
        void EditFontDefinitions(){RequireAppliedProperties();using(var dialog=new FontDefinitionsDialog(project,selected==null?null:selected.Resolved("font"))){string key=selected==null?null:selected.Key;if(dialog.ShowDialog(this)==DialogResult.OK){canvas.ResetAssets();RefreshProject(key);ShowNotice("Font definitions applied. Undo restores fonts.xml; Save skin copy keeps the edited definitions.");}}}
    }
}
