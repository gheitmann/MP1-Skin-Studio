using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace MP1SkinStudio
{
    public sealed class TextEditInfo
    {
        public SkinControl Control;
        public string Raw,Origin,Reason;
        public bool Bound,LanguageId;
        public bool Allowed{get{return Reason==null;}}
        public bool NeedsApproval{get{return Bound||LanguageId;}}
    }
    public sealed partial class SkinProject
    {
        public TextEditInfo InspectTextEdit(SkinControl control)
        {
            RequireCurrent(control);int id;PropertyValue value;
            var info=new TextEditInfo{Control=control,Raw=control.Get("label"),Bound=control.IsBound("label"),LanguageId=Int32.TryParse(control.Get("label"),NumberStyles.Integer,CultureInfo.InvariantCulture,out id),Origin=control.Properties.TryGetValue("label",out value)?value.Origin:"Not set"};
            string type=control.Get("type");
            if(!(new[]{"label","fadelabel","textbox","textboxscrollup"}.Contains(type)||type.Contains("button")))info.Reason="This control has no editable primary text (label) here.";
            else if(IsLocked(control))info.Reason="The control or its group is locked. Unlock it first.";
            else if(control.Node.Children.Count(n=>n.Name=="label")>1)info.Reason="Multiple local label entries: resolve the XML mapping first.";
            else if(control.Node.Child("label")!=null&&control.Node.Child("label").Children.Count>0)info.Reason="Nested text properties remain protected.";
            else if(info.Raw.Length>32768)info.Reason="This text exceeds the compact editor limit of 32768 characters.";
            return info;
        }
        public bool SetControlText(SkinControl control,string text,bool replaceProtected)
        {
            var info=InspectTextEdit(control);if(!info.Allowed)throw new InvalidOperationException(info.Reason);
            if(text==null||text.Length>32768)throw new InvalidOperationException("Enter text with at most 32768 characters.");XmlConvert.VerifyXmlChars(text);
            if(text==info.Raw)return false;
            if(info.NeedsApproval&&!replaceProtected)throw new InvalidOperationException(info.Bound?"Data binding protected. Explicitly allow replacement.":"Numeric value protected as a possible language-resource ID. Explicitly allow replacement.");
            EditMany(new[]{new ControlEdit(control,new Dictionary<string,string>{{"label",text}})},"Edit text");return true;
        }
    }
}
