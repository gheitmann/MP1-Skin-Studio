using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class TextEditDialog : Form
    {
        readonly TextEditInfo info;
        readonly PreviewState state;
        readonly TextBox editor,preview;
        readonly CheckBox approval;
        readonly Button accept;
        readonly Label warning;
        readonly string initialText;
        public string Result;
        public bool ReplaceProtected{get{return approval.Checked;}}
        public TextEditDialog(TextEditInfo value,PreviewState sample)
        {
            info=value;state=sample;Text="Edit Text - MP1 Skin Studio";Size=new Size(860,700);MinimumSize=new Size(720,680);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(29,35,46);ForeColor=Color.FromArgb(225,234,244);
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=59,Padding=new Padding(14)};
            accept=new Button{Text="Apply text",DialogResult=DialogResult.None,Width=155,Height=32,Dock=DockStyle.Right,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(30,113,170),ForeColor=ForeColor};accept.Click+=(s,e)=>AcceptText();bottom.Controls.Add(accept);
            var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Width=115,Height=32,Dock=DockStyle.Right,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor};bottom.Controls.Add(cancel);CancelButton=cancel;
            bottom.Controls.Add(new Label{Text="Ctrl+Enter: apply · Esc: cancel",Dock=DockStyle.Left,Width=300,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(149,168,189),Font=new Font("Segoe UI",9)});
            var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Padding=new Padding(18),BackColor=BackColor};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(float height in new[]{29,57,24,0,27,24,105,36,60})content.RowStyles.Add(new RowStyle(height==0?SizeType.Percent:SizeType.Absolute,height==0?100:height));
            content.Controls.Add(new Label{Text="ELEMENT PRIMARY TEXT",Font=new Font(Font,FontStyle.Bold),Dock=DockStyle.Fill},0,0);
            content.Controls.Add(new Label{Text=value.Control.Name+" · "+value.Control.Get("type")+" · ID "+value.Control.Get("id","-")+"\n"+value.Control.Source.RelativePath+" · Source: "+value.Origin,Dock=DockStyle.Fill,AutoEllipsis=true},0,1);
            var textTitle=new Panel{Dock=DockStyle.Fill};textTitle.Controls.Add(new Label{Text="Stored value (label)",Dock=DockStyle.Fill});
            var pickLanguage=new Button{Text="Choose Language Text …",Dock=DockStyle.Right,Width=170,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9),ForeColor=ForeColor,Enabled=state.Project.Editor.Language.Active};pickLanguage.Click+=(s,e)=>{using(var picker=new LanguageDialog(state.Project.Editor.Language,true))if(picker.ShowDialog(this)==DialogResult.OK&&picker.SelectedId.HasValue)editor.Text=picker.SelectedId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);};textTitle.Controls.Add(pickLanguage);content.Controls.Add(textTitle,0,2);
            editor=Box(false);editor.AccessibleName="Edit stored text";editor.MaxLength=32768;editor.Text=value.Raw;initialText=editor.Text;content.Controls.Add(editor,0,3);
            approval=new CheckBox{AutoSize=true,Dock=DockStyle.Fill,Text=value.Bound?"Intentionally replace the existing data binding with this local value":"Intentionally replace a numeric value / possible language-resource ID",Visible=value.NeedsApproval,ForeColor=Color.FromArgb(244,193,102)};content.Controls.Add(approval,0,4);
            content.Controls.Add(new Label{Text="Preview text with current sample data (approximate)",Dock=DockStyle.Fill},0,5);
            preview=Box(true);preview.AccessibleName="Text preview with sample data";content.Controls.Add(preview,0,6);
            warning=new Label{Dock=DockStyle.Fill,ForeColor=Color.FromArgb(244,193,102),AutoEllipsis=true};content.Controls.Add(warning,0,7);
            content.Controls.Add(new Label{Dock=DockStyle.Fill,Text="Changes the label in the selected XML control. Include changes affect every use. Loaded language text is used for previews. Original files remain unchanged.",Font=new Font("Segoe UI",9),ForeColor=Color.FromArgb(149,168,189)},0,8);
            Controls.Add(content);Controls.Add(bottom);editor.TextChanged+=(s,e)=>UpdateState();approval.CheckedChanged+=(s,e)=>UpdateState();UpdateState();
            Shown+=(s,e)=>{editor.Focus();editor.SelectAll();};
        }
        TextBox Box(bool readOnly){return new TextBox{Dock=DockStyle.Fill,Multiline=true,AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,ReadOnly=readOnly,BackColor=Color.FromArgb(20,26,35),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};}
        string Candidate{get{return editor.Text==initialText?info.Raw:editor.Text;}}
        void UpdateState()
        {
            string issue=info.Reason;
            try{if(Candidate.Length>32768)issue="Enter at most 32768 characters.";else XmlConvert.VerifyXmlChars(Candidate);}catch(XmlException){issue="Text contains an invalid XML character.";}
            if(issue==null&&info.NeedsApproval&&Candidate!=info.Raw&&!approval.Checked)issue=info.Bound?"Check the box before replacing the existing binding.":"Check the box before replacing the numeric value.";
            warning.Text=issue??(Candidate==info.Raw?"Value unchanged.":"Applying will set a local text property.");accept.Enabled=issue==null;preview.Text=state.Label(info.Control,Candidate);
        }
        void AcceptText(){UpdateState();if(!accept.Enabled)return;Result=Candidate;DialogResult=DialogResult.OK;Close();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {if(keyData==(Keys.Control|Keys.Enter)){AcceptText();return true;}return base.ProcessCmdKey(ref msg,keyData);}
        internal void CommitShortcutForTest(){var message=new Message();ProcessCmdKey(ref message,Keys.Control|Keys.Enter);}
        internal void TextForTest(string text){editor.Text=text;}
        internal void PickLanguageForTest(){foreach(Control control in Controls)foreach(var panel in Walk(control)){var button=panel as Button;if(button!=null&&button.Text.StartsWith("Choose language text",StringComparison.OrdinalIgnoreCase)){button.PerformClick();return;}}throw new InvalidOperationException("Language text selection is missing.");}
        static System.Collections.Generic.IEnumerable<Control> Walk(Control control){yield return control;foreach(Control child in control.Controls)foreach(var nested in Walk(child))yield return nested;}
        internal void ApproveForTest(){approval.Checked=true;}
        internal bool CanAccept{get{return accept.Enabled;}}
        internal string PreviewForTest{get{return preview.Text;}}
        internal void AcceptForTest(){AcceptText();}
        public void RenderTo(string path){Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
    }
}
