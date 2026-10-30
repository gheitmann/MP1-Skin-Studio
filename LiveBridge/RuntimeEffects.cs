using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using MediaPortal.GUI.Library;

namespace MP1SkinStudio.LiveBridge
{
    public sealed partial class RuntimeProperty
    {
        static bool EffectAdapter(Type type){var method=type.GetMethod("SetAnimations");return type.Assembly==typeof(GUIControl).Assembly&&method!=null&&method.DeclaringType==typeof(GUIControl);}
        sealed class EffectState {public List<VisualEffect> Effects;public bool Replay;}
        static RuntimeProperty Effects(GUIControl control)
        {
            var field=Field(typeof(GUIControl),"_animations");
            return new RuntimeProperty
            {
                Read=()=>new EffectState{Effects=(List<VisualEffect>)field.GetValue(control)},Format=value=>((EffectState)value).Effects.Count.ToString(CultureInfo.InvariantCulture)+" native effects",
                Parse=text=>
                {
                    if(text==null||text.Length>16000)throw new InvalidDataException("Effects exceed their live limit.");var doc=new XmlDocument{XmlResolver=null};using(var reader=XmlReader.Create(new StringReader(text),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=16000}))doc.Load(reader);
                    if(doc.DocumentElement==null||doc.DocumentElement.Name!="effects"||doc.DocumentElement.Attributes.Count>0||doc.DocumentElement.ChildNodes.Count>32)throw new InvalidDataException("Expected a bounded effects list.");
                    var list=new List<VisualEffect>();CultureInfo culture=Thread.CurrentThread.CurrentCulture;
                    try{foreach(XmlNode node in doc.DocumentElement.ChildNodes){if(!(node is XmlElement)||node.Name!="animation"||node.ChildNodes.Cast<XmlNode>().Any(n=>n is XmlElement))throw new InvalidDataException("Invalid live animation node.");var effect=new VisualEffect();if(!effect.Create(node))throw new InvalidDataException("MP cannot parse this effect.");if(effect.AnimationType==AnimationType.VisibleChange){effect.AnimationType=AnimationType.Visible;var reverse=(VisualEffect)effect.CloneReverse();reverse.AnimationType=AnimationType.Hidden;list.Add(reverse);}list.Add(effect);}}
                    finally{Thread.CurrentThread.CurrentCulture=culture;}return new EffectState{Effects=list,Replay=true};
                },
                Write=value=>{var state=(EffectState)value;field.SetValue(control,state.Effects);if(state.Replay){control.QueueAnimation(AnimationType.WindowOpen);control.QueueAnimation(control.Visible?AnimationType.Visible:AnimationType.Hidden);if(control.Focus)control.QueueAnimation(AnimationType.Focus);}}
            };
        }
    }
}
