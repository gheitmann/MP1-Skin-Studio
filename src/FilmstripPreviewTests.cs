using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject FilmstripFixture(string root,string name)
        {
            var p=ScopeFixture(root,name,"<window><id>6</id><defaultcontrol>50</defaultcontrol><controls><control><type>button</type><id>2</id></control><control><type>facadeview</type><id>50</id><control><type>listcontrol</type><id>50</id><width>700</width><height>300</height></control><control><type>filmstrip</type><id>50</id><posX>100</posX><posY>300</posY><width>700</width><height>250</height><itemWidth>150</itemWidth><itemHeight>200</itemHeight><thumbPosX>10</thumbPosX><thumbPosY>5</thumbPosY><thumbWidth>120</thumbWidth><thumbHeight>180</thumbHeight></control></control><control><type>image</type><id>101</id><posX property='#facadeview.focus.X'>1500</posX><posY property='#(sub(add(cint(#facadeview.focus.Y),cint(#facadeview.focus.Height)),30))'>-1000</posY><width>50</width><height>50</height></control></controls></window>","<window><controls/></window>");
            p.Editor.Collection.Mode="filmstrip";p.Editor.Collection.Items.Clear();for(int i=0;i<8;i++)p.Editor.Collection.Items.Add(new PreviewItem{Title="Item "+i});return p;
        }
        static void RunFilmstripPreviewTests(string root)
        {
            Test("Static filmstrip focus coordinates follow the selected rendered thumbnail",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-focus");var s=new PreviewState(p);var strip=p.Controls.Single(c=>c.Get("type")=="filmstrip");var tile=CollectionGeometry.Tiles(strip,new StackPreview(p,s).Bounds(strip),p.Editor.Collection).Single(t=>t.Selected);
                Check(s.Values[FacadeKey("X")]==((int)tile.Image.X).ToString()&&s.Values[FacadeKey("Width")]=="120","Focus properties disagree with rendered thumbnail bounds.");
                Check(p.TryMoveCollectionSelection(s,"onright")&&s.Values[FacadeKey("X")]=="260"&&s.Values["#selecteditem"]=="Item 1","Selection did not refresh focus and item data together.");p.Editor.Collection.Focus=7;s.UpdateCollectionValues();Check(s.Values[FacadeKey("X")]=="560","Scrolled sample did not follow its visible slot.");
            });
            Test("Filmstrip focus values clear across view focus and page changes without stale coordinates",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-clear");var s=new PreviewState(p);s.SetFocus(2);Check(FacadeFocusMissing(s),"Leaving facade retained stale coordinates.");s.SetFocus(50);Check(s.Values.ContainsKey(FacadeKey("X")),"Returning to facade did not restore coordinates.");p.Editor.Collection.Mode="list";s.UpdateCollectionValues();Check(FacadeFocusMissing(s),"List mode retained filmstrip coordinates.");p.OpenPage("part.xml");s.UpdateCollectionValues();Check(FacadeFocusMissing(s),"Unrelated page inherited coordinates.");
            });
            Test("Explicit facade samples override derived values and can be removed to resume derivation",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-overrides");p.Editor.Preview.Values[FacadeKey("X")]="900";var s=new PreviewState(p);Check(s.Values[FacadeKey("X")]=="900"&&s.Values[FacadeKey("Y")]=="305","Partial explicit override erased other derived values.");p.TryMoveCollectionSelection(s,"onright");Check(s.Values[FacadeKey("X")]=="900","Navigation overwrote an explicit sample.");p.Editor.Preview.Values.Remove(FacadeKey("X"));s.UpdateCollectionValues();Check(s.Values[FacadeKey("X")]=="260","Removing override did not resume derivation.");
            });
            Test("Empty hidden ambiguous and property-driven facades avoid invented focus coordinates",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-bounds");p.Editor.Collection.Items.Clear();var s=new PreviewState(p);Check(PreviewState.FacadeFocusKeys.All(k=>s.Values.ContainsKey(k)&&s.Values[k]==""),"Empty filmstrip invented a selected item.");
                p.Editor.Collection.Items.Add(new PreviewItem{Title="One"});p.Controls.Single(c=>c.Get("type")=="filmstrip").Properties["visible"]=new PropertyValue("false","",true);s.UpdateCollectionValues();Check(FacadeFocusMissing(s),"Hidden facade published coordinates.");
                p.OpenPage("main.xml");p.Controls.Single(c=>c.Get("type")=="filmstrip").Properties["posX"]=new PropertyValue("100","",true){Expression="#unknown"};s.UpdateCollectionValues();Check(FacadeFocusMissing(s),"Circular/property geometry was inferred.");
                p.OpenPage("main.xml");p.Controls[0].Properties["id"]=new PropertyValue("50","",true);s.UpdateCollectionValues();Check(FacadeFocusMissing(s),"Duplicate logical ID was ignored.");
            });
            Test("Integer preview expressions support native cint add sub without guessing unknown inputs",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-arithmetic");var s=new PreviewState(p);var c=p.Controls.Last();Check(s.Text(c,"#(sub(add(cint(#facadeview.focus.X),cint(#facadeview.focus.Width)),30))")=="200","Nested position arithmetic failed.");
                Check(s.Text(c,"#(cint('invalid'))")=="0"&&s.Text(c,"#(add(1,2,3))")=="6"&&s.Text(c,"#(sub(1,3))")=="-2","Supported integer function semantics differ.");
                foreach(string text in new[]{"#(cint(#missing))","#(add(2147483647,1))","#(sub(-2147483648,1))","#(add('1',2))","#(mul(2,3))"}){string result;Check(!s.TryText(c,text,out result),"Unknown, overflow or unsupported arithmetic was guessed: "+text);}
            });
            Test("Image property positions use samples while fallback XML remains unchanged",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-property");var s=new PreviewState(p);var c=p.Controls.Last();string xml=ProjectFile.XmlSignature(p),signature=ProjectFile.ContentSignature(p);var bounds=new StackPreview(p,s).Bounds(c);Check(bounds.X==110&&bounds.Y==455&&c.Get("posX")=="1500"&&c.Get("posY")=="-1000","Property attribute was ignored or fallback XML mutated.");
                Check(ProjectFile.XmlSignature(p)==xml&&ProjectFile.ContentSignature(p)==signature&&!p.Dirty&&p.Undo.Count==0,"Measuring geometry changed project state.");s.SetFocus(2);bounds=new StackPreview(p,s).Bounds(c);Check(bounds.X==1500&&bounds.Y==-1000,"Unknown geometry did not retain XML fallback.");
            });
            Test("Inherited position attributes survive scalar overrides until an explicit attribute clears them",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-style");Write(p.Root,"references.xml","<window><control><type>image</type><posX property='#offset'>-100</posX></control></window>");Write(p.Root,"styled.xml","<window><define>#offset:250</define><controls><control><type>image</type><id>1</id></control><control><type>image</type><id>2</id><posX>70</posX></control><control><type>image</type><id>3</id><posX property=''>80</posX></control></controls></window>");p.OpenPage("styled.xml");var s=new PreviewState(p);Check(new StackPreview(p,s).Bounds(p.Controls[0]).X==250&&new StackPreview(p,s).Bounds(p.Controls[1]).X==250&&p.Controls[1].Get("posX")=="70"&&p.Controls[1].Properties["posX"].ExpressionOrigin=="references.xml · image"&&new StackPreview(p,s).Bounds(p.Controls[2]).X==80,"Attribute inheritance lost scope, provenance or explicit clearing semantics.");
            });
            Test("Invalid and negative image property results retain fallback and explain geometry limits",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-invalid");var c=p.Controls.Last();foreach(string invalid in new[]{"-1","100001","2.5","NaN","2147483648",""}){p.Editor.Preview.Values[FacadeKey("X")]=invalid;var s=new PreviewState(p);float result;Check(!s.TryImagePosition(c,"posX",out result)&&new StackPreview(p,s).Bounds(c).X==1500,"Invalid image position moved the control.");Check(PreviewProblems.Run(p,s).Issues.Any(i=>i.Code=="PREVIEW_GEOMETRY"&&i.Property=="posX"&&i.Information),"Unsupported geometry was not explained.");}
            });
            Test("Property-driven positions reject edits alignment and copying atomically",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-protection");var c=p.Controls.Last();string signature=ProjectFile.ContentSignature(p);Check(c.IsBound("posX"),"Property attribute is not recognized as a binding.");Reject(()=>p.EditMany(new[]{new ControlEdit(p.Controls[0],Set("width","88")),new ControlEdit(c,Set("posX","60"))},"Mixed edit"));Reject(()=>p.DuplicateControl(c,true));Reject(()=>LayoutTools.Align(new[]{c,p.Controls[0]},"left",true));Reject(()=>c.Source.SetControlProperties(c.Ordinal,Set("posY","40")));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==0,"Rejected operation changed source or history.");
            });
            Test("PIN scenarios remain explicit preserve unrelated samples and can return to unknown",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-pin");var s=new PreviewState(p);string expression="string.equals(#MyVideos.PinLocked,True)";Check(s.Evaluate(expression,new HashSet<string>())==null,"Missing PIN state was guessed.");p.Editor.Preview.Values["#custom"]="Keep";VideoPinPreview.Set(p.Editor.Preview,true);s=new PreviewState(p);Check(s.Evaluate(expression,new HashSet<string>())==true,"Locked preview failed.");VideoPinPreview.Set(p.Editor.Preview,false);s=new PreviewState(p);Check(s.Evaluate(expression,new HashSet<string>())==false,"Unlocked preview failed.");VideoPinPreview.Set(p.Editor.Preview,null);s=new PreviewState(p);Check(s.Evaluate(expression,new HashSet<string>())==null&&p.Editor.Preview.Values["#custom"]=="Keep"&&!p.Dirty,"Removing PIN sample changed other state.");
            });
            Test("Filmstrip selection PIN and explicit focus overrides survive save without export leakage",()=>
            {
                var p=FilmstripFixture(root,"filmstrip-save");p.Editor.Collection.Focus=2;p.Editor.Preview.Values[FacadeKey("Width")]="180";VideoPinPreview.Set(p.Editor.Preview,true);var state=new PreviewState(p);string signature=ProjectFile.ContentSignature(p),x=state.Values[FacadeKey("X")],file=Path.Combine(root,"filmstrip.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var restored=new PreviewState(loaded);Check(ProjectFile.ContentSignature(loaded)==signature&&restored.Values[FacadeKey("X")]==x&&restored.Values[FacadeKey("Width")]=="180"&&loaded.Editor.Preview.Values[VideoPinPreview.Key]=="True","Filmstrip sample changed after reopening.");
                loaded.Edit(loaded.Controls.Last(),Set("width","60"),"Resize");string after=loaded.Controls.Last().Source.Text,zip=Path.Combine(root,"filmstrip-export.zip");loaded.Export(zip);using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==after&&after.Contains("property='#facadeview.focus.X'"),"Export lost original property binding.");loaded.UndoChange();Check(!loaded.Dirty&&new PreviewState(loaded).Values[FacadeKey("X")]==x,"Undo changed filmstrip selection.");loaded.RedoChange();Check(loaded.Controls.Last().Source.Text==after,"Redo did not preserve original attribute.");
            });
        }
        static string FacadeKey(string suffix){return "#facadeview.focus."+suffix;}
        static bool FacadeFocusMissing(PreviewState state){return PreviewState.FacadeFocusKeys.All(k=>!state.Values.ContainsKey(k));}
    }
}
