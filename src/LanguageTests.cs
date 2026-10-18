using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        internal static LanguagePreview DemoLanguage(){return new LanguagePreview{Primary=LanguagePack.Load(Path.Combine(StudioForm.DemoRoot(),"Languages","strings_de.xml")),Fallback=LanguagePack.Load(Path.Combine(StudioForm.DemoRoot(),"Languages","strings_en.xml"))};}
        static LanguagePack Pack(string xml){return LanguagePack.FromBytes(Encoding.UTF8.GetBytes(xml),"strings_test.xml");}
        static void RunLanguageTests(string root)
        {
            Test("Language text uses the primary language, fallback and configurable prefixes",()=>
            {var language=DemoLanguage();string text,origin;Check(language.TryGet(10003,out text,out origin)&&text=="Meine Filme"&&origin.Contains("Deutsch"),"Incorrect primary language or prefix.");language.Prefixes=false;Check(language.TryGet(10003,out text,out origin)&&text=="Filme"&&language.TryGet(10004,out text,out origin)&&text=="Settings"&&origin.Contains("Fallback"),"Incorrect fallback or prefix disabling.");});
            Test("Empty primary translations do not fall back to other text",()=>
            {var language=DemoLanguage();string text,origin;Check(language.TryGet(10005,out text,out origin)&&text==""&&origin.Contains("Deutsch"),"Intentionally empty text was replaced.");});
            Test("Language files reject DTDs, duplicate IDs and unsupported formats",()=>
            {Reject(()=>Pack("<!DOCTYPE Language [<!ENTITY a 'x'>]><Language><Section name='unmapped'><String id='1'>&a;</String></Section></Language>"));Reject(()=>Pack("<Language><Section name='unmapped'><String id='1'>A</String><String id='01'>B</String></Section></Language>"));Reject(()=>Pack("<window/>"));Reject(()=>Pack("<Language><Section name='other'><String id='1'>A</String></Section></Language>"));});
            Test("Language previews preserve numeric XML IDs and identify missing IDs",()=>
            {var p=Demo();p.OpenPage("demo.language.xml");p.Editor.Language.SetFrom(DemoLanguage());var state=new PreviewState(p);var c=p.Controls.First(x=>x.Get("id")=="501");Check(state.Label(c,c.Get("label"))=="Meine Filme"&&c.Get("label")=="10003"&&!p.Dirty&&state.Label(c,"99999999")=="[99999999]","Language ID was overwritten or loss was hidden.");});
            Test("Numeric sample data is not mistaken for language IDs",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());p.Editor.Preview.Values["#title"]="10003";var state=new PreviewState(p);var c=p.Controls[0];Check(state.Label(c,"#title")=="10003","Dynamic numeric value was localized.");c.Defines["#code"]="10003";Check(state.Label(c,"#code")=="Meine Filme"&&state.Label(c,"-")=="","Incorrect static define or empty label.");});
            Test("Language-file snapshots are immutable and copies are independent",()=>
            {var language=DemoLanguage();var copy=language.Copy();copy.Prefixes=false;copy.Primary=null;byte[] bytes=language.Primary.Snapshot();bytes[0]=0;string text,origin;Check(language.TryGet(10003,out text,out origin)&&text=="Meine Filme"&&language.Primary.Snapshot()[0]!=0,"Copy changes original state.");});
            Test("Projects restore language snapshots without original language files",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());string temporary=Path.Combine(root,Guid.NewGuid().ToString("N")+".xml");File.WriteAllBytes(temporary,p.Editor.Language.Primary.Snapshot());p.Editor.Language.Primary=LanguagePack.Load(temporary);p.Editor.ShowUnresolved=false;string before=ProjectFile.ContentSignature(p),path=Path.Combine(root,"language.mp1skinproj");ProjectFile.Save(p,path);File.Delete(temporary);var loaded=ProjectFile.Load(path);Check(ProjectFile.ContentSignature(loaded)==before&&!loaded.Editor.ShowUnresolved&&!loaded.Dirty&&loaded.Editor.Language.Primary.Count==4,"Language state was not fully saved.");using(var zip=ZipFile.OpenRead(path))Check(zip.GetEntry("language/primary.xml")!=null&&zip.GetEntry("language/fallback.xml")!=null,"Language files are missing from the archive.");});
            Test("Corrupted language snapshots and duplicate language states are rejected",()=>
            {var p=Demo();p.Editor.Language.SetFrom(DemoLanguage());string path=Path.Combine(root,"language-bad.mp1skinproj");ProjectFile.Save(p,path);using(var zip=ZipFile.Open(path,ZipArchiveMode.Update)){zip.GetEntry("language/primary.xml").Delete();var entry=zip.CreateEntry("language/primary.xml");using(var writer=new StreamWriter(entry.Open()))writer.Write("<Language/>");}Reject(()=>ProjectFile.Load(path));ProjectFile.Save(p,path);RewriteManifest(path,text=>text.Replace("</editorState>","<languagePreview prefixes='true'/></editorState>"));Reject(()=>ProjectFile.Load(path));});
            Test("Language settings change the editor signature without changing skin exports",()=>
            {var p=Demo();string before=ProjectFile.EditorSignature(p);p.Editor.Language.SetFrom(DemoLanguage());Check(before!=ProjectFile.EditorSignature(p)&&!p.Dirty&&p.Undo.Count==0,"Language is not separated as editor state.");Reject(()=>p.Export(Path.Combine(root,"language-only.zip")));});
            Test("Element search finds translated labels while preserving raw values",()=>
            {var p=Demo();p.OpenPage("demo.language.xml");p.Editor.Language.SetFrom(DemoLanguage());var c=p.Controls.First(x=>x.Get("id")=="501");var state=new PreviewState(p);Check(ElementSearch.Matches(c,"text:Meine Filme",state.Label(c,c.Get("label")))&&ElementSearch.Matches(c,"id:501",state.Label(c,c.Get("label")))&&c.Get("label")=="10003"&&LanguagePreview.UseLabelName(c),"Translated search or raw value was lost.");});
            Test("Missing language IDs are reported only when language previews are loaded",()=>
            {var p=Demo();p.OpenPage("demo.language.xml");p.SetControlText(p.Controls.First(c=>c.Get("id")=="501"),"99999999",true);Check(!SkinAudit.Run(p).Issues.Any(i=>i.Code=="LANG_MISSING"),"Value was guessed without a language preview.");p.Editor.Language.SetFrom(DemoLanguage());var issue=SkinAudit.Run(p).Issues.Single(i=>i.Code=="LANG_MISSING");Check(issue.Property=="label"&&issue.ControlKey==p.Controls.First(c=>c.Get("id")=="501").Key,"Missing ID has no navigable location.");});
            Test("UTF-16 language snapshots preserve BOM and file bytes",()=>
            {string xml="<?xml version='1.0' encoding='utf-16'?><Language name='Deutsch'><Section name='unmapped'><String id='1' prefix='Mein '>Gruß</String></Section></Language>";byte[] bytes=Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(xml)).ToArray();var pack=LanguagePack.FromBytes(bytes,"strings_de.xml");string value;Check(pack.Snapshot().SequenceEqual(bytes)&&pack.TryGet(1,true,out value)&&value=="Mein Gruß","Language bytes or BOM were changed.");});
            Test("Legacy project format 2 remains readable without language snapshots",()=>
            {var p=Demo();p.Edit(Named(p,"Movies"),Set("posX","200"),"Change");string path=Path.Combine(root,"language-format2.mp1skinproj");ProjectFile.Save(p,path);RewriteManifest(path,text=>{var doc=new System.Xml.XmlDocument();doc.LoadXml(text);doc.DocumentElement.SetAttribute("version","2");return doc.OuterXml;});var loaded=ProjectFile.Load(path);Check(!loaded.Editor.Language.Active&&Named(loaded,"Movies").Get("posX")=="200","Previous format is incompatible.");});
        }
    }
    public sealed partial class StudioForm
    {
        void RunLanguageUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.language.xml");string source=selected.Source.Text;ApplyLanguage(SelfTests.DemoLanguage());
            verify(selected.Get("id")=="501"&&selectionName.Text=="Meine Filme"&&((SkinControl)tree.SelectedNode.Tag).Get("label")=="10003"&&tree.SelectedNode.Text=="Meine Filme","Language changes show translated names while preserving original XML IDs");
            elementSearch.Text="text:Meine Filme";verify(SearchResults().Length==1&&VisibleTreeControls().Length==1,"Element search finds translated text in the real UI");elementSearch.Clear();
            verify(!project.Dirty&&project.Undo.Count==0&&SessionNeedsSaving()&&selected.Source.Text==source,"Language changes mark only the editor project for saving");
            using(var dialog=new LanguageDialog(project.Editor.Language)){dialog.Show(this);Application.DoEvents();dialog.Width=860;Application.DoEvents();verify(dialog.SearchFits,"The language-catalog search field remains fully visible in narrow windows");dialog.SearchForTest("10004");verify(dialog.VisibleCount==1,"Language catalogs filter fallback IDs");dialog.PrefixesForTest(false);dialog.SearchForTest("Meine Filme");verify(dialog.VisibleCount==0,"Disabling prefixes is reflected in the language catalog");dialog.Close();}verify(project.Editor.Language.Prefixes,"Cancelling language dialogs preserves preview settings");
            using(var picker=new LanguageDialog(project.Editor.Language,true)){picker.Show(this);Application.DoEvents();picker.SearchForTest("Settings");picker.AcceptForTest();verify(picker.SelectedId==10004,"Language-text selection returns the matching ID rather than fixed text");}
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state)){dialog.Show(this);Application.DoEvents();verify(dialog.PreviewForTest=="Meine Filme","Text editors show language IDs alongside resolved previews");dialog.TextForTest("10004");verify(!dialog.CanAccept&&dialog.PreviewForTest=="Settings","Other language IDs show fallback text and remain protected until approved");dialog.Close();}
            using(var dialog=new TextEditDialog(project.InspectTextEdit(selected),state))
            {dialog.Show(this);Application.DoEvents();bool picked=false;using(var timer=new Timer{Interval=30}){timer.Tick+=(s,e)=>{var picker=Application.OpenForms.OfType<LanguageDialog>().FirstOrDefault();if(picker!=null){timer.Stop();picker.SearchForTest("Settings");picker.AcceptForTest();picked=true;if(picker.DialogResult==DialogResult.None){picker.DialogResult=DialogResult.Cancel;picker.Close();}}};timer.Start();dialog.PickLanguageForTest();timer.Stop();}verify(picked&&!dialog.CanAccept&&dialog.PreviewForTest=="Settings","Language pickers set IDs while preserving replacement protection");dialog.Close();}
            string path=Path.Combine(directory,"Language.mp1skinproj");SaveSession(path);ApplyLanguage(new LanguagePreview());ResumeSession(path);verify(project.Editor.Language.Primary!=null&&selectionName.Text=="Meine Filme"&&!SessionNeedsSaving(),"Saved projects restore preview languages and translated selection");
            LoadProject(DemoRoot(),"demo.xml");var c=project.Controls.First(x=>x.Name=="Movies");project.Edit(c,new Dictionary<string,string>{{"visible","unknown.condition()"}},"Unknown condition");RefreshProject(c.Key);c=selected;verify(canvas.ExerciseDrawn(c),"Unverified conditions remain visible with dotted markers by default");SetUnresolvedPreview(false);verify(!canvas.ExerciseDrawn(c)&&project.Undo.Count==1,"Unverified states can be hidden from the preview without changing conditions");undo.PerformClick();verify(!project.Dirty&&!project.Editor.ShowUnresolved,"XML undo preserves independently selected preview options");
            LoadProject(DemoRoot(),"demo.xml");
        }
        public void PrepareLanguagePreview(){LoadProject(DemoRoot(),"demo.language.xml");ApplyLanguage(SelfTests.DemoLanguage());}
    }
}
