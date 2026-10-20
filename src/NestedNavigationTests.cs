using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string NestedButton(int id,string extra="")
        {return "<control><type>button</type><id>"+id+"</id><width>100</width><height>50</height>"+extra+"</control>";}
        static SkinProject NestedNavigation(string root,string name,string leftExtra="",string middle="")
        {
            return AuditFixture(root,name,"<control><type>group</type><id>90</id><posX>100</posX><posY>200</posY><layout>StackLayout(30,Horizontal)</layout>"+
                "<control><type>group</type><id>91</id><layout>GridLayout(1,0,0,20)</layout>"+leftExtra+NestedButton(10)+NestedButton(11)+"</control>"+middle+
                "<control><type>group</type><id>92</id><layout>StackLayout(20)</layout>"+NestedButton(12)+NestedButton(13)+"</control></control>");
        }
        static void RunNestedNavigationTests(string root)
        {
            Test("Nested grid and stack branches navigate across all four directions",()=>
            {
                var p=NestedNavigation(root,"nested-navigation-route");var s=new PreviewState(p){FocusId=10};string xml=ProjectFile.XmlSignature(p);
                GridMove(p,s,"onright",12);GridMove(p,s,"ondown",13);GridMove(p,s,"onleft",11);GridMove(p,s,"onup",10);
                Check(ProjectFile.XmlSignature(p)==xml&&p.Undo.Count==0&&!p.Dirty,"Nested navigation generated XML/history.");
            });
            Test("Top-level stack buttons can enter and leave a nested grid",()=>
            {
                var p=AuditFixture(root,"nested-navigation-enter","<control><type>group</type><layout>StackLayout(20)</layout>"+NestedButton(10)+"<control><type>group</type><layout>GridLayout(2,0,30,20)</layout>"+NestedButton(11)+NestedButton(12)+"</control></control>");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"ondown",11);GridMove(p,s,"onright",12);GridMove(p,s,"onleft",11);GridMove(p,s,"onup",10);
            });
            Test("Hidden and collapsed outer groups remove all descendant targets",()=>
            {
                foreach(string visibility in new[]{"Hidden","Collapsed"})
                {
                    var p=NestedNavigation(root,"nested-navigation-hidden-"+visibility);p.Edit(p.Controls.First(c=>c.Get("id")=="92"),Set("Visibility",visibility),"Hide branch");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",10);
                    p.Editor.Hidden.Add(p.Controls.First(c=>c.Get("id")=="91").Key);s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10,"Hidden-source rejection leaked focus.");
                }
            });
            Test("Unverified nested visibility blocks routes without leaking prospective focus",()=>
            {
                var p=NestedNavigation(root,"nested-navigation-unknown");p.Edit(p.Controls.First(c=>c.Get("id")=="92"),Set("visible","unknown.condition()"),"Unknown branch");var s=new PreviewState(p){FocusId=10};string signature=ProjectFile.ContentSignature(p);Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&ProjectFile.ContentSignature(p)==signature,"Unknown nested branch mutated state.");
            });
            Test("Unsupported layout wrappers stay outside nested navigation",()=>
            {
                foreach(string layout in new[]{"UnsupportedLayout(2)","GridLayout(0,0)"})
                {var p=NestedNavigation(root,"nested-navigation-layout-"+Guid.NewGuid().ToString("N"));p.Edit(p.Controls[0],Set("layout",layout),"Unsupported wrapper");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10,"Invalid wrapper leaked focus.");}
            });
            Test("Nested navigation validates geometry on nonfocusable measurement siblings",()=>
            {
                var p=NestedNavigation(root,"nested-navigation-measure",middle:"<control><type>label</type><width>#missing</width><height>50</height><label>Unknown size</label></control>");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&!p.Dirty,"Unknown measurement was guessed.");
            });
            Test("Nested layout depth is bounded without changing accepted shallow routes",()=>
            {
                foreach(int depth in new[]{16,17})
                {
                    string xml=String.Concat(Enumerable.Repeat("<control><type>group</type><layout>StackLayout(5)</layout>",depth))+NestedButton(10)+NestedButton(11)+String.Concat(Enumerable.Repeat("</control>",depth));
                    var p=AuditFixture(root,"nested-navigation-depth-"+depth,xml);var s=new PreviewState(p){FocusId=10};
                    if(depth==16)GridMove(p,s,"ondown",11);else Reject(()=>p.PreviewNavigationTarget(s,"ondown"));Check(!p.Dirty&&p.Undo.Count==0,"Depth handling mutated XML/history.");
                }
            });
            Test("Explicit mappings inside nested branches retain priority and ambiguity guards",()=>
            {
                var p=NestedNavigation(root,"nested-navigation-explicit");p.Edit(p.Controls.First(c=>c.Get("id")=="10"),Set("onright","13"),"Explicit route");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",13);
                p.Edit(p.Controls.First(c=>c.Get("id")=="10"),Set("onright","999"),"Missing route");s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));
            });
            Test("Nested layout edits preserve saved focus and exact XML export with undo redo",()=>
            {
                var p=NestedNavigation(root,"nested-navigation-save");var original=File.ReadAllBytes(p.Controls[0].Source.Path);string before=p.Controls[0].Source.Text;var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",12);GridMove(p,s,"ondown",13);
                p.Edit(p.Controls.First(c=>c.Get("id")=="91"),Set("Margin","10,20,10,20"),"Nested margin");string after=p.Controls[0].Source.Text,file=Path.Combine(root,"nested-navigation.mp1skinproj"),zip=Path.Combine(root,"nested-navigation.zip");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);Check(new PreviewState(q).FocusId==13&&ProjectFile.ContentSignature(q)==ProjectFile.ContentSignature(p),"Nested saved state changed.");q.Export(zip);
                using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after,"Nested export changed XML.");
                q.UndoChange();Check(q.Controls[0].Source.Text==before,"Restored nested undo failed.");q.RedoChange();Check(q.Controls[0].Source.Text==after&&File.ReadAllBytes(q.Controls[0].Source.Path).SequenceEqual(original),"Nested redo changed originals.");
            });
        }
    }
}
