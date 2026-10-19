using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject CloneGroupFixture(string root,string name)
        {
            return AuditFixture(root,name,"<control custom=\"keep\"><type>group</type><id keep=\"yes\">10<!--identity--></id><description>Menu</description><posX>100.5</posX><posY>200</posY><layout>StackLayout(7, Vertical, true)</layout><defaultcontrol>11</defaultcontrol>"+
                "<!--keep group--><control Style=\"Button\"><type>button</type><id>11</id><description>First</description><posX>5</posX><onright>12</onright><onleft>99</onleft><visible>Control.HasFocus( 012 ) | control.isvisible(11)</visible><extension a=\"1\">keep</extension></control>"+
                "<control><type>group</type><id>0</id><description>Nested</description><control><type>button</type><id>12</id><description>Second</description><onleft>11</onleft><onright>12</onright><visible>Control.HasText(11)+Control.HasThumb(99)</visible></control>"+
                "<control><type>label</type><id>0</id><label>Unchanged</label></control></control></control><control><type>button</type><id>99</id><description>Outside</description></control>");
        }
        static void RunGroupDuplicationTests(string root)
        {
            Test("Group duplication remaps nested IDs, navigation and supported visibility references",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-links");var original=Named(p,"Menu");string raw=original.Source.Text.Substring(original.Node.Start,original.Node.End-original.Node.Start);
                string key=p.DuplicateControl(original,false);var clone=p.Controls.First(c=>c.Key==key);var nodes=p.Controls.Where(c=>c.Source==clone.Source&&c.Node.Start>=clone.Node.Start&&c.Node.End<=clone.Node.End).ToArray();
                Check(nodes.Length==5&&nodes.Select(c=>c.Get("id")).Distinct().Count()==5&&nodes.All(c=>Int32.Parse(c.Get("id"))>=10000),"Copied group IDs collide or children are missing.");
                var first=nodes.First(c=>c.Name=="First");var second=nodes.First(c=>c.Name=="Second");
                Check(clone.Get("defaultcontrol")==first.Get("id")&&first.Get("onright")==second.Get("id")&&first.Get("onleft")=="99"&&second.Get("onleft")==first.Get("id")&&second.Get("onright")==second.Get("id"),"Internal, external or self navigation changed incorrectly.");
                Check(first.Get("visible")=="Control.HasFocus( "+second.Get("id")+" ) | control.isvisible("+first.Get("id")+")"&&second.Get("visible")=="Control.HasText("+first.Get("id")+")+Control.HasThumb(99)","Visibility references did not follow clone IDs.");
                Check(clone.Get("posX")=="130.5"&&clone.Get("posY")=="230"&&first.Get("posX")=="5"&&clone.Get("layout")==original.Get("layout"),"Group offset changed local child geometry or layout.");
                string copied=clone.Source.Text.Substring(clone.Node.Start,clone.Node.End-clone.Node.Start);
                Check(original.Source.Text.Contains(raw)&&copied.Contains("<!--keep group-->")&&copied.Contains("<!--identity-->")&&copied.Contains("keep=\"yes\"")&&copied.Contains("<extension a=\"1\">keep</extension>")&&copied.Contains("Style=\"Button\""),"Original, comments, attributes or extensions changed.");
            });
            Test("Group duplication is one exact undo step with stable marker identities",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-history");p.Editor.Hidden.Add(Named(p,"Outside").Key);string before=ProjectFile.ContentSignature(p);string key=p.DuplicateControl(Named(p,"Menu"),false),after=ProjectFile.ContentSignature(p);
                Check(p.Undo.Count==1&&p.IsEditorHidden(Named(p,"Outside"))&&!p.IsEditorHidden(p.Controls.First(c=>c.Key==key)),"Group history or existing marker changed.");
                p.UndoChange();Check(ProjectFile.ContentSignature(p)==before&&!p.Dirty,"Group undo was not exact.");p.RedoChange();Check(ProjectFile.ContentSignature(p)==after&&p.Controls.Any(c=>c.Key==key),"Group redo changed identities.");
                p.Editor.Locked.Add(p.Controls.First(c=>c.Key==key).Key);p.UndoChange();Check(!p.Controls.Any(p.IsLocked),"Clone lock leaked into original after undo.");p.RedoChange();Check(p.IsLocked(p.Controls.First(c=>c.Key==key)),"Clone lock was lost after redo.");
            });
            Test("Duplicated groups survive project restore, export and restored structural undo",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-project");var source=p.Controls[0].Source;string original=source.Text,key=p.DuplicateControl(Named(p,"Menu"),false),changed=source.Text;
                string file=Path.Combine(root,"clone-group.mp1skinproj"),zip=Path.Combine(root,"clone-group.zip");ProjectFile.Save(p,file);p=ProjectFile.Load(file);
                Check(p.Controls.Any(c=>c.Key==key&&c.Name=="Menu (Copy)")&&p.Controls[0].Source.Text==changed,"Project lost duplicated subtree.");p.Export(zip);
                using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==changed,"Export lost cloned group XML.");
                p.UndoChange();Check(p.Controls[0].Source.Text==original&&!p.Dirty,"Restored group undo did not recover exact original.");p.RedoChange();Check(p.Controls[0].Source.Text==changed,"Restored group redo changed XML.");Check(File.ReadAllText(source.Path)==original,"Source file was overwritten.");
            });
            Test("Locked descendants and external group includes reject copying atomically",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-locked");p.Editor.Locked.Add(Named(p,"Second").Key);string signature=ProjectFile.ContentSignature(p);Reject(()=>p.DuplicateControl(Named(p,"Menu"),true));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==0,"Locked descendant was bypassed.");
                p=AuditFixture(root,"clone-group-include","<control><type>group</type><id>10</id><import>part.xml</import></control>");Write(p.Root,"part.xml","<window><controls/></window>");p.OpenPage("main.xml");signature=ProjectFile.ContentSignature(p);Reject(()=>p.DuplicateControl(p.Controls[0],true));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==0,"Rejected include copied partial XML.");
            });
            Test("Ambiguous IDs and dynamic navigation reject group copies without losing redo",()=>
            {
                foreach(string child in new[]{"<control><type>button</type><id>10</id></control>","<control><type>button</type><id>#id</id></control>","<control><type>button</type><id>11</id><onleft>#target</onleft></control>","<control><type>button</type><id>11</id><label>#(control.getlabel(10))</label></control>"})
                {
                    var p=AuditFixture(root,"clone-group-reject-"+Guid.NewGuid().ToString("N"),"<control><type>group</type><id>10</id>"+child+"</control>");p.Edit(p.Controls[0],Set("description","Changed"),"Earlier");p.UndoChange();string signature=ProjectFile.ContentSignature(p);Reject(()=>p.DuplicateControl(p.Controls[0],true));Check(ProjectFile.ContentSignature(p)==signature&&p.Redo.Count==1&&p.Undo.Count==0,"Rejected clone changed state or redo history.");
                }
            });
            Test("Group copy protects bound positions and offsets only the approved group frame",()=>
            {
                string folder=Path.Combine(root,"clone-group-bound");Directory.CreateDirectory(folder);Write(folder,"main.xml","<window><define>#x:100</define><controls><control><type>group</type><id>10</id><posX>#x</posX><control><type>label</type><id>0</id><posX>#x</posX></control></control></controls></window>");
                var p=new SkinProject(folder);p.OpenPage("main.xml");string signature=ProjectFile.ContentSignature(p);Reject(()=>p.DuplicateControl(p.Controls[0],false));Check(ProjectFile.ContentSignature(p)==signature,"Protected group position changed.");
                string key=p.DuplicateControl(p.Controls[0],true);var clone=p.Controls.First(c=>c.Key==key);Check(clone.Get("posX")=="130"&&clone.Node.Children.First(n=>n.Name=="control").Value("posX")=="#x","Approved group replacement changed child binding.");
            });
            Test("Group copies override inherited internal references without editing shared styles",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-inherited");string references="<controls><style Name=\"Button\"><ondown>12</ondown><visible>control.hasfocus(11)</visible></style></controls>";Write(p.Root,"references.xml",references);p=new SkinProject(p.Root);p.OpenPage("main.xml");
                p.ResetProperty(new[]{Named(p,"First")},"visible",false);
                string key=p.DuplicateControl(Named(p,"Menu"),false);var clone=p.Controls.First(c=>c.Key==key);var first=p.Controls.First(c=>c.Node.Start>clone.Node.Start&&c.Name=="First");var second=p.Controls.First(c=>c.Node.Start>clone.Node.Start&&c.Name=="Second");
                Check(first.Get("ondown")==second.Get("id")&&first.Properties["ondown"].Local&&File.ReadAllText(Path.Combine(p.Root,"references.xml"))==references,"Inherited navigation did not become a local clone reference.");
                Check(first.Get("visible")=="control.hasfocus("+first.Get("id")+")"&&first.Properties["visible"].Local,"Inherited visibility still refers to the original.");
                Check(Named(p,"First").Get("ondown")=="12"&&!Named(p,"First").Properties["ondown"].Local,"Original style inheritance was changed.");
            });
            Test("Unsupported internal XML attribute references reject group copies atomically",()=>
            {
                var p=AuditFixture(root,"clone-group-attribute","<control><type>group</type><id>10</id><visible condition=\"control.hasfocus(10)\">true</visible></control>");string signature=ProjectFile.ContentSignature(p);Reject(()=>p.DuplicateControl(p.Controls[0],false));Check(signature==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Unsupported attribute reference was silently retained.");
            });
            Test("ID allocation reserves unopened-page default and visibility reference targets",()=>
            {
                var p=CloneGroupFixture(root,"clone-group-reservations");Write(p.Root,"other.xml","<window><defaultcontrol>10000</defaultcontrol><controls><control><type>label</type><id>0</id><visible>Control.HasFocus(10001)</visible></control><include condition=\"control.isvisible(10002)\">absent.xml</include></controls></window>");
                string key=p.DuplicateControl(Named(p,"Menu"),false);Check(p.Controls.First(c=>c.Key==key).Get("id")=="10003","Default/visibility reference reservations were ignored.");
            });
        }
    }
}
