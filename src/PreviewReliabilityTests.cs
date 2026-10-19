using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunPreviewReliabilityTests(string root)
        {
            Test("Equivalent nested source paths retain one edited identity through restore undo and redo",()=>
            {
                var p=SessionFixture(root,"source-path-alias");Directory.CreateDirectory(Path.Combine(p.Root,"parts"));Write(Path.Combine(p.Root,"parts"),"part.xml","<window><controls><control><type>label</type><id>1</id><label>Original</label></control></controls></window>");
                Write(p.Root,"nested.xml","<window><controls><import>parts/part.xml</import></controls></window>");p.OpenPage("nested.xml");var source=p.Controls[0].Source;string alias=Path.Combine(p.Root,"parts/./part.xml");
                Check(Object.ReferenceEquals(source,p.Source(alias)),"Path separator/dot aliases created duplicate XML identities.");p.Edit(p.Controls[0],Set("label","Changed"),"Nested text");Check(p.Source(alias).Text==source.Text&&p.Sources.Values.Count(s=>s.RelativePath=="parts/part.xml")==1,"Alias lookup lost an in-memory edit.");
                string file=Path.Combine(root,"source-path-alias.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);loaded.UndoChange();Check(!loaded.Dirty&&loaded.Source(alias).Text.Contains("Original"),"Restored undo is inconsistent through an alias.");loaded.RedoChange();Check(loaded.Source(alias).Text==source.Text,"Restored redo reads an unedited duplicate source.");
            });
            Test("Source lookup rejects paths outside the skin without polluting the cache",()=>
            {
                var p=SessionFixture(root,"source-path-outside");string outside=Path.Combine(root,"outside-source.xml");File.WriteAllText(outside,"<window><controls/></window>");int count=p.Sources.Count;string signature=ProjectFile.ContentSignature(p);Reject(()=>p.Source(outside));Check(p.Sources.Count==count&&ProjectFile.ContentSignature(p)==signature,"Outside source lookup changed the project cache or state.");
            });
            Test("Idle conditions require explicit seconds and follow native positive threshold bounds",()=>
            {
                var p=AuditFixture(root,"idle-thresholds","<control><type>label</type><id>1</id><visible>system.idletime(10)</visible></control>");var state=new PreviewState(p);Check(state.Visible(p.Controls[0])==null,"Idle time was inferred from the desktop.");
                p.Editor.Preview.Values[IdlePreview.Key]="0";Check(state.Visible(p.Controls[0])==false,"Active input became idle.");p.Editor.Preview.Values[IdlePreview.Key]="60";
                Check(state.Evaluate("SYSTEM.IDLETIME(60)",new HashSet<string>())==true&&state.Evaluate("system.idletime(61)",new HashSet<string>())==false,"Idle threshold is not inclusive.");
                p.Editor.Preview.Values[IdlePreview.Key]="1000";Check(state.Evaluate("system.idletime(2000)",new HashSet<string>())==true,"Native 1000-second threshold cap was ignored.");
                foreach(string argument in new[]{"0","-1","1.5","#unknown","2147483648"})Check(state.Evaluate("system.idletime("+argument+")",new HashSet<string>())==null,"Invalid/unknown idle threshold was guessed.");
                p.Controls[0].Defines["#delay"]="10";p.Controls[0].Properties["visible"]=new PropertyValue("system.idletime(#delay)","",true);Check(state.Visible(p.Controls[0])==true,"Scoped idle threshold was not resolved.");
            });
            Test("Idle sample validation rejects malformed values before replacing saved projects",()=>
            {
                var p=SessionFixture(root,"idle-validation");p.Editor.Preview.Values[IdlePreview.Key]="60";string file=Path.Combine(root,"idle-safe.mp1skinproj");ProjectFile.Save(p,file);byte[] before=File.ReadAllBytes(file);
                foreach(string invalid in new[]{"-1","86401","1.5","NaN","Infinity"," 10","+10","true",""}){p.Editor.Preview.Values[IdlePreview.Key]=invalid;Reject(()=>p.Editor.Preview.Validate());Reject(()=>ProjectFile.Save(p,file));Check(File.ReadAllBytes(file).SequenceEqual(before),"Invalid idle sample replaced the saved archive.");}
                p.Editor.Preview.Values[IdlePreview.Key]="86400";p.Editor.Preview.Validate();p.Editor.Preview.Values.Remove(IdlePreview.Key);Check(new PreviewState(p).Evaluate("system.idletime(10)",new HashSet<string>())==null,"Removed idle sample stayed active.");
            });
            Test("Idle samples roundtrip independently from XML edits and exports",()=>
            {
                var p=SessionFixture(root,"idle-persist");p.Editor.Preview.Values[IdlePreview.Key]="60";p.Edit(p.Controls[0],Set("posX","80"),"Move");string signature=ProjectFile.ContentSignature(p),file=Path.Combine(root,"idle.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);
                Check(ProjectFile.ContentSignature(loaded)==signature&&new PreviewState(loaded).Evaluate("system.idletime(10)",new HashSet<string>())==true,"Idle state did not survive reopening.");string zip=Path.Combine(root,"idle.zip");loaded.Export(zip);
                using(var archive=ZipFile.OpenRead(zip))using(var reader=new StreamReader(archive.GetEntry("main.xml").Open()))Check(!reader.ReadToEnd().Contains(IdlePreview.Key),"Idle sample leaked into exported XML.");
            });
            Test("Deep, oversized and chained conditions remain unknown without leaking evaluation budgets",()=>
            {
                var p=SessionFixture(root,"condition-budget");var state=new PreviewState(p);
                foreach(string expression in new[]{new string('!',200)+"true",new string('[',100)+"true"+new string(']',100),new string('!',9000)+"true"})Check(state.Evaluate(expression,new HashSet<string>())==null,"Unbounded condition was evaluated.");
                Check(state.Evaluate("true",new HashSet<string>())==true&&state.Evaluate("false+"+new string('!',200)+"true",new HashSet<string>())==false,"Evaluation budget leaked into a later query or broke three-valued logic.");
                var body=new StringBuilder();for(int i=1;i<=150;i++)body.Append("<control><type>label</type><id>").Append(i).Append("</id><visible>").Append(i==150?"true":"control.isvisible("+(i+1)+")").Append("</visible></control>");
                p=AuditFixture(root,"condition-chain",body.ToString());state=new PreviewState(p);Check(state.Visible(p.Controls[0])==null&&state.Visible(p.Controls.Last())==true,"Long dependency chain was unbounded or budget recovery failed.");
            });
            Test("Media folder profiles use explicit matching titles, artwork and neutral browsing data",()=>
            {
                foreach(var entry in new[]{new{Kind="video",Title="Videos",Image="DefaultFolderBig.png"},new{Kind="music",Title="Music",Image="DefaultFolderBigMusic.png"},new{Kind="pictures",Title="Pictures",Image="defaultFolderBigPictures.png"}})
                {
                    var p=AuditFixture(root,"media-profile-"+entry.Kind,"<control><type>listcontrol</type><id>50</id></control>");p.Editor.Preview.Values["#custom"]="Keep";p.Editor.Preview.Plugins.Add("Custom plugin");p.Edit(p.Controls[0],Set("height","300"),"Height");p.UndoChange();string xml=ProjectFile.XmlSignature(p);
                    MediaFolderPreview.Apply(p,entry.Kind);var state=new PreviewState(p);Check(p.Editor.Collection.Items.Single().Title==entry.Title&&state.Values["#selectedthumb"]==entry.Image&&state.Values["#selecteditem"]==entry.Title,"Folder type was mislabeled.");
                    Check(state.Values["#hideinfo"]=="False"&&state.Values["#Titan.HiddenMenu"]=="false"&&state.Evaluate("system.idletime(10)",new HashSet<string>())==false,"Explicit neutral browsing flags were not installed.");
                    Check(state.Values["#custom"]=="Keep"&&p.Editor.Preview.Plugins.Contains("Custom plugin")&&ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Redo.Count==1,"Folder preset changed unrelated data, XML or redo history.");
                    if(entry.Kind=="pictures")Check(new[]{"#pictures.IsVideo","#pictures.IsHDR","#pictures.Favorite","#pictures.exif.ishdr","#PicturesFolderChanged"}.All(k=>state.Values[k]=="false"),"Picture folder acquired video/HDR/file metadata.");
                }
            });
            Test("Media folder rejection preserves all state for missing lists and full sample/focus maps",()=>
            {
                var p=SessionFixture(root,"media-reject");string before=ProjectFile.ContentSignature(p);Reject(()=>MediaFolderPreview.Apply(p,"music"));Reject(()=>MediaFolderPreview.Apply(p,"unknown"));Check(ProjectFile.ContentSignature(p)==before,"Rejected profile changed the project.");
                p=AuditFixture(root,"media-full-samples","<control><type>listcontrol</type><id>50</id></control>");for(int i=0;i<128;i++)p.Editor.Preview.Values["#custom"+i]="Keep";before=ProjectFile.ContentSignature(p);Reject(()=>MediaFolderPreview.Apply(p,"pictures"));Check(ProjectFile.ContentSignature(p)==before,"Sample capacity failure partially applied profile.");
                p.Editor.Preview.Values.Clear();for(int i=0;i<128;i++)p.Editor.RememberFocus("page"+i+".xml","",1);before=ProjectFile.ContentSignature(p);Reject(()=>MediaFolderPreview.Apply(p,"pictures"));Check(ProjectFile.ContentSignature(p)==before,"Focus capacity failure partially applied profile.");
            });
            Test("Preview problems identify missing samples and unsupported spelling without source mutation",()=>
            {
                var p=AuditFixture(root,"preview-problems","\n<control><type>label</type><id>1</id><visible>string.equals(#missing,true)+Control.IsVisble(9)</visible></control>");var state=new PreviewState(p);string signature=ProjectFile.ContentSignature(p);var values=String.Join(";",state.Values.OrderBy(v=>v.Key).Select(v=>v.Key+"="+v.Value));var report=PreviewProblems.Run(p,state);var issue=report.Issues.Single();
                Check(issue.Code=="PREVIEW_CONDITION"&&issue.Information&&issue.Line==2&&issue.ControlKey==p.Controls[0].Key&&issue.Detail.Contains("Missing sample or unresolved define: #missing")&&issue.Detail.Contains("Check the spelling of Control.IsVisible"),"Preview issue lacks actionable scope/spelling/source details.");
                Check(report.Warnings==0&&ProjectFile.ContentSignature(p)==signature&&String.Join(";",state.Values.OrderBy(v=>v.Key).Select(v=>v.Key+"="+v.Value))==values&&!p.Dirty&&p.Undo.Count==0,"Read-only preview explanation changed the session.");
            });
            Test("Preview explanations skip quoted literal tokens and preserve include contexts",()=>
            {
                var p=ScopeFixture(root,"preview-problem-scope","<window><define>#outer:Parent</define><controls><include condition=\"string.equals(#parentMissing,'#literal')\">part.xml</include></controls></window>","<window><define>#outer:Child</define><controls><control><type>label</type><id>1</id><visible>string.equals(#childMissing,yes)+string.equals(#outer,Child)</visible></control></controls></window>");
                var report=PreviewProblems.Run(p,new PreviewState(p));var issue=report.Issues.Single();Check(issue.Detail.Contains("Missing sample or unresolved define: #parentMissing")&&issue.Detail.Contains("Missing sample or unresolved define: #childMissing")&&!issue.Detail.Contains("Missing sample or unresolved define: #literal")&&!issue.Detail.Contains("Missing sample or unresolved define: #outer"),"Literal tokens or scoped defines were misdiagnosed.");
            });
            Test("Preview explanations expose ambiguous controls, cycles and unsupported artwork caches",()=>
            {
                var p=AuditFixture(root,"preview-problem-dependencies","<control><type>label</type><id>1</id><visible>control.isvisible(2)</visible></control><control><type>label</type><id>2</id></control><control><type>label</type><id>2</id></control><control><type>label</type><id>3</id><visible>control.isvisible(3)</visible></control><control><type>label</type><id>4</id><visible>!control.hasthumb(5)</visible></control><control><type>image</type><id>5</id><texture>../../Thumbs/cache.png</texture></control><control><type>label</type><id>6</id><visible>control.hastext(999)</visible></control>");
                var report=PreviewProblems.Run(p,new PreviewState(p));string details=String.Join("\n",report.Issues.Select(i=>i.Detail));Check(details.Contains("Control ID 2 is ambiguous")&&details.Contains("Cyclic control dependency involving ID 3")&&details.Contains("runtime/cache artwork outside")&&details.Contains("Control 999 is absent"),"Dependency explanation lost a known cause.");
            });
            Test("Preview reports exclude known-hidden content and separate unresolved visible text",()=>
            {
                var p=AuditFixture(root,"preview-problem-hidden","<control><type>label</type><id>1</id><visible>false+unknown.condition()</visible><label>#hidden</label></control><control><type>label</type><id>2</id><visible>string.equals(#show,yes)</visible><label>#missingText</label></control>");var state=new PreviewState(p);var report=PreviewProblems.Run(p,state);Check(report.Issues.Count==1&&report.Issues[0].Code=="PREVIEW_CONDITION","Known-hidden content or unresolved-visibility text flooded report.");
                p.Editor.Preview.Values["#show"]="yes";report=PreviewProblems.Run(p,state);Check(report.Issues.Count==1&&report.Issues[0].Code=="PREVIEW_TEXT"&&report.Issues[0].Detail.Contains("#missingText"),"Unresolved visible text was not explained after condition became known.");
            });
            Test("Preview text problems identify missing loaded language IDs",()=>
            {
                var p=AuditFixture(root,"preview-problem-language","<control><type>label</type><id>1</id><label>42</label></control>");p.Editor.Language.Primary=LanguagePack.FromBytes(Encoding.UTF8.GetBytes("<Language name='Test'><Section name='unmapped'><String id='1'>One</String></Section></Language>"),"strings_test.xml");var issue=PreviewProblems.Run(p,new PreviewState(p)).Issues.Single();Check(issue.Code=="PREVIEW_TEXT"&&issue.Detail.Contains("Language ID 42"),"Missing translation was diagnosed as a generic expression.");
            });
            Test("Preview reports explain idle samples and enforce bounded output",()=>
            {
                var p=AuditFixture(root,"preview-problem-idle","<control><type>label</type><id>1</id><visible>system.idletime(10)</visible></control>");Check(PreviewProblems.Run(p,new PreviewState(p)).Issues.Single().Detail.Contains(IdlePreview.Key),"Idle missing-input explanation is absent.");p.Editor.Preview.Values[IdlePreview.Key]="0";Check(PreviewProblems.Run(p,new PreviewState(p)).Issues.Count==0,"Resolved hidden idle content remained in report.");
                p=AuditFixture(root,"preview-problem-cap",String.Concat(Enumerable.Range(1,1026).Select(i=>"<control><type>label</type><id>"+i+"</id><visible>unknown.condition()</visible></control>")));var report=PreviewProblems.Run(p,new PreviewState(p));Check(report.Issues.Count==1025&&report.Issues.Last().Code=="PREVIEW_LIMIT","Large preview report was unbounded.");
                Reject(()=>PreviewProblems.Run(p,new PreviewState(SessionFixture(root,"preview-wrong-state"))));
            });
        }
    }
}
