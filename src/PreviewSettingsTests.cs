using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunPreviewSettingsTests(string root)
        {
            Test("Oversized project descriptions are rejected while preserving the last saved file",()=>
            {
                var p=ResetFixture(root,"preview-manifest-limit");string file=Path.Combine(root,"preview-limit.mp1skinproj");ProjectFile.Save(p,file);byte[] original=File.ReadAllBytes(file);
                for(int index=0;index<128;index++)p.Editor.Preview.Values.Add("#v"+index.ToString("D3"),new string('界',2048));
                Reject(()=>ProjectFile.Save(p,file));Check(original.SequenceEqual(File.ReadAllBytes(file)),"Last readable project was replaced.");
            });
            Test("Preview data replaces whole variables without prefix errors or recursive replacement",()=>
            {
                var p=ResetFixture(root,"preview-tokens");p.Editor.Preview.Values["#title"]="Title #date";var state=new PreviewState(p);
                Check(state.Text(p.Controls[0],"#title / #title.extra / #date")=="Title #date / #title.extra / Saturday, October 17","Partial variable or inserted text was replaced again.");
                p.Controls[0].Defines["#local"]="#alias";p.Controls[0].Defines["#alias"]="#title";Check(state.Text(p.Controls[0],"#local")=="Title #date","Define chain was resolved incorrectly.");
            });
            Test("Explicit sample values take preview precedence without changing local bindings",()=>
            {
                var p=ResetFixture(root,"preview-priority");p.Controls[0].Defines["#header.label"]="Lokaler Titel";string original=p.Controls[0].Source.Text;
                Check(new PreviewState(p).Text(p.Controls[0],"#header.label")=="Lokaler Titel","Local define without an override was lost.");p.Editor.Preview.Values["#header.label"]="Preview";
                Check(new PreviewState(p).Text(p.Controls[0],"#header.label")=="Preview"&&p.Controls[0].Source.Text==original&&!p.Dirty,"Sample value leaked into XML.");
            });
            Test("Plugin and data states toggle supported conditions while unknown conditions remain unknown",()=>
            {
                var p=ResetFixture(root,"preview-conditions");p.Editor.Preview.SetFrom(PreviewSettings.Preset("Weather"));p.Editor.Preview.Values["#myNotifier.Messages.Count.Unread"]="12";var state=new PreviewState(p);
                Check(state.Evaluate("plugin.isenabled(World Weather)",new HashSet<string>())==true&&state.Evaluate("!string.equals(#myNotifier.Messages.Count.Unread,0)",new HashSet<string>())==true,"Simulated states have no effect.");
                Check(state.Evaluate("plugin.isenabled(World Weather)+unsupported.test()",new HashSet<string>())==null,"Unknown condition was guessed.");
            });
            Test("Invalid preview data is rejected before applying changes",()=>
            {
                var settings=PreviewSettings.Preset("News");string before=settings.SignatureText();var invalid=new PreviewSettings();invalid.Values["no-hash"]="Value";Reject(()=>settings.SetFrom(invalid));Check(settings.SignatureText()==before,"Invalid state was partially applied.");
                invalid.Values.Clear();invalid.Values["#valid"]="\u0001";Reject(()=>settings.SetFrom(invalid));invalid.Values["#valid"]=new string('x',2049);Reject(()=>settings.SetFrom(invalid));
                var copy=settings.Copy();copy.Values["#myNotifier.Messages.Count.Unread"]="99";Check(settings.Values["#myNotifier.Messages.Count.Unread"]=="12","Working copy changes original state.");
            });
            Test("Projects preserve empty values, Unicode, whitespace and plugin names in preview state",()=>
            {
                var p=ResetFixture(root,"preview-roundtrip");p.Editor.Preview.Values["#empty"]="";p.Editor.Preview.Values["#space"]="  \t\r\nÄ & <Wert>  ";p.Editor.Preview.Plugins.Add("World Weather");
                string signature=ProjectFile.ContentSignature(p),file=Path.Combine(root,"preview-roundtrip.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(loaded)==signature&&loaded.Editor.Preview.Values["#space"]==p.Editor.Preview.Values["#space"]&&loaded.Editor.Preview.Values["#empty"]==""&&new PreviewState(loaded).Plugins.Contains("World Weather")&&!loaded.Dirty,"Preview state changed or was not restored.");
            });
            Test("Legacy projects without preview sections load the previous baseline data",()=>
            {
                var p=ResetFixture(root,"preview-legacy");p.Editor.Preview.SetFrom(PreviewSettings.Preset("Weather"));string file=Path.Combine(root,"preview-legacy.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var node=doc.SelectSingleNode("/skinStudioProject/editorState/preview");node.ParentNode.RemoveChild(node);return doc.OuterXml;});
                var loaded=ProjectFile.Load(file);var state=new PreviewState(loaded);Check(loaded.Editor.Preview.Values.Count==0&&state.Plugins.Count==0&&state.Values["#time"]=="18:42","Legacy projects lose the default view.");
            });
            Test("Duplicate preview variables in projects are rejected",()=>
            {
                var p=ResetFixture(root,"preview-invalid");p.Editor.Preview.Values["#title"]="A";string file=Path.Combine(root,"preview-invalid.mp1skinproj");ProjectFile.Save(p,file);
                RewriteManifest(file,text=>{var doc=new XmlDocument();doc.LoadXml(text);var node=doc.SelectSingleNode("/skinStudioProject/editorState/preview/value");node.ParentNode.AppendChild(node.CloneNode(true));return doc.OuterXml;});Reject(()=>ProjectFile.Load(file));
            });
            Test("Preview data changes the editor signature but not the XML signature or skin export",()=>
            {
                var p=ResetFixture(root,"preview-export");p.Edit(p.Controls[0],Set("label","Bearbeitet"),"Test");string xml=ProjectFile.XmlSignature(p),editor=ProjectFile.EditorSignature(p),source=p.Controls[0].Source.Text;
                p.Editor.Preview.SetFrom(PreviewSettings.Preset("Weather"));Check(xml==ProjectFile.XmlSignature(p)&&editor!=ProjectFile.EditorSignature(p),"Incorrect change marker.");
                string file=Path.Combine(root,"preview-export.zip");p.Export(file);using(var zip=ZipFile.OpenRead(file))using(var reader=new StreamReader(zip.GetEntry("main.xml").Open()))Check(reader.ReadToEnd()==source,"Preview values leaked into the skin export.");
            });
        }
    }
    public sealed partial class StudioForm
    {
        void RunPreviewSettingsUiSmoke(string directory,Action<bool,string> verify)
        {
            LoadProject(DemoRoot(),"demo.xml");string initial=ProjectFile.ContentSignature(project);
            using(var dialog=new PreviewSettingsDialog(project))
            {
                dialog.Show();Application.DoEvents();verify(dialog.CollectForTest().Values.Count==0,"Sample-data dialogs do not automatically clear unknown bindings");
                dialog.PresetForTest("Long title");verify(dialog.CollectForTest().Values["#title"].Length>80&&ProjectFile.ContentSignature(project)==initial,"Preset selection remains local to the dialog until applied");dialog.Close();
            }
            verify(ProjectFile.ContentSignature(project)==initial,"Cancelling sample-data dialogs preserves the complete project state");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));string key=selected.Key;fields["posY"].Text="640";
            var notifier=project.Controls.First(c=>c.Get("description")=="MyNotifier message icon");verify(state.Visible(notifier)==false,"The news preset starts with a hidden notification");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.Show();Application.DoEvents();dialog.PresetForTest("News");dialog.AcceptForTest();ApplyPreviewSettings(dialog.Result);}
            verify(state.Visible(notifier)==true&&selected.Key==key&&fields["posY"].Text=="640"&&!project.Dirty&&project.Undo.Count==0,"Sample data toggles control visibility while preserving pending property edits");
            verify(SessionNeedsSaving(),"Preview-state changes are detected as unsaved without XML edits");SelectControl(project.Controls.First(c=>c.Key==key));
            string file=Path.Combine(directory,"Preview.mp1skinproj");SaveSession(file);ResumeSession(file);
            verify(state.Values["#myNotifier.Messages.Count.Unread"]=="12"&&!SessionNeedsSaving()&&!project.Dirty,"Opening projects restores saved preview values and saved-state status");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.Show();Application.DoEvents();dialog.PresetForTest("Weather");dialog.AcceptForTest();ApplyPreviewSettings(dialog.Result);}
            verify(state.Plugins.Contains("World Weather")&&state.Values["#WorldWeather.TodayTemperature"]=="−4 °C","The weather preset applies plugin state and temperature");
            ApplyPreviewSettings(new PreviewSettings());verify(state.Plugins.Count==0&&state.Values["#myNotifier.Messages.Count.Unread"]=="0"&&project.Undo.Count==0,"The baseline restores default values without an XML undo step");
            using(var dialog=new PreviewSettingsDialog(project))
            {dialog.SetValueForTest("invalid","Test");bool rejected=false;try{dialog.CollectForTest();}catch{rejected=true;}verify(rejected&&project.Editor.Preview.Values.Count==0,"Invalid variables are detected before changing the active preview");}
            var dependent=project.Controls.First(c=>c.Get("description")=="Sample news");string dependentKey=dependent.Key;project.Edit(dependent,new Dictionary<string,string>{{"visible","control.hasfocus(10)"}},"Focus check");RefreshProject(key);
            Func<string> mark=()=>TreeControlNodes().First(n=>((SkinControl)n.Tag).Key==dependentKey).Text;
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));bool shown=!mark().StartsWith("○ ",StringComparison.Ordinal);SelectControl(project.Controls.First(c=>c.Get("description")=="Series"));
            verify(shown&&mark().StartsWith("○ ",StringComparison.Ordinal),"Focus changes immediately update tree visibility markers");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
