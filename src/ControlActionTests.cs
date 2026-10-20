using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ActionFixture(string root,string name,string type,string extra="")
        {return AuditFixture(root,name,NestedButton(10)+"<control><type>"+type+"</type><id>20</id><posX>200</posX><width>100</width><height>50</height><onleft>10</onleft><onright>30</onright><onup>10</onup><ondown>30</ondown>"+extra+"</control>"+NestedButton(30));}
        static SkinControl ActionControl(SkinProject p){return p.Controls.First(c=>c.Get("id")=="20");}
        static void RunControlActionTests(string root)
        {
            Test("Slider value arrows cannot follow even valid explicit navigation links",()=>
            {var p=ActionFixture(root,"action-slider","slider");var s=new PreviewState(p){FocusId=20};string signature=ProjectFile.ContentSignature(p);Reject(()=>p.PreviewNavigationTarget(s,"onleft"));Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(p.NavigationTarget(ActionControl(p),"onright").Get("id")=="30","Raw XML navigation editor lost its link.");Check(s.FocusId==20&&ProjectFile.ContentSignature(p)==signature&&!p.Dirty&&p.Undo.Count==0,"Slider guard changed state.");GridMove(p,s,"onup",10);s.FocusId=20;GridMove(p,s,"ondown",30);});
            Test("Horizontal spin inner-button arrows stay unverified while cross-axis links work",()=>
            {foreach(string orientation in new[]{"","<orientation>horizontal</orientation>","<orientation>0</orientation>"}){var p=ActionFixture(root,"action-spin-h-"+Guid.NewGuid().ToString("N"),"spincontrol",orientation);var s=new PreviewState(p){FocusId=20};Reject(()=>p.PreviewNavigationTarget(s,"onleft"));Reject(()=>p.PreviewNavigationTarget(s,"onright"));GridMove(p,s,"onup",10);s.FocusId=20;GridMove(p,s,"ondown",30);}});
            Test("Vertical spin inner-button arrows use the vertical axis",()=>
            {foreach(string orientation in new[]{"vertical","1"}){var p=ActionFixture(root,"action-spin-v-"+orientation,"spincontrol","<orientation>"+orientation+"</orientation>");var s=new PreviewState(p){FocusId=20};Reject(()=>p.PreviewNavigationTarget(s,"onup"));Reject(()=>p.PreviewNavigationTarget(s,"ondown"));GridMove(p,s,"onleft",10);s.FocusId=20;GridMove(p,s,"onright",30);}});
            Test("Unknown and property-driven spin orientations do not guess an arrow axis",()=>
            {foreach(string value in new[]{"<orientation>#unknown</orientation>","<orientation>diagonal</orientation>","<orientation property='#runtime'>horizontal</orientation>"}){var p=ActionFixture(root,"action-spin-unknown-"+Guid.NewGuid().ToString("N"),"spincontrol",value);var s=new PreviewState(p){FocusId=20};string signature=ProjectFile.ContentSignature(p);foreach(string dir in new[]{"onleft","onright","onup","ondown"})Reject(()=>p.PreviewNavigationTarget(s,dir));Check(s.FocusId==20&&ProjectFile.ContentSignature(p)==signature,"Orientation rejection changed editor state.");}});
            Test("Control action guards retain cross-axis target visibility and ID uncertainty",()=>
            {var p=ActionFixture(root,"action-target-guard","slider");p.Edit(p.Controls.First(c=>c.Get("id")=="30"),Set("visible","unknown.condition()"),"Unknown target");var s=new PreviewState(p){FocusId=20};Reject(()=>p.PreviewNavigationTarget(s,"ondown"));Check(s.FocusId==20,"Unverified cross-axis target leaked focus.");p.Edit(ActionControl(p),Set("onup","999"),"Missing target");Reject(()=>p.PreviewNavigationTarget(s,"onup"));});
            Test("Interaction diagnostics are bounded informational and read-only",()=>
            {var p=ActionFixture(root,"action-diagnostics","slider");var s=new PreviewState(p);string signature=ProjectFile.ContentSignature(p);var issue=PreviewProblems.Run(p,s).Issues.Single(i=>i.Code=="PREVIEW_INTERACTION");Check(issue.Information&&issue.Message.Contains("interaction")&&issue.ControlKey==ActionControl(p).Key&&ProjectFile.ContentSignature(p)==signature&&!p.Dirty,"Interaction explanation was missing or mutated XML.");p.Editor.Hidden.Add(ActionControl(p).Key);Check(!PreviewProblems.Run(p,new PreviewState(p)).Issues.Any(i=>i.Code=="PREVIEW_INTERACTION"),"Editor-hidden control reported visible interaction.");var mark=MarkFixture(root,"action-checkmark-message","<selected>#unknown</selected>");Check(PreviewProblems.Run(mark,new PreviewState(mark)).Issues.Single(i=>i.Code=="PREVIEW_CHECKMARK").Message.StartsWith("Checkmark"),"Checkmark issue was mislabeled as text.");});
            Test("Action-limited controls preserve editable XML through saved project export undo redo",()=>
            {var p=ActionFixture(root,"action-roundtrip","spincontrol");var original=File.ReadAllBytes(ActionControl(p).Source.Path);string before=ActionControl(p).Source.Text;var state=new PreviewState(p);state.SetFocus(20);p.Edit(ActionControl(p),Set("orientation","vertical"),"Change spin orientation");string after=ActionControl(p).Source.Text,file=Path.Combine(root,"action-limit.mp1skinproj"),zip=Path.Combine(root,"action-limit.zip");ProjectFile.Save(p,file);var q=ProjectFile.Load(file);var s=new PreviewState(q);Check(s.FocusId==20,"Saved control focus changed.");Reject(()=>q.PreviewNavigationTarget(s,"onup"));Check(q.NavigationTarget(ActionControl(q),"onup").Get("id")=="10","Guard changed raw XML link.");q.Export(zip);using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after,"Guard changed exported XML.");q.UndoChange();Check(ActionControl(q).Source.Text==before,"Restored orientation undo failed.");q.RedoChange();Check(ActionControl(q).Source.Text==after&&File.ReadAllBytes(ActionControl(q).Source.Path).SequenceEqual(original),"Orientation redo changed original files.");});
            Test("Ordinary button and checkmark arrow mappings remain usable",()=>
            {foreach(string type in new[]{"button","checkmark"}){var p=ActionFixture(root,"action-unaffected-"+type,type);var s=new PreviewState(p){FocusId=20};GridMove(p,s,"onleft",10);s.FocusId=20;GridMove(p,s,"onright",30);Check(!PreviewProblems.Run(p,s).Issues.Any(i=>i.Code=="PREVIEW_INTERACTION"),"Ordinary control received a false limitation.");}});
        }
    }
}
