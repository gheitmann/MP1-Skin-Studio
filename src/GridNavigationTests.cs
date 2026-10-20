using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject NavigationGrid(string root,string name,string layout="GridLayout(2,0,30,25,Horizontal)",string extra="",string second="")
        {
            string children=String.Concat(Enumerable.Range(10,5).Select(id=>"<control><type>button</type><id>"+id+"</id><width>100</width><height>50</height>"+(id==11?second:"")+"</control>"));
            return AuditFixture(root,name,"<control><type>group</type><id>90</id><posX>100</posX><posY>200</posY><layout>"+layout+"</layout>"+children+"</control>"+extra);
        }
        static void GridMove(SkinProject p,PreviewState s,string direction,int expected)
        {int before=s.FocusId;var target=p.PreviewNavigationTarget(s,direction);Check(SkinProject.ControlId(target)==expected&&s.FocusId==before,"Unexpected grid route or leaked focus: "+direction+" from "+before);s.SetFocus(expected);}
        static void RunGridNavigationTests(string root)
        {
            Test("Grid arrows navigate all axes and retain incomplete-row boundaries",()=>
            {
                var p=NavigationGrid(root,"grid-nav-horizontal");var s=new PreviewState(p){FocusId=10};string xml=ProjectFile.XmlSignature(p);
                GridMove(p,s,"onright",11);GridMove(p,s,"ondown",13);GridMove(p,s,"onleft",12);GridMove(p,s,"ondown",14);GridMove(p,s,"onright",13);GridMove(p,s,"onup",11);GridMove(p,s,"onleft",10);GridMove(p,s,"onup",10);GridMove(p,s,"onleft",10);
                Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Navigation generated XML links or history.");
            });
            Test("Grid navigation follows vertical fill and row-count precedence",()=>
            {
                var p=NavigationGrid(root,"grid-nav-vertical","GridLayout(99,2,30,25,Vertical)");var s=new PreviewState(p){FocusId=10};
                GridMove(p,s,"ondown",11);GridMove(p,s,"onright",13);GridMove(p,s,"onup",12);GridMove(p,s,"onright",14);GridMove(p,s,"ondown",12);GridMove(p,s,"onleft",10);
            });
            Test("Grid navigation skips known hidden candidates and rejects unknown visibility atomically",()=>
            {
                var p=NavigationGrid(root,"grid-nav-hidden",second:"<visible>false</visible>");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",13);
                p.Edit(p.Controls.First(c=>c.Get("id")=="11"),Set("visible","unknown.condition()"),"Unknown visibility");s=new PreviewState(p){FocusId=10};string signature=ProjectFile.ContentSignature(p);Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&signature==ProjectFile.ContentSignature(p),"Rejected navigation changed state.");
            });
            Test("Explicit grid links take precedence over automatic geometry",()=>
            {
                var p=NavigationGrid(root,"grid-nav-explicit");p.Edit(p.Controls.First(c=>c.Get("id")=="10"),Set("onright","14"),"Explicit link");var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",14);
                p.Edit(p.Controls.First(c=>c.Get("id")=="10"),Set("onright","999"),"Missing link");s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));
            });
            Test("Grid navigation considers standalone buttons outside its group",()=>
            {
                var p=NavigationGrid(root,"grid-nav-outside",extra:"<control><type>button</type><id>30</id><posX>500</posX><posY>200</posY><width>100</width><height>50</height></control>");var s=new PreviewState(p){FocusId=11};GridMove(p,s,"onright",30);
            });
            Test("Ambiguous IDs and unsupported collection candidates block automatic grid navigation",()=>
            {
                foreach(string extra in new[]{"<control><type>button</type><id>011</id><width>100</width><height>50</height></control>","<control><type>listcontrol</type><id>30</id><width>100</width><height>50</height></control>"})
                {var p=NavigationGrid(root,"grid-nav-ambiguous-"+Guid.NewGuid().ToString("N"),extra:extra);var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10&&!p.Dirty,"Rejected candidate mutated project.");}
            });
            Test("Grid navigation rejects bound and nonfinite geometry without guessed targets",()=>
            {
                foreach(string value in new[]{"#missing","NaN","Infinity","10000001"})
                {var p=NavigationGrid(root,"grid-nav-bounds-"+Guid.NewGuid().ToString("N"));p.Edit(p.Controls.First(c=>c.Get("id")=="11"),Set("width",value),"Unsupported geometry");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10,"Geometry rejection leaked focus.");}
            });
            Test("Native bearing cones and document-order ties retain their exact selection semantics",()=>
            {
                var p=NavigationGrid(root,"grid-nav-tie",extra:"<control><type>button</type><id>30</id><posX>500</posX><posY>200</posY><width>100</width><height>50</height></control><control><type>button</type><id>31</id><posX>500</posX><posY>200</posY><width>100</width><height>50</height></control>");var s=new PreviewState(p){FocusId=11};GridMove(p,s,"onright",31);
                Check(SkinProject.NavigationBearing(new System.Drawing.Point(0,0),new System.Drawing.Point(-100,0))==270&&SkinProject.NavigationBearing(new System.Drawing.Point(0,0),new System.Drawing.Point(100,0))==-90,"Native bearing normalization changed.");
            });
            Test("Grid edits preserve focus through save export and restored undo redo",()=>
            {
                var p=NavigationGrid(root,"grid-nav-roundtrip");var original=File.ReadAllBytes(p.Controls[0].Source.Path);string before=p.Controls[0].Source.Text;var s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",11);GridMove(p,s,"ondown",13);
                p.Edit(p.Controls[0],Set("layout","GridLayout(99,2,30,25,Vertical)"),"Change fill");string after=p.Controls[0].Source.Text;string file=Path.Combine(root,"grid-navigation.mp1skinproj"),zip=Path.Combine(root,"grid-navigation.zip");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var state=new PreviewState(loaded);Check(state.FocusId==13,"Grid focus was not restored.");GridMove(loaded,state,"onup",12);loaded.Export(zip);
                using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after,"Grid export changed XML.");
                loaded.UndoChange();Check(loaded.Controls[0].Source.Text==before,"Restored grid undo failed.");loaded.RedoChange();Check(loaded.Controls[0].Source.Text==after&&File.ReadAllBytes(loaded.Controls[0].Source.Path).SequenceEqual(original),"Grid redo changed originals.");
            });
            Test("Grid navigation preserves native ID-one exclusion and editor-hidden candidates",()=>
            {
                var p=NavigationGrid(root,"grid-nav-id-one",extra:"<control><type>button</type><id>1</id><posX>240</posX><posY>200</posY><width>100</width><height>50</height></control>");
                var s=new PreviewState(p){FocusId=11};GridMove(p,s,"onright",11);p.Editor.Hidden.Add(p.Controls.First(c=>c.Get("id")=="11").Key);s=new PreviewState(p){FocusId=10};GridMove(p,s,"onright",13);
            });
            Test("Grid navigation refuses unknown layout inputs and runtime enable rules",()=>
            {
                foreach(var change in new[]{Set("Margin","#missing"),Set("HorizontalAlignment","#missing"),Set("Visibility","#missing"),Set("enable","true"),Set("allowhiddenfocus","true")})
                {var p=NavigationGrid(root,"grid-nav-layout-"+Guid.NewGuid().ToString("N"));p.Edit(p.Controls.First(c=>c.Get("id")=="11"),change,"Unsupported layout");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(s.FocusId==10,"Guard leaked focus.");}
            });
            Test("Grid navigation rejects nested groups instead of fabricating a geometric route",()=>
            {
                var p=AuditFixture(root,"grid-nav-nested","<control><type>group</type><layout>StackLayout()</layout><control><type>group</type><layout>GridLayout(2)</layout><control><type>button</type><id>10</id><width>100</width><height>50</height></control><control><type>button</type><id>11</id><width>100</width><height>50</height></control></control></control>");var s=new PreviewState(p){FocusId=10};Reject(()=>p.PreviewNavigationTarget(s,"onright"));Check(!p.Dirty&&s.FocusId==10,"Nested rejection changed state.");
            });
        }
    }
}
