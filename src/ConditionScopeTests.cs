using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ScopeFixture(string root,string name,string main,string part,string alternate=null)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);Write(folder,"main.xml",main);Write(folder,"part.xml",part);if(alternate!=null)Write(folder,"alternate.xml",alternate);var p=new SkinProject(folder);p.OpenPage("main.xml");return p;
        }
        static void RunConditionScopeTests(string root)
        {
            Test("Define-only includes load values without false missing-controls warnings",()=>
            {var p=ScopeFixture(root,"scope-defines","<window><controls><import>part.xml</import><control><type>label</type><id>1</id><visible>string.equals(#flag,Yes)</visible></control></controls></window>","<window><define>#flag:Yes</define></window>");Check(new PreviewState(p).Visible(p.Controls[0])==true&&!p.Warnings.Any(w=>w.Contains("No &lt;controls&gt;")||w.Contains("No <controls>")),"Define include was evaluated incorrectly.");});
            Test("Group conditions retain their context despite defines in child includes",()=>
            {var p=ScopeFixture(root,"scope-parent","<window><define>#flag:Yes</define><controls><control><type>group</type><id>1</id><visible>string.equals(#flag,Yes)</visible><include>part.xml</include></control></controls></window>","<window><define>#flag:No</define><controls><control><type>button</type><id>8</id><visible>string.equals(#flag,No)</visible></control></controls></window>");var child=p.Controls.First(c=>c.Get("id")=="8");Check(child.RuntimeParent!=null&&new PreviewState(p).Visible(child)==true,"Parent condition used child context or a group-only include was missed.");});
            Test("Include and otherwise branches use the condition context captured before loading",()=>
            {var p=ScopeFixture(root,"scope-otherwise","<window><define>#flag:Yes</define><controls><include condition='string.equals(#flag,Yes)' otherwise='alternate.xml'>part.xml</include></controls></window>","<window><define>#flag:No</define><controls><control><type>label</type><id>8</id></control></controls></window>","<window><controls><control><type>label</type><id>9</id></control></controls></window>");var state=new PreviewState(p);Check(state.Visible(p.Controls.First(c=>c.Get("id")=="8"))==true&&state.Visible(p.Controls.First(c=>c.Get("id")=="9"))==false,"Otherwise uses a modified context.");});
            Test("String equality respects quoted commas and unknown values",()=>
            {var p=Demo();var s=new PreviewState(p);Check(s.Evaluate("string.equals('A,B','A,B')",new HashSet<string>())==true&&s.Evaluate("string.equals(#notKnown,'')",new HashSet<string>())==null,"Comparison was parsed incorrectly or an unknown value was guessed.");});
            Test("String searches honor scoped defines and preserve unresolved operands",()=>
            {var p=ScopeFixture(root,"scope-string-search","<window><define>#flag: Movie </define><controls><include condition='string.contains(#flag,ovi)'>part.xml</include></controls></window>","<window><define>#flag:Other</define><controls><control><type>label</type><id>8</id><visible>string.starts(#flag,oth)</visible></control></controls></window>");var s=new PreviewState(p);Check(s.Visible(p.Controls.Single())==true,"Search conditions lost include/child define scope.");Check(s.Evaluate("string.contains('A,B','a,b')",new HashSet<string>())==true&&s.Evaluate("string.equals(' A ','a')",new HashSet<string>())==true,"Quoted commas or trimmed equality failed.");Check(s.Evaluate("string.contains('Text','')",new HashSet<string>())==false&&s.Evaluate("string.starts('','')",new HashSet<string>())==true,"Empty searches diverge from native equality semantics.");Check(s.Evaluate("!string.contains(#unknown,yes)",new HashSet<string>())==null&&s.Evaluate("string.starts(a,b,c)",new HashSet<string>())==null,"Missing data or malformed arguments were guessed.");});
            Test("Boolean define chains are resolved and cycles remain unknown",()=>
            {var p=ScopeFixture(root,"scope-cycle","<window><define>#a:#b</define><define>#b:#a</define><define>#good:Yes</define><controls><control><type>label</type><id>1</id><visible>#a</visible></control><control><type>label</type><id>2</id><visible>#good</visible></control></controls></window>","<window/>");var s=new PreviewState(p);Check(s.Visible(p.Controls[0])==null&&s.Visible(p.Controls[1])==true,"Incorrect define chain or cycle.");});
            Test("Control.IsVisible evaluates targets in their own define context",()=>
            {var p=ScopeFixture(root,"scope-target","<window><define>#flag:Outer</define><controls><control><type>label</type><id>1</id><visible>control.isvisible(8)</visible></control><import>part.xml</import></controls></window>","<window><define>#flag:Inner</define><controls><control><type>label</type><id>8</id><visible>string.equals(#flag,Inner)</visible></control></controls></window>");Check(new PreviewState(p).Visible(p.Controls[0])==true,"Target condition uses the caller context.");});
            Test("Group locks and hiding also affect included children",()=>
            {var p=ScopeFixture(root,"scope-flags","<window><controls><control><type>group</type><id>1</id><include>part.xml</include></control><import>part.xml</import></controls></window>","<window><controls><control><type>button</type><id>8</id><label>Kind</label></control></controls></window>");var group=p.Controls.First(c=>c.Get("id")=="1");var children=p.Controls.Where(c=>c.Get("id")=="8").ToArray();p.Editor.Hidden.Add(group.Key);Check(p.IsEditorHidden(children[0])&&!p.IsEditorHidden(children[1]),"Hiding affects the wrong include occurrence.");p.Editor.Locked.Add(group.Key);Check(p.IsLocked(children[0])&&p.IsLocked(children[1]),"Shared source bypasses protected use.");Reject(()=>p.Edit(children[1],Set("label","Neu"),"Change"));});
            Test("Locked included children prevent removal of their containing group",()=>
            {var p=ScopeFixture(root,"scope-delete-lock","<window><controls><control><type>group</type><id>1</id><include>part.xml</include></control></controls></window>","<window><controls><control><type>button</type><id>8</id></control></controls></window>");p.Editor.Locked.Add(p.Controls.First(c=>c.Get("id")=="8").Key);Reject(()=>p.DeleteControls(new[]{p.Controls[0]}));Check(!p.Dirty,"Protected group was modified.");});
            Test("Window default focus loads independently of label selection",()=>
            {var p=Demo();p.OpenPage("demo.language.xml");var s=new PreviewState(p);Check(p.DefaultControlId==501&&s.FocusId==501&&s.Evaluate("control.hasfocus(501)",new HashSet<string>())==true,"Default focus is missing.");});
            Test("Parentheses inside quoted comparison values do not change condition structure",()=>
            {var state=new PreviewState(Demo());Check(state.Evaluate("[string.equals(']','x')]+true",new HashSet<string>())==false,"Quoted parenthesis was incorrectly parsed as an expression boundary.");});
            Test("A dash texture placeholder is not reported as a missing image",()=>
            {var p=AuditFixture(root,"texture-sentinel","<control><type>button</type><id>1</id><textureFocus>-</textureFocus><textureNoFocus>-</textureNoFocus><label>Test</label></control>");Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="TEXTURE_MISSING"),"Intentionally empty texture was reported as an error.");});
        }
    }
}
