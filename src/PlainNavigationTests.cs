using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string PlainButton(int id,int x,int y)
        {return NestedButton(id,"<posX>"+x+"</posX><posY>"+y+"</posY>");}
        static SkinProject PlainScene(string root,string name,string groupExtra="")
        {return AuditFixture(root,name,"<control><type>group</type><id>90</id><posX>900</posX><posY>700</posY>"+groupExtra+PlainButton(10,100,200)+PlainButton(11,100,300)+"<control><type>group</type><id>91</id>"+PlainButton(12,300,200)+PlainButton(13,300,300)+"</control></control>");}
        static void RunPlainNavigationTests(string root)
        {
            Test("Property-driven empty layout fallbacks are not mistaken for ordinary groups",()=>
            {var p=PlainScene(root,"plain-navigation-bound-layout","<layout property='#runtime.layout' />");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&!p.Dirty,"Dynamic layout was guessed.");});
            Test("Plain group children retain absolute positions and navigate through nested wrappers",()=>
            {var p=PlainScene(root,"plain-navigation-route");var s=new PreviewState(p){FocusId=10};var layout=new StackPreview(p,s);Check(layout.Bounds(p.Controls.First(c=>c.Get("id")=="10")).X==100,"Plain wrapper translated child coordinates.");GridMove(p,s,"onright",12);GridMove(p,s,"ondown",13);GridMove(p,s,"onleft",11);GridMove(p,s,"onup",10);Check(!p.Dirty&&p.Undo.Count==0,"Plain routes mutated XML.");});
            Test("Standalone buttons can enter and leave ordinary groups",()=>
            {var p=AuditFixture(root,"plain-navigation-root",PlainButton(20,50,100)+"<control><type>group</type>"+PlainButton(21,250,100)+"</control>");var s=new PreviewState(p){FocusId=20};GridMove(p,s,"onright",21);GridMove(p,s,"onleft",20);});
            Test("Plain wrappers can contain automatic children with independent absolute origins",()=>
            {var p=AuditFixture(root,"plain-navigation-auto","<control><type>group</type><posX>800</posX><posY>600</posY>"+PlainButton(10,100,200)+"<control><type>group</type><posX>300</posX><posY>200</posY><layout>GridLayout(1,0,0,20)</layout>"+NestedButton(11)+NestedButton(12)+"</control></control>");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",11);GridMove(p,s,"ondown",12);GridMove(p,s,"onup",11);GridMove(p,s,"onleft",10);});
            Test("Plain groups arranged by automatic ancestors remain guarded",()=>
            {var p=AuditFixture(root,"plain-navigation-reflow","<control><type>group</type><layout>StackLayout()</layout><control><type>group</type><width>500</width><height>200</height>"+PlainButton(10,100,200)+PlainButton(11,300,200)+"</control></control>");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&!p.Dirty,"Unsupported translation leaked state.");});
            Test("Plain groups preserve hidden branch and unknown visibility protection",()=>
            {var p=PlainScene(root,"plain-navigation-visibility");p.Edit(p.Controls.First(c=>c.Get("id")=="91"),Set("Visibility","Hidden"),"Hide group");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",10);p.Edit(p.Controls.First(c=>c.Get("id")=="91"),new System.Collections.Generic.Dictionary<string,string>{{"Visibility","Visible"},{"visible","unknown.condition()"}},"Unverified branch");s=new PreviewState(p){FocusId=10};string signature=ProjectFile.ContentSignature(p);Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&signature==ProjectFile.ContentSignature(p),"Unknown plain branch changed state.");});
            Test("Grouping and ungrouping preserve geometric routes through undo and redo",()=>
            {var p=AuditFixture(root,"plain-navigation-structure",PlainButton(10,100,200)+PlainButton(11,300,200));string xml=p.Controls[0].Source.Text;string key=p.CreateGroup(p.Controls.ToArray());var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",11);p.UndoChange();s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",11);p.RedoChange();s=new PreviewState(p){FocusId=11};GridMove(p,s,"onleft",10);p.UngroupControl(p.Controls.First(c=>c.Key==key));s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",11);Check(p.Controls[0].Source.Text.Contains("<id>10</id>")&&xml.Contains("<id>11</id>"),"Structural navigation lost source controls.");});
            Test("Plain button edits roundtrip and export without modifying originals",()=>
            {var p=PlainScene(root,"plain-navigation-save");var original=File.ReadAllBytes(p.Controls[0].Source.Path);string before=p.Controls[0].Source.Text;var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",12);p.Edit(p.Controls.First(c=>c.Get("id")=="12"),Set("posX","320"),"Move plain button");string after=p.Controls[0].Source.Text,file=Path.Combine(root,"plain-navigation.mp1skinproj"),zip=Path.Combine(root,"plain-navigation.zip");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);Check(new PreviewState(q).FocusId==12,"Plain focus did not reopen.");q.Export(zip);using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after,"Plain export changed XML.");q.UndoChange();Check(q.Controls[0].Source.Text==before,"Plain restored undo failed.");q.RedoChange();Check(q.Controls[0].Source.Text==after&&File.ReadAllBytes(q.Controls[0].Source.Path).SequenceEqual(original),"Plain redo changed disk originals.");});
        }
    }
}
