using System;
using System.Linq;
using System.Collections.Generic;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunVisibilityPassTests(string root)
        {
            Test("Visibility passes preserve unknown cycles and order-independent root results",()=>
            {
                var p=AuditFixture(root,"visibility-pass-cycle","<control><type>label</type><id>1</id><visible>control.isvisible(2)</visible></control><control><type>label</type><id>2</id><visible>control.isvisible(1)</visible></control><control><type>label</type><id>3</id><visible>true</visible></control><control><type>label</type><id>4</id><visible>false</visible></control>");
                var state=new PreviewState(p);var expected=p.Controls.Select(state.Visible).ToArray();
                using(state.BeginVisibilityPass()){foreach(var c in p.Controls.AsEnumerable().Reverse())Check(state.Visible(c)==expected[p.Controls.IndexOf(c)],"Pass changed recursive visibility.");using(state.BeginVisibilityPass())foreach(var c in p.Controls)Check(state.Visible(c)==expected[p.Controls.IndexOf(c)],"Nested pass changed visibility.");}
                Check(expected[0]==null&&expected[1]==null&&expected[2]==true&&expected[3]==false,"Cycle fixture no longer exercises unknown results.");
            });
            Test("Visibility scopes release on exceptions and observe subsequent sample focus and XML edits",()=>
            {
                var p=AuditFixture(root,"visibility-pass-fresh","<control><type>button</type><id>1</id><visible>control.hasfocus(1)+#flag</visible></control>");var s=new PreviewState(p){FocusId=1};s.Values["#flag"]="true";
                try{using(s.BeginVisibilityPass()){Check(s.Visible(p.Controls[0])==true,"Initial visibility failed.");throw new InvalidOperationException("Probe");}}catch(InvalidOperationException ex){if(ex.Message!="Probe")throw;}
                s.Values["#flag"]="false";Check(s.Visible(p.Controls[0])==false,"Exception leaked cached sample.");s.Values["#flag"]="true";s.FocusId=2;Check(s.Visible(p.Controls[0])==false,"Focus retained stale visibility.");s.FocusId=1;
                p.Edit(p.Controls[0],Set("visible","false"),"Visibility probe");using(s.BeginVisibilityPass())Check(s.Visible(p.Controls[0])==false,"XML edit was stale.");p.UndoChange();using(s.BeginVisibilityPass())Check(s.Visible(p.Controls[0])==true,"Undo was stale.");
            });
            Test("Position-bounded source scanning preserves comments CDATA PIs and final empty tags",()=>
            {
                string text="<?xml version='1.0'?><window><!-- <fake/> --><controls><control test='a>b'><type>label</type><?probe value?><label><![CDATA[<fake><!--text--></fake>]]></label><visible>true</visible></control><control><type>image</type><texture/></control></controls></window>";
                var source=SkinSource.FromText(text,"scanner.xml");Check(source.Elements.Count==9&&source.Elements.All(e=>text.Substring(e.Start,e.OpenEnd-e.Start).StartsWith("<"))&&source.Root.End==text.Length,"Source spans changed.");source.SetControlProperties(0,Set("label","Edited"));Check(source.Text.Contains("<!-- <fake/> -->")&&source.Text.Contains("<?probe value?>")&&source.Text.Contains("<![CDATA[Edited]]>")&&source.Elements.Last().Empty,"Scalar edit lost protected XML.");
            });
        }
    }
}
