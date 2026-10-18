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
            var info=new TextEditInfo{Control=control,Raw=control.Get("label"),Bound=control.IsBound("label"),LanguageId=Int32.TryParse(control.Get("label"),NumberStyles.Integer,CultureInfo.InvariantCulture,out id),Origin=control.Properties.TryGetValue("label",out value)?value.Origin:"Nicht gesetzt"};
            string type=control.Get("type");
            if(!(new[]{"label","fadelabel","textbox","textboxscrollup"}.Contains(type)||type.Contains("button")))info.Reason="Dieses Control unterstützt hier keinen bearbeitbaren Haupttext (label).";
            else if(IsLocked(control))info.Reason="Das Control oder seine Gruppe ist gesperrt. Zuerst entsperren.";
            else if(control.Node.Children.Count(n=>n.Name=="label")>1)info.Reason="Mehrere lokale label-Einträge: XML-Zuordnung zuerst klären.";
            else if(control.Node.Child("label")!=null&&control.Node.Child("label").Children.Count>0)info.Reason="Verschachtelte Text-Eigenschaft bleibt geschützt.";
            else if(info.Raw.Length>32768)info.Reason="Dieser Text ist für den kompakten Texteditor zu lang (höchstens 32768 Zeichen).";
            return info;
        }
        public bool SetControlText(SkinControl control,string text,bool replaceProtected)
        {
            var info=InspectTextEdit(control);if(!info.Allowed)throw new InvalidOperationException(info.Reason);
            if(text==null||text.Length>32768)throw new InvalidOperationException("Text mit höchstens 32768 Zeichen eingeben.");XmlConvert.VerifyXmlChars(text);
            if(text==info.Raw)return false;
            if(info.NeedsApproval&&!replaceProtected)throw new InvalidOperationException(info.Bound?"Datenbindung geschützt. Ersetzen ausdrücklich freigeben.":"Numerischer Wert als mögliche Sprachressourcen-ID geschützt. Ersetzen ausdrücklich freigeben.");
            EditMany(new[]{new ControlEdit(control,new Dictionary<string,string>{{"label",text}})},"Text bearbeiten");return true;
        }
    }
}
