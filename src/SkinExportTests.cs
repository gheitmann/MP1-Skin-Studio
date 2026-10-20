using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string ExportInstructions(string path)
        {using(var archive=ZipFile.OpenRead(path))using(var reader=new StreamReader(archive.GetEntry("_SkinStudio-Instructions.txt").Open()))return reader.ReadToEnd();}
        static void RunSkinExportTests(string root)
        {
            Test("Export instructions identify exact original and edited bytes without embedding machine paths",()=>
            {
                var p=SessionFixture(root,"export-identities");var control=p.Controls.First(c=>c.Source.RelativePath=="part.xml");byte[] before=File.ReadAllBytes(control.Source.Path);p.Edit(control,Set("label","New & text"),"Export");byte[] after=control.Source.Bytes();string signature=ProjectFile.ContentSignature(p),path=Path.Combine(root,"identities.zip");p.Export(path);string text=ExportInstructions(path);
                Check(text.Contains("File: part.xml")&&text.Contains("Original SHA256: "+ProjectFile.Digest(before))&&text.Contains("Exported SHA256: "+ProjectFile.Digest(after))&&text.Contains("Original bytes: "+before.Length)&&text.Contains("Exported bytes: "+after.Length)&&!text.Contains(p.Root),"Export file identities are incomplete or disclose the machine root.");
                using(var zip=ZipFile.OpenRead(path))Check(zip.Entries.Count==2&&zip.GetEntry("part.xml").Length==after.Length,"Export contains unexpected files.");
                Check(ProjectFile.ContentSignature(p)==signature&&File.ReadAllBytes(control.Source.Path).SequenceEqual(before),"Export changed project or source.");
            });
            Test("External source drift blocks export and preserves a previous ZIP and pending edits",()=>
            {
                var p=SessionFixture(root,"export-drift");var c=p.Controls[0];p.Edit(c,Set("posX","80"),"Export");string target=Path.Combine(root,"drift.zip");p.Export(target);byte[] previous=File.ReadAllBytes(target);string signature=ProjectFile.ContentSignature(p);int undo=p.Undo.Count;
                File.AppendAllText(c.Source.Path,"\n<!--external edit-->");Reject(()=>p.Export(target));
                Check(File.ReadAllBytes(target).SequenceEqual(previous)&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==undo&&p.Dirty&&!Directory.GetFiles(root,"drift.zip.*.tmp").Any(),"Conflict damaged archive, state or undo history.");
                string project=Path.Combine(root,"drift-recovery.mp1skinproj");ProjectFile.Save(p,project);Check(File.Exists(project),"Conflict prevented a recovery project save.");
            });
            Test("Missing modified source blocks export before creating the archive",()=>
            {
                var p=SessionFixture(root,"export-missing");var c=p.Controls[0];p.Edit(c,Set("posX","90"),"Export");File.Move(c.Source.Path,c.Source.Path+".backup");string target=Path.Combine(root,"missing-source.zip");Reject(()=>p.Export(target));Check(!File.Exists(target)&&!Directory.GetFiles(root,"missing-source.zip.*.tmp").Any()&&p.Dirty,"Missing-source export wrote output or lost edits.");
            });
            Test("Restoring matching original bytes permits export again without losing undo",()=>
            {
                var p=SessionFixture(root,"export-recover");var c=p.Controls[0];byte[] original=File.ReadAllBytes(c.Source.Path);p.Edit(c,Set("posX","75"),"Export");File.AppendAllText(c.Source.Path," ");string target=Path.Combine(root,"recover.zip");Reject(()=>p.Export(target));File.WriteAllBytes(c.Source.Path,original);p.Export(target);p.UndoChange();Check(!p.Dirty&&p.Controls[0].Get("posX")=="50","Recovery export damaged Undo.");p.RedoChange();Check(p.Dirty&&p.Controls[0].Get("posX")=="75","Recovery export damaged Redo.");
            });
            Test("Export hash records and payload preserve UTF-16 encoding and BOM",()=>
            {
                var p=SessionFixture(root,"export-utf16");string source=Path.Combine(p.Root,"part.xml");File.WriteAllText(source,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><controls><control><type>label</type><id>0</id><label>Ä</label></control></controls></window>",new UnicodeEncoding(false,true));p=new SkinProject(p.Root);p.OpenPage("main.xml");var c=p.Controls.First(x=>x.Source.RelativePath=="part.xml");byte[] original=File.ReadAllBytes(source);p.Edit(c,Set("label","Ö"),"UTF16 export");string target=Path.Combine(root,"utf16-export.zip");p.Export(target);byte[] edited;using(var zip=ZipFile.OpenRead(target))using(var input=zip.GetEntry("part.xml").Open())using(var memory=new MemoryStream()){input.CopyTo(memory);edited=memory.ToArray();}
                Check(edited[0]==255&&edited[1]==254&&edited.SequenceEqual(c.Source.Bytes())&&ExportInstructions(target).Contains(ProjectFile.Digest(original))&&ExportInstructions(target).Contains(ProjectFile.Digest(edited)),"Encoding/BOM or hash identity changed.");
            });
            Test("Drift checking is limited to exported source files",()=>
            {
                var p=SessionFixture(root,"export-scope");p.Edit(p.Controls[0],Set("posX","65"),"Export");File.AppendAllText(Path.Combine(p.Root,"part.xml"),"\n<!--not exported-->");string target=Path.Combine(root,"bounded-export.zip");p.Export(target);Check(ExportInstructions(target).Contains("Only the modified source files were checked for drift"),"Export scope was not explained.");using(var zip=ZipFile.OpenRead(target))Check(zip.GetEntry("part.xml")==null,"Unmodified externally changed source was exported.");
            });
        }
    }
}
