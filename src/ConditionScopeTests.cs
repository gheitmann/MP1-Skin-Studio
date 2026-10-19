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
            Test("Boolean expression include conditions retain scope and unknown values",()=>
            {
                var p=ScopeFixture(root,"expression-condition","<window><define>#mode:Home</define><controls><include condition=\"#(eq(#mode,'Home'))\" otherwise='alternate.xml'>part.xml</include></controls></window>","<window><define>#mode:Other</define><controls><control><type>label</type><id>8</id><visible>#(not(eq(#mode,'Home')))</visible></control></controls></window>","<window><controls><control><type>label</type><id>9</id></control></controls></window>");
                var state=new PreviewState(p);Check(state.Visible(p.Controls.First(c=>c.Get("id")=="8"))==true&&state.Visible(p.Controls.First(c=>c.Get("id")=="9"))==false,"Expression condition lost include scope or otherwise semantics.");
                Check(state.Evaluate("#(and(true,not(false)))",new HashSet<string>())==true&&state.Evaluate("#(or(false,false))",new HashSet<string>())==false&&state.Evaluate("#(plugin.isenabled('Missing'))",new HashSet<string>())==false,"Boolean functions failed.");
                Check(state.Evaluate("#(eq(#unknown,'Home'))",new HashSet<string>())==null&&state.Evaluate("#(3)",new HashSet<string>())==null&&state.Evaluate("#(unsupported(true))",new HashSet<string>())==null,"Unknown or nonboolean expression was coerced.");
            });
            Test("Visibility of absent static controls is false while ambiguous and cyclic controls stay unknown",()=>
            {
                var p=ScopeFixture(root,"absent-visible","<window><controls><control><type>label</type><id>8</id><visible>!control.isvisible(999)</visible></control><control><type>label</type><id>9</id><visible>control.isvisible(9)</visible></control><control><type>label</type><id>10</id></control><control><type>label</type><id>10</id></control></controls></window>","<window/>");
                var state=new PreviewState(p);Check(state.Visible(p.Controls[0])==true&&state.Evaluate("control.isvisible(999)",new HashSet<string>())==false&&state.Evaluate("control.isvisible(10)",new HashSet<string>())==null&&state.Visible(p.Controls[1])==null,"Absent, ambiguous or cyclic visibility was evaluated incorrectly.");
                Check(state.Evaluate("control.hastext(999)",new HashSet<string>())==null,"Missing content lookup semantics changed.");
            });
            Test("Nested localization expressions use loaded language snapshots and retain missing IDs",()=>
            {
                var p=ResetFixture(root,"content-localize");var c=p.Controls[0];string xml=ProjectFile.XmlSignature(p);
                p.Editor.Language.Primary=LanguagePack.FromBytes(System.Text.Encoding.UTF8.GetBytes("<Language name='Primary'><Section name='unmapped'><String id='3' prefix='My '>Videos</String><String id='299'> Runtime : </String></Section></Language>"),"strings_primary.xml");
                p.Editor.Language.Fallback=LanguagePack.FromBytes(System.Text.Encoding.UTF8.GetBytes("<Language name='Fallback'><Section name='unmapped'><String id='4'>Fallback</String></Section></Language>"),"strings_fallback.xml");var s=new PreviewState(p);
                Check(s.Text(c,"#(L(3))")=="My Videos"&&s.Text(c,"#(L('4'))")=="Fallback","Localization lost prefixes or fallback.");
                Check(s.Text(c,"#(string.rtrim(L(299),' :'))")==" Runtime","Nested native localization did not trim.");
                p.Editor.Language.Prefixes=false;p.Editor.Preview.Values["#languageId"]="3";s=new PreviewState(p);Check(s.Text(c,"#(L(#languageId))")=="Videos","Sample language ID was not resolved.");
                foreach(string expression in new[]{"#(L(99999))","#(L('bad'))","#(L(-1))","#(L(3,4))","#(string.trim(L(99999)))"})Check(s.Text(c,expression)==expression,"Unknown localization was guessed.");
                Check(ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Localization changed skin XML.");
            });
            Test("Localization without a language snapshot or beyond expression bounds stays unresolved",()=>
            {
                var p=ResetFixture(root,"content-localize-bounds");var c=p.Controls[0];Check(new PreviewState(p).Text(c,"#(L(3))")=="#(L(3))","Missing language was guessed.");
                p.Editor.Language.Primary=LanguagePack.FromBytes(System.Text.Encoding.UTF8.GetBytes("<Language><Section name='unmapped'><String id='3'>"+new string('x',8193)+"</String></Section></Language>"),"strings_large.xml");
                Check(new PreviewState(p).Text(c,"#(L(3))")=="#(L(3))","Localization bypassed expression expansion bound.");
            });
            Test("Control.HasText uses known content without recursing through target visibility",()=>
            {
                var p=ScopeFixture(root,"content-text","<window><define>#text:#alias</define><define>#alias:#sample</define><controls><control><type>label</type><id>8</id><label>#text</label><visible>control.hastext(8)</visible></control></controls></window>","<window/>");
                var c=p.Controls.Single();string xml=ProjectFile.XmlSignature(p);var state=new PreviewState(p);
                Check(state.Visible(c)==null,"Missing text was guessed.");p.Editor.Preview.Values["#sample"]="";state=new PreviewState(p);Check(state.Visible(c)==false,"Known empty self-reference did not resolve.");
                p.Editor.Preview.Values["#sample"]="Literal #unknown";state=new PreviewState(p);Check(state.Visible(c)==true&&state.Evaluate("control.isvisible(8)",new HashSet<string>())==true,"Known inserted text was evaluated recursively or content was mistaken for a visibility cycle.");
                p.Edit(c,Set("label","#(string.trim(#sample))"),"Expression");p.Editor.Preview.Values["#sample"]="   ";Check(new PreviewState(p).Visible(p.Controls.Single())==false,"Known empty expression was not recognized.");p.UndoChange();
                Check(ProjectFile.XmlSignature(p)==xml&&p.Redo.Count==1,"Content evaluation changed XML/history.");
            });
            Test("Content conditions keep ambiguous IDs, missing language and unsupported expressions unresolved",()=>
            {
                var p=ScopeFixture(root,"content-unknown","<window><controls><control><type>label</type><id>1</id><label>3</label></control><control><type>label</type><id>2</id><label>#(unsupported(#missing))</label></control><control><type>label</type><id>3</id><label>A</label></control><control><type>label</type><id>3</id><label>B</label></control></controls></window>","<window/>");var s=new PreviewState(p);
                foreach(int id in new[]{1,2,3,999})Check(s.Evaluate("control.hastext("+id+")",new HashSet<string>())==null,"Unknown/ambiguous text was guessed.");
                p.Editor.Language.Primary=LanguagePack.FromBytes(System.Text.Encoding.UTF8.GetBytes("<Language name='Test'><Section name='unmapped'><String id='3'>Videos</String></Section></Language>"),"strings_test.xml");
                Check(new PreviewState(p).Evaluate("control.hastext(1)",new HashSet<string>())==true,"Known language text was not used.");
            });
            Test("Control.HasThumb follows known local images and invalidates changed file observations",()=>
            {
                var p=ScopeFixture(root,"content-thumb","<window><controls><control><type>image</type><id>60</id><texture>#thumb</texture><visible>!control.hasthumb(60)</visible></control></controls></window>","<window/>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));string file=Path.Combine(p.Root,"Media","folder.png");using(var image=new System.Drawing.Bitmap(2,3))image.Save(file,System.Drawing.Imaging.ImageFormat.Png);
                var state=new PreviewState(p);Check(state.Visible(p.Controls.Single())==null,"Unknown image binding was guessed.");
                p.Editor.Preview.Values["#thumb"]="folder.png";state=new PreviewState(p);Check(state.Evaluate("control.hasthumb(60)",new HashSet<string>())==true&&state.Visible(p.Controls.Single())==false,"Known thumbnail/negation failed.");
                File.WriteAllText(file,"invalid image");File.SetLastWriteTimeUtc(file,DateTime.UtcNow.AddMinutes(1));Check(state.Evaluate("control.hasthumb(60)",new HashSet<string>())==null,"Stale thumbnail result survived file replacement.");
                File.Delete(file);Check(state.Evaluate("control.hasthumb(60)",new HashSet<string>())==false,"Missing static thumbnail was not recognized.");
                foreach(string value in new[]{"","-"}){p.Editor.Preview.Values["#thumb"]=value;Check(new PreviewState(p).Evaluate("control.hasthumb(60)",new HashSet<string>())==false,"Empty thumbnail was not recognized.");}
                foreach(string value in new[]{"../outside.png","C:\\outside.png"}){p.Editor.Preview.Values["#thumb"]=value;Check(new PreviewState(p).Evaluate("control.hasthumb(60)",new HashSet<string>())==null,"Out-of-media lookup was guessed.");}
                Check(!p.Dirty&&p.Undo.Count==0,"Thumbnail inspection edited the skin.");
            });
            Test("Thumbnail conditions use theme media before base media",()=>
            {
                var p=ScopeFixture(root,"content-theme","<window><controls><control><type>image</type><id>60</id><texture>folder.png</texture></control></controls></window>","<window/>");
                Directory.CreateDirectory(Path.Combine(p.Root,"Media"));Directory.CreateDirectory(Path.Combine(p.Root,"Themes","Test","Media"));using(var image=new System.Drawing.Bitmap(2,3))image.Save(Path.Combine(p.Root,"Media","folder.png"),System.Drawing.Imaging.ImageFormat.Png);
                File.WriteAllText(Path.Combine(p.Root,"Themes","Test","Media","folder.png"),"invalid");p.Theme="Test";
                Check(new PreviewState(p).Evaluate("control.hasthumb(60)",new HashSet<string>())==null,"Invalid theme media incorrectly used the base thumbnail.");p.Theme="";
                Check(new PreviewState(p).Evaluate("control.hasthumb(60)",new HashSet<string>())==true,"Base media did not recover after theme change.");
            });
            Test("Editing group visibility updates descendants and exact undo restores conditions",()=>
            {var p=ScopeFixture(root,"scope-edit-visibility","<window><controls><control><type>group</type><id>1</id><visible allowhiddenfocus='true'>yes</visible><include>part.xml</include></control></controls></window>","<window><controls><control><type>label</type><id>8</id><label>Child</label></control></controls></window>");string before=p.Controls[0].Source.Text;p.Edit(p.Controls[0],Set("visible","no"),"Hide group");var child=p.Controls.Single(c=>c.Get("id")=="8");Check(new PreviewState(p).Visible(child)==false&&p.Controls[0].Node.Child("visible").Element.GetAttribute("allowhiddenfocus")=="true","Visibility edit lost attributes or descendant state.");p.UndoChange();Check(p.Controls[0].Source.Text==before&&new PreviewState(p).Visible(p.Controls.Single(c=>c.Get("id")=="8"))==true,"Visibility undo is not exact.");p.RedoChange();Check(new PreviewState(p).Visible(p.Controls.Single(c=>c.Get("id")=="8"))==false,"Visibility redo did not update descendants.");});
            Test("Unsupported visibility conditions survive project and export without preview guesses",()=>
            {var p=ScopeFixture(root,"scope-save-visibility","<window><controls><control><type>label</type><id>1</id><visible>yes</visible></control></controls></window>","<window/>");string expression="control.hasthumb(50)";p.Edit(p.Controls[0],Set("visible",expression),"Runtime condition");Check(new PreviewState(p).Visible(p.Controls[0])==null,"Unsupported runtime condition was guessed.");string path=Path.Combine(root,"visibility.mp1skinproj");ProjectFile.Save(p,path);var loaded=ProjectFile.Load(path);Check(loaded.Controls[0].Get("visible")==expression&&new PreviewState(loaded).Visible(loaded.Controls[0])==null,"Project changed condition or preview state.");string zipPath=Path.Combine(root,"visibility.zip");loaded.Export(zipPath);using(var zip=System.IO.Compression.ZipFile.OpenRead(zipPath))using(var reader=new StreamReader(zip.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==loaded.Controls[0].Source.Text,"Export changed the visibility expression.");});
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
            Test("Clean preview distinguishes literal samples from unresolved label bindings",()=>
            {
                var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());var s=new PreviewState(p);var c=p.Controls[0];
                Check(!s.ResolvedLabel(c,"#unknown.runtime")&&!s.ResolvedLabel(c,"99999999")&&s.ResolvedLabel(c,"10003")&&s.ResolvedLabel(c,"-"),"Unverified labels were treated as known.");
                p.Editor.Preview.Values["#custom"]="#literal";s=new PreviewState(p);Check(s.ResolvedLabel(c,"#custom")&&s.Text(c,"#custom")=="#literal","Explicit literal samples were hidden.");
            });
            Test("Parentheses inside quoted comparison values do not change condition structure",()=>
            {var state=new PreviewState(Demo());Check(state.Evaluate("[string.equals(']','x')]+true",new HashSet<string>())==false,"Quoted parenthesis was incorrectly parsed as an expression boundary.");});
            Test("A dash texture placeholder is not reported as a missing image",()=>
            {var p=AuditFixture(root,"texture-sentinel","<control><type>button</type><id>1</id><textureFocus>-</textureFocus><textureNoFocus>-</textureNoFocus><label>Test</label></control>");Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="TEXTURE_MISSING"),"Intentionally empty texture was reported as an error.");});
        }
    }
}
