using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunChangeReviewTests(string root)
        {
            Test("Change review excludes clean XML and preview-only settings",()=>
            {var p=AuditFixture(root,"review-clean","<control><type>button</type><id>10</id><posX>10</posX></control>");p.Editor.ShowUnresolved=!p.Editor.ShowUnresolved;Check(ChangeReview.Capture(p).Length==0,"Preview settings produced XML changes.");});
            Test("Change review includes shared and earlier-page XML without mutating session",()=>
            {
                var p=ScopeFixture(root,"review-pages","<window><controls><import>part.xml</import></controls></window>","<window><controls><control><type>button</type><id>10</id><posX>10</posX></control></controls></window>","<window><controls><control><type>button</type><id>20</id><posX>20</posX></control></controls></window>");
                p.Edit(p.Controls[0],Set("posX","15"),"Edit shared");p.OpenPage("alternate.xml");p.Edit(p.Controls[0],Set("posX","25"),"Edit page");
                string before=ProjectFile.ContentSignature(p);int undo=p.Undo.Count;var files=ChangeReview.Capture(p);
                Check(files.Select(f=>f.RelativePath).SequenceEqual(new[]{"alternate.xml","part.xml"})&&files.All(f=>f.SourceMatches),"Session-wide files or checks missing.");
                Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==undo&&p.Redo.Count==0,"Review modified session.");
                foreach(var file in files)Check(file.EditedHash==ProjectFile.Digest(p.Sources.Values.Single(s=>s.RelativePath==file.RelativePath).Bytes())&&file.OriginalHash==ProjectFile.Digest(File.ReadAllBytes(Path.Combine(p.Root,file.RelativePath))),"Hashes do not identify exact XML bytes.");
            });
            Test("Change review snapshots remain unchanged through Undo and refresh",()=>
            {var p=AuditFixture(root,"review-snapshot","<control><type>button</type><id>10</id><posX>10</posX></control>");p.Edit(p.Controls[0],Set("posX","45"),"Move");var old=ChangeReview.Capture(p)[0];p.UndoChange();Check(old.Edited.Contains("<posX>45</posX>")&&ChangeReview.Capture(p).Length==0,"Snapshot changed or refresh ignored Undo.");p.RedoChange();Check(ChangeReview.Capture(p)[0].Edited==old.Edited,"Redo review differs.");});
            Test("Change review flags same-length source drift and missing sources",()=>
            {var p=AuditFixture(root,"review-drift","<control><type>button</type><id>10</id><posX>10</posX></control>");p.Edit(p.Controls[0],Set("posX","45"),"Move");var source=p.Controls[0].Source;byte[] before=File.ReadAllBytes(source.Path);string sig=ProjectFile.ContentSignature(p);File.WriteAllText(source.Path,source.Original.Replace("<posX>10","<posX>11"),new UTF8Encoding(false));Check(!ChangeReview.Capture(p)[0].SourceMatches&&ChangeReview.Capture(p)[0].SourceStatus=="Source changed","Same-size drift missed.");File.Delete(source.Path);Check(ChangeReview.Capture(p)[0].SourceStatus=="Missing source"&&ProjectFile.ContentSignature(p)==sig,"Missing check changed session.");File.WriteAllBytes(source.Path,before);Check(ChangeReview.Capture(p)[0].SourceMatches,"Restored source not matched.");});
            Test("Change review reports exclusively locked sources without losing snapshot",()=>
            {var p=AuditFixture(root,"review-locked","<control><type>button</type><id>10</id><posX>10</posX></control>");p.Edit(p.Controls[0],Set("posX","45"),"Move");using(var held=new FileStream(p.Controls[0].Source.Path,FileMode.Open,FileAccess.Read,FileShare.None)){var file=ChangeReview.Capture(p)[0];Check(!file.SourceMatches&&file.SourceStatus=="Source unavailable"&&file.Edited.Contains("45"),"Locked source lost review.");}});
            Test("Change review preserves UTF-16 BOM byte identity and Unicode text",()=>
            {string folder=Path.Combine(root,"review-utf16");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"main.xml");File.WriteAllText(path,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><controls><control><type>label</type><id>10</id><label>Ä old</label></control></controls></window>",new UnicodeEncoding(false,true,true));var p=new SkinProject(folder);p.OpenPage("main.xml");byte[] original=File.ReadAllBytes(path);p.Edit(p.Controls[0],Set("label","Ö new"),"Label");var f=ChangeReview.Capture(p)[0];Check(f.OriginalBytes==original.Length&&f.OriginalHash==ProjectFile.Digest(original)&&f.EditedBytes==p.Controls[0].Source.Bytes().Length&&f.EncodingName.Contains("BOM 2")&&f.Edited.Contains("Ö new"),"Encoding/BOM bytes lost.");});
            Test("Change review first difference and line numbers cover insertion deletion and line endings",()=>
            {Check(ChangeReview.LineAt("a\r\nb\rc\nd",8)==4&&ChangeReview.LineAt("a\r\nb",3)==2,"CR/LF line numbers wrong.");foreach(string value in new[]{"X","XYZ",""}){string text="<window><controls><control><type>label</type><label>XY</label></control></controls></window>";var source=SkinSource.FromText(text,"fixture.xml");source.SetControlProperties(0,Set("label",value));var file=new ReviewedXmlChange(source);int first=0;while(first<file.Original.Length&&first<file.Edited.Length&&file.Original[first]==file.Edited[first])first++;Check(file.FirstDifference==first,"First changed character wrong.");}});
            Test("Change review file filtering is literal case insensitive and leaves full snapshot intact",()=>
            {var p=AuditFixture(root,"review-filter","<control><type>button</type><id>10</id><posX>10</posX></control>");p.Edit(p.Controls[0],Set("posX","45"),"Move");var files=ChangeReview.Capture(p);Check(ChangeReview.Filter(files," MAIN.XML ").Length==1&&ChangeReview.Filter(files,"*").Length==0&&files.Length==1,"File filter rewrote snapshot or treated query as pattern.");});
        }
    }
}
