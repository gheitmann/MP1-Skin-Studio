using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;

namespace MP1SkinStudio
{
    public static class EffectEditing
    {
        public static string Read(SkinControl control)
        {
            var doc=Parse(control.EffectXml);if(control.Defines!=null)foreach(XmlElement effect in doc.DocumentElement.ChildNodes)foreach(XmlAttribute attribute in effect.Attributes){var seen=new System.Collections.Generic.HashSet<string>();string value=attribute.Value,next;while(control.Defines.TryGetValue(value,out next)&&seen.Add(value))value=next;attribute.Value=value;}return doc.OuterXml;
        }
        public static string ReadLocal(SkinControl control)
        {return "<effects>"+String.Concat(control.Node.Children.Where(n=>n.Name=="animation").Select(n=>n.Element.OuterXml))+"</effects>";}
        public static XmlDocument Parse(string text)
        {
            if(text==null||text.Length>16000)throw new InvalidDataException("Effects exceed the live size limit.");
            var doc=new XmlDocument{XmlResolver=null};using(var reader=XmlReader.Create(new StringReader(text),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=16000}))doc.Load(reader);
            if(doc.DocumentElement==null||doc.DocumentElement.Name!="effects"||doc.DocumentElement.Attributes.Count!=0||doc.DocumentElement.ChildNodes.Count>32||doc.DocumentElement.ChildNodes.Cast<XmlNode>().Any(n=>!(n is XmlElement)||n.Name!="animation"||n.ChildNodes.Cast<XmlNode>().Any(c=>c is XmlElement)))throw new InvalidDataException("Expected up to 32 animation effects.");
            return doc;
        }
        public static string Create(string effect,string trigger,int duration,int start,int end)
        {
            if(!new[]{"fade","zoom","rotate"}.Contains(effect)||!new[]{"WindowOpen","Visible","Focus","Unfocus"}.Contains(trigger)||duration<1||duration>60000||start<0||end<0||start>1000||end>1000)throw new InvalidDataException("Choose a valid effect, duration and start/end value.");
            if(effect=="fade"&&(start>100||end>100))throw new InvalidDataException("Fade values must be between 0 and 100 percent.");
            var doc=new XmlDocument();var node=doc.CreateElement("animation");doc.AppendChild(node);node.SetAttribute("effect",effect);node.SetAttribute("time",duration.ToString(CultureInfo.InvariantCulture));node.SetAttribute("start",start.ToString(CultureInfo.InvariantCulture));node.SetAttribute("end",end.ToString(CultureInfo.InvariantCulture));node.InnerText=trigger;return node.OuterXml;
        }
    }
    public sealed partial class SkinProject
    {
        public void SetEffects(SkinControl control,string xml)
        {
            RequireCurrent(control);if(IsLocked(control))throw new InvalidOperationException("Unlock this element before changing effects.");
            var effects=EffectEditing.Parse(xml);var nodes=control.Node.Children.Where(n=>n.Name=="animation").ToArray();string replacement=String.Concat(effects.DocumentElement.ChildNodes.Cast<XmlNode>().Select(n=>n.OuterXml));
            Transaction(new[]{control.Source},()=>
            {
                int insertion=nodes.Length==0?control.Node.CloseStart:nodes[0].Start;string text=control.Source.Text;
                foreach(var node in nodes.OrderByDescending(n=>n.Start))text=text.Remove(node.Start,node.End-node.Start);
                control.Source.Text=text.Insert(insertion,replacement);
            },"Edit element effects");
        }
        public void SetReferenceDefault(string type,string field,string value)
        {
            if(!new[]{"font","textcolor","textureCheckmark","textureCheckmarkNoFocus"}.Contains(field))throw new InvalidOperationException("This global default is not editable here.");
            if(field=="font"&&!FontNames.Contains(value))throw new InvalidOperationException("Choose an existing skin font.");
            if(field=="textcolor"){uint color;string digits=value.TrimStart('#');if((digits.Length!=6&&digits.Length!=8)||!UInt32.TryParse(digits,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out color))throw new InvalidOperationException("Enter a RRGGBB or AARRGGBB color.");}
            if(field.StartsWith("texture",StringComparison.Ordinal)&&value.Length>0&&value!="-")MP1SkinStudio.Live.LiveWire.SafeRelative(Root,"Media/"+value);
            var source=Source(Resolve("references.xml"));var matches=source.Root.Children.Where(n=>n.Name=="control"&&n.Value("type")==type).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Choose a type with one unambiguous references.xml default.");
            var previous=matches[0].Child(field);if(previous!=null&&(previous.Element.InnerText.Contains("#")||previous.Element.GetAttribute("property").Length>0))throw new InvalidOperationException("This shared default contains a protected binding. Edit an individual element explicitly instead.");
            if(Controls.Any(c=>c.Get("type")==type&&IsLocked(c)))throw new InvalidOperationException("Unlock affected elements before changing a shared default.");
            Transaction(new[]{source},()=>source.SetControlProperties(matches[0].ControlOrdinal,new System.Collections.Generic.Dictionary<string,string>{{field,value}}),"Edit shared "+type+" default");
        }
    }
    public sealed class EffectsDialog:Form
    {
        readonly ListBox list=new ListBox();readonly ChoiceBox effect=new ChoiceBox(),trigger=new ChoiceBox();readonly NumericUpDown duration=new NumericUpDown(),start=new NumericUpDown(),end=new NumericUpDown();readonly Label notice=new Label();
        public string Result {get;private set;}
        public EffectsDialog(SkinControl control)
        {
            Text="Element effects";ClientSize=new Size(670,440);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);Font=new Font("Segoe UI",9.5f);
            var description=new Label{Text="Local effects override shared animations. Removing all local effects restores shared defaults.\r\nMP previews supported effects; the Studio canvas stays static.",Bounds=new Rectangle(16,12,640,48)};Controls.Add(description);
            list.SetBounds(16,64,638,155);list.BackColor=Color.FromArgb(20,26,35);list.ForeColor=ForeColor;list.DisplayMember="Description";foreach(XmlElement node in EffectEditing.Parse(EffectEditing.ReadLocal(control)).DocumentElement.ChildNodes)list.Items.Add(new EffectItem{Xml=node.OuterXml,Description=node.GetAttribute("effect")+" · "+node.InnerText+" · "+node.GetAttribute("time")+" ms"});Controls.Add(list);
            effect.Items.AddRange(new object[]{"fade","zoom","rotate"});trigger.Items.AddRange(new object[]{"WindowOpen","Visible","Focus","Unfocus"});effect.SelectedIndex=trigger.SelectedIndex=0;effect.SetBounds(16,238,130,28);trigger.SetBounds(156,238,150,28);effect.AccessibleName="Animation effect";trigger.AccessibleName="Animation trigger";Controls.Add(effect);Controls.Add(trigger);
            duration.Minimum=1;duration.Maximum=60000;duration.Value=300;start.Maximum=end.Maximum=1000;end.Value=100;
            var inputs=new[]{duration,start,end};var names=new[]{"Duration (ms)","Start value","End value"};for(int i=0;i<inputs.Length;i++){int x=16+i*155;Controls.Add(new Label{Text=names[i],Bounds=new Rectangle(x,277,145,22)});inputs[i].SetBounds(x,302,145,28);inputs[i].BackColor=Color.FromArgb(20,26,35);inputs[i].ForeColor=ForeColor;inputs[i].AccessibleName=names[i];Controls.Add(inputs[i]);}
            var add=new Button{Text="Add effect",Bounds=new Rectangle(320,238,135,30)};add.Click+=(s,e)=>{try{string xml=EffectEditing.Create((effect.SelectedItem as string),(trigger.SelectedItem as string),(int)duration.Value,(int)start.Value,(int)end.Value);if(list.Items.Count>=32)throw new InvalidOperationException("Keep at most 32 effects.");list.Items.Add(new EffectItem{Xml=xml,Description=(effect.SelectedItem as string)+" · "+(trigger.SelectedItem as string)+" · "+duration.Value+" ms"});notice.Text="";}catch(Exception ex){notice.Text=ex.Message;}};Controls.Add(add);
            var remove=new Button{Text="Remove selected",Bounds=new Rectangle(465,238,189,30)};remove.Click+=(s,e)=>{if(list.SelectedIndex>=0)list.Items.RemoveAt(list.SelectedIndex);};Controls.Add(remove);
            notice.SetBounds(16,341,638,40);Controls.Add(notice);var apply=new Button{Text="Apply effects",Bounds=new Rectangle(374,390,135,32)};apply.Click+=(s,e)=>{Result="<effects>"+String.Concat(list.Items.Cast<EffectItem>().Select(i=>i.Xml))+"</effects>";DialogResult=DialogResult.OK;};Controls.Add(apply);var cancel=new Button{Text="Cancel",Bounds=new Rectangle(519,390,135,32),DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;
            foreach(var button in Controls.OfType<Button>()){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(35,48,64);button.ForeColor=ForeColor;}
        }
        sealed class EffectItem{public string Xml;public string Description{get;set;}}
    }
    public sealed partial class StudioForm
    {
        void EditElementEffects()
        {
            RequireAppliedProperties();if(selected==null||SelectedControls().Length!=1)throw new InvalidOperationException("Select one element for its effects.");
            using(var dialog=new EffectsDialog(selected))if(dialog.ShowDialog(this)==DialogResult.OK){string key=selected.Key;project.SetEffects(selected,dialog.Result);RefreshProject(key);ShowNotice("Effects applied. Undo restores the original XML. MP previews supported effects; the Studio canvas remains static.");}
        }
        void EditReferenceDefault()
        {
            RequireAppliedProperties();if(selected==null)throw new InvalidOperationException("Select an element type first.");
            using(var dialog=new Form{Text="Shared default — "+selected.Get("type"),ClientSize=new Size(610,250),StartPosition=FormStartPosition.CenterParent,BackColor=panel,ForeColor=ink,Font=Font})
            {
                var help=new Label{Text="Changes references.xml defaults for this element type. Local overrides stay intact.\r\nLive applies supported values on the connected page. Other pages update when linked.\r\nFont choice uses fonts already loaded by MP; this does not reload fonts.xml.",Bounds=new Rectangle(16,12,578,68)};dialog.Controls.Add(help);
                var field=new ChoiceBox{Bounds=new Rectangle(16,90,200,28)};field.Items.AddRange(new object[]{"font","textcolor","textureCheckmark","textureCheckmarkNoFocus"});field.SelectedIndex=0;dialog.Controls.Add(field);
                var value=new TextBox{Bounds=new Rectangle(228,90,366,28),Text=selected.Get("font"),Visible=false,BackColor=Color.FromArgb(20,26,35),ForeColor=ink};dialog.Controls.Add(value);
                var fonts=new ChoiceBox{Bounds=value.Bounds,AccessibleName="Shared default font"};fonts.Items.AddRange(project.FontNames.Cast<object>());fonts.SelectedIndex=Math.Max(0,project.FontNames.IndexOf(selected.Get("font")));dialog.Controls.Add(fonts);field.SelectedIndexChanged+=(s,e)=>{value.Text=selected.Get((field.SelectedItem as string));fonts.Visible=(field.SelectedItem as string)=="font";value.Visible=!fonts.Visible;};
                var status=new Label{Bounds=new Rectangle(16,130,578,60)};dialog.Controls.Add(status);
                var apply=new Button{Text="Apply shared default",Bounds=new Rectangle(280,200,185,32)};apply.Click+=(s,e)=>{try{string key=selected.Key;project.SetReferenceDefault(selected.Get("type"),(field.SelectedItem as string),(field.SelectedItem as string)=="font"?(fonts.SelectedItem as string):value.Text);RefreshProject(key);dialog.DialogResult=DialogResult.OK;}catch(Exception ex){status.Text=ex.Message;}};dialog.Controls.Add(apply);var cancel=new Button{Text="Cancel",Bounds=new Rectangle(475,200,119,32),DialogResult=DialogResult.Cancel};dialog.Controls.Add(cancel);dialog.CancelButton=cancel;dialog.ShowDialog(this);
            }
        }
    }
}
