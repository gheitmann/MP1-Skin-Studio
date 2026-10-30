using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunSpecialLiveTests(string root)
        {
            string folder=Path.Combine(root,"special-live");Directory.CreateDirectory(folder);
            Write(folder,"references.xml","<controls><!-- keep shared note --><control><type>label</type><font>font10</font><textcolor>ffffffff</textcolor><animation effect='fade' time='100' start='0' end='100'>Visible</animation></control></controls>");
            Write(folder,"fonts.xml","<fonts><font><name>font10</name><height>10</height></font><font><name>font18</name><height>18</height></font></fonts>");
            Write(folder,"main.xml","<window><id>0</id><controls><!-- keep page note --><control><type>label</type><id>1</id><description>Inherited</description><label>One</label></control><control><type>label</type><id>2</id><font>font18</font><label>Two</label><animation effect='zoom' time='200' start='80' end='100'>Focus</animation></control></controls></window>");
            Func<SkinProject> open=()=>{var p=new SkinProject(folder);p.OpenPage("main.xml");return p;};
            Test("Effects inherit references and local XML overrides them",()=>{var p=open();Check(EffectEditing.Read(p.Controls[0]).Contains("fade")&&EffectEditing.Read(p.Controls[1]).Contains("zoom")&&!EffectEditing.Read(p.Controls[1]).Contains("fade"),"Effective effect precedence changed.");});
            Test("Effect edits preserve comments and undo exact source bytes",()=>{var p=open();var c=p.Controls[0];string before=c.Source.Text;p.SetEffects(c,"<effects>"+EffectEditing.Create("fade","WindowOpen",300,0,100)+"</effects>");Check(p.Controls[0].Source.Text.Contains("keep page note")&&p.Undo.Count==1,"Effect edit lost comments/history.");p.UndoChange();Check(p.Controls[0].Source.Text==before,"Effect undo differs.");p.RedoChange();Check(EffectEditing.Read(p.Controls[0]).Contains("300"),"Effect redo lost duration.");});
            Test("Removing local effects restores shared effect definitions",()=>{var p=open();p.SetEffects(p.Controls[1],"<effects/>");Check(EffectEditing.Read(p.Controls[1]).Contains("fade")&&!EffectEditing.Read(p.Controls[1]).Contains("zoom"),"Empty local list does not inherit.");});
            Test("Effects become a native live frame operation with attributes",()=>{var baseline=open();var p=open();var binding=new LiveBinding(baseline,LiveSnapshot(baseline));p.SetEffects(p.Controls[0],"<effects>"+EffectEditing.Create("rotate","Focus",250,0,90)+"</effects>");var frame=binding.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1);var node=frame.SelectSingleNode("/live/control/set[@key='animations']");Check(node!=null&&node.InnerText.Contains("effect=\"rotate\"")&&node.InnerText.Contains("250"),"Live frame lost effect attributes.");});
            Test("Shared font defaults update inherited fields and retain local overrides",()=>{var baseline=open();var p=open();var binding=new LiveBinding(baseline,LiveSnapshot(baseline));p.SetReferenceDefault("label","font","font18");Check(p.Controls.All(c=>c.Get("font")=="font18"),"Shared font not updated.");Check(binding.Frame(p,new Dictionary<string,Dictionary<string,string>>(),1).SelectNodes("/live/control/set[@key='font']").Count==1,"Local font override was overwritten.");Check(p.Sources.Values.Single(s=>s.RelativePath=="references.xml").Text.Contains("keep shared note"),"Shared comment lost.");p.UndoChange();Check(p.Controls[0].Get("font")=="font10"&&p.Controls[1].Get("font")=="font18","Shared font undo failed.");});
            Test("Invalid effect XML and invalid shared values leave history intact",()=>{var p=open();string before=p.Controls[0].Source.Text;Reject(()=>p.SetEffects(p.Controls[0],"<!DOCTYPE effects [<!ENTITY bad SYSTEM 'file:///invalid'>]><effects>&bad;</effects>"));Reject(()=>p.SetReferenceDefault("label","font","not-loaded"));Reject(()=>p.SetReferenceDefault("label","textcolor","not-color"));Check(p.Undo.Count==0&&p.Controls[0].Source.Text==before,"Rejected edit changed history.");});
            Test("Locked elements prevent shared defaults and effect changes",()=>{var p=open();p.Editor.Locked.Add(p.Controls[0].Key);Reject(()=>p.SetEffects(p.Controls[0],"<effects/>"));Reject(()=>p.SetReferenceDefault("label","textcolor","ff00ff00"));Check(p.Undo.Count==0,"Locked edit changed history.");});
        }
    }
}
