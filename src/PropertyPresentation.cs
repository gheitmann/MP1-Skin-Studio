using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static class PropertyPresentation
    {
        public static readonly string[] Groups={"Position and size","Text","Images and colors","Collection layout","Visibility and state","Navigation"};
        public static string Group(string key)
        {
            if(new[]{"posX","posY","width","height"}.Contains(key))return Groups[0];
            if(key.StartsWith("on",StringComparison.Ordinal)||key=="hyperlink")return Groups[5];
            if(key=="visible"||key=="selected")return Groups[4];
            if(CollectionProperties.Find(key)!=null)return Groups[3];
            if(key.StartsWith("texture",StringComparison.Ordinal)||new[]{"colordiffuse","keepaspectratio","zoom","zoomfromtop","markWidth","markHeight"}.Contains(key))return Groups[2];
            return Groups[1];
        }
        public static string Help(string key)
        {
            switch(key)
            {
                case "posX":return "Horizontal position in skin pixels, relative to the parent group. Automatic layouts may control this value.";
                case "posY":return "Vertical position in skin pixels, relative to the parent group. Automatic layouts may control this value.";
                case "width":case "height":return "Field size in skin pixels, independent of preview zoom. A single-line text outline follows its caption.";
                case "label":return "Enter literal text, a language text ID, or a data binding. Double-click text in the preview for the guided text editor.";
                case "font":return "Choose a skin font with sample text and size information. This changes the selected element, not the shared font definition.";
                case "textcolor":case "disabledcolor":case "colordiffuse":return "Eight hexadecimal digits: AA transparency followed by RR GG BB. FF is opaque, 00 is transparent. The color picker preserves AA.";
                case "visible":return "A condition controls when the element appears. Empty adds no restriction; parent and include conditions still apply. Unknown expressions are preserved.";
                case "selected":return "A true/false expression controls the checkmark. Preview focus does not toggle it.";
                case "hyperlink":return "Window ID of the destination page, not a filename. Used when this control is activated in MediaPortal.";
                case "keepaspectratio":return "Keep the original image proportions instead of stretching it to the field.";
                case "zoom":return "Crop the image to fill the field when Keep image proportions is enabled.";
                case "zoomfromtop":return "Anchor the crop at the top. Takes precedence over centered cropping.";
                case "textXOff":case "textYOff":return "Inset of the text inside a button, in skin pixels.";
                case "align":case "valign":return "Alignment of the text or image inside its field. Labels without dimensions use their position as an anchor.";
                case "textalign":case "textvalign":return "Alignment of the caption inside a button, after its text offsets.";
                case "markWidth":case "markHeight":return "Size of the checkmark image in skin pixels, separate from the caption.";
            }
            if(key.StartsWith("texture",StringComparison.Ordinal))return "Image path relative to the skin Media directory. Use the picker to browse thumbnails without editing XML.";
            if(key.StartsWith("on",StringComparison.Ordinal))return "Control ID to receive focus in this direction. This is a control on the page, not a window ID.";
            if(CollectionProperties.Find(key)!=null)return "Collection item geometry in skin pixels. Applies to this collection template; preview zoom does not change the stored value.";
            return "Edit the selected element. Apply Properties commits the value as one undoable change.";
        }
    }
    public sealed partial class StudioForm
    {
        readonly Label propertyHelp=new Label();
        readonly HashSet<string> collapsedPropertyGroups=new HashSet<string>();
        Button AddPropertyGroup(string group)
        {
            var header=new Button{Width=251,Height=31,TextAlign=ContentAlignment.MiddleLeft,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(39,49,63),ForeColor=ink,Margin=new Padding(0,7,0,5),Tag="group:"+group,AccessibleName=group+" properties"};
            header.FlatAppearance.BorderColor=Color.FromArgb(63,78,96);
            Action update=()=>{header.Text=(collapsedPropertyGroups.Contains(group)?"+  ":"−  ")+group;tips.SetToolTip(header,"Show or hide this group. Unapplied inputs are retained.");};
            header.Click+=(s,e)=>{if(!collapsedPropertyGroups.Add(group))collapsedPropertyGroups.Remove(group);properties.SuspendLayout();foreach(Control row in properties.Controls)if(Convert.ToString(row.Tag)=="property-group:"+group)row.Visible=!collapsedPropertyGroups.Contains(group);update();properties.ResumeLayout(true);};
            update();properties.Controls.Add(header);return header;
        }
    }
}
