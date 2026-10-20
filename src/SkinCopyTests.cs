using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject CopyFixture(string root,string name)
        {
            var p=AuditFixture(root,name,"<control><type>label</type><id>1</id><label>Original</label></control>");
            Write(p.Root,"references.xml","<controls/>");Directory.CreateDirectory(Path.Combine(p.Root,"Media","Empty"));
            Directory.CreateDirectory(Path.Combine(p.Root,"Themes/Blue"));Write(p.Root,"Themes/Blue/theme.xml","<theme/>");File.WriteAllBytes(Path.Combine(p.Root,"Media","Artwork.bin"),new byte[]{0,255,13,10,42});return p;
        }
        static void NoPartialCopy(string parent,string name)
        {Check(!Directory.Exists(Path.Combine(parent,name))&&!Directory.GetDirectories(parent,".skinstudio-copy-*").Any(),"A partial skin copy was left behind.");}
        static void RunSkinCopyTests(string root)
        {
            Test("Skin copies retain complete nested assets empty folders and exact source bytes",()=>
            {
                var p=CopyFixture(root,"copy-full");var files=Directory.GetFiles(p.Root,"*",SearchOption.AllDirectories).ToDictionary(f=>f,f=>ProjectFile.Digest(File.ReadAllBytes(f)));
                string target=SkinCopyPlan.Capture(p,false).CopyTo(root,"Independent skin",CancellationToken.None);
                Check(files.All(f=>ProjectFile.Digest(File.ReadAllBytes(f.Key))==f.Value&&ProjectFile.Digest(File.ReadAllBytes(Path.Combine(target,f.Key.Substring(p.Root.Length+1))))==f.Value),"Copy lost bytes or modified its source.");
                Check(Directory.Exists(Path.Combine(target,"Media/Empty"))&&Directory.GetFiles(target,"*",SearchOption.AllDirectories).Length==files.Count,"Copy omitted an empty folder or added files.");
                Check(SkinCatalog.Discover(p.Root,null,new string[0],new[]{target}).Any(c=>c.Root==target),"Completed copy was not discoverable.");
            });
            Test("Skin copies can include applied edits without changing source files or undo history",()=>
            {
                var p=CopyFixture(root,"copy-edited");string original=File.ReadAllText(Path.Combine(p.Root,"main.xml"));p.Edit(p.Controls[0],Set("label","New base"),"Rename");string signature=ProjectFile.ContentSignature(p);int undo=p.Undo.Count;
                string target=SkinCopyPlan.Capture(p,true).CopyTo(root,"Edited base",CancellationToken.None);var copied=new SkinProject(target);copied.OpenPage("main.xml");
                Check(copied.Controls[0].Get("label")=="New base"&&!copied.Dirty&&File.ReadAllText(Path.Combine(p.Root,"main.xml"))==original&&ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==undo,"Applied copy mutated the original or lost edits.");
                string disk=SkinCopyPlan.Capture(p,false).CopyTo(root,"Disk base",CancellationToken.None);Check(File.ReadAllText(Path.Combine(disk,"main.xml"))==original,"Disk-only copy included session edits.");
            });
            Test("Skin copy snapshots are independent of later editor edits",()=>
            {
                var p=CopyFixture(root,"copy-snapshot");p.Edit(p.Controls[0],Set("label","Captured"),"First");var plan=SkinCopyPlan.Capture(p,true);p.Edit(p.Controls[0],Set("label","Later"),"Second");
                string target=plan.CopyTo(root,"Captured base",CancellationToken.None);var copied=new SkinProject(target);copied.OpenPage("main.xml");Check(copied.Controls[0].Get("label")=="Captured"&&p.Controls[0].Get("label")=="Later","Worker accessed mutable editor sources.");
            });
            Test("Skin copying rejects duplicate names nested destinations and invalid Windows names",()=>
            {
                var p=CopyFixture(root,"copy-paths");var plan=SkinCopyPlan.Capture(p,false);
                Reject(()=>plan.CopyTo(root,"copy-paths",CancellationToken.None));Reject(()=>plan.CopyTo(p.Root,"Nested",CancellationToken.None));
                File.WriteAllText(Path.Combine(root,"Occupied"),"keep");Reject(()=>plan.CopyTo(root,"Occupied",CancellationToken.None));
                foreach(string name in new[]{"../escape","CON","nul.txt","bad."," bad","bad ","a:b","..",""})Reject(()=>SkinCopyPlan.ValidateName(name));
                Check(File.ReadAllText(Path.Combine(root,"Occupied"))=="keep"&&!Directory.GetDirectories(root,".skinstudio-copy-*").Any(),"Rejected copy overwrote files or created staging folders.");
            });
            Test("Copying edited XML rejects external source drift and removes the unfinished copy",()=>
            {
                var p=CopyFixture(root,"copy-drift");p.Edit(p.Controls[0],Set("label","Session"),"Edit");var plan=SkinCopyPlan.Capture(p,true);File.AppendAllText(Path.Combine(p.Root,"main.xml"),"<!-- External edit -->");
                Reject(()=>plan.CopyTo(root,"Conflicting base",CancellationToken.None));NoPartialCopy(root,"Conflicting base");Check(File.ReadAllText(Path.Combine(p.Root,"main.xml")).EndsWith("<!-- External edit -->"),"External source edit was overwritten.");
            });
            Test("Copy cancellation and locked source failures preserve the original and remove staging",()=>
            {
                var p=CopyFixture(root,"copy-cancel");var plan=SkinCopyPlan.Capture(p,false);using(var cancel=new CancellationTokenSource()){cancel.Cancel();Reject(()=>plan.CopyTo(root,"Cancelled base",cancel.Token));}NoPartialCopy(root,"Cancelled base");
                using(var locked=new FileStream(Path.Combine(p.Root,"Media/Artwork.bin"),FileMode.Open,FileAccess.Read,FileShare.None))Reject(()=>plan.CopyTo(root,"Locked base",CancellationToken.None));NoPartialCopy(root,"Locked base");
                Check(File.ReadAllBytes(Path.Combine(p.Root,"Media/Artwork.bin")).SequenceEqual(new byte[]{0,255,13,10,42}),"Failed copy changed source assets.");
            });
            Test("Invalid copied XML is rejected before the destination skin is published",()=>
            {
                var p=CopyFixture(root,"copy-invalid");var plan=SkinCopyPlan.Capture(p,false);File.WriteAllText(Path.Combine(p.Root,"main.xml"),"<broken");Reject(()=>plan.CopyTo(root,"Invalid base",CancellationToken.None));NoPartialCopy(root,"Invalid base");
            });
            Test("Copied editor source paths cannot escape the staging directory",()=>
            {
                var p=CopyFixture(root,"copy-escape");p.Edit(p.Controls[0],Set("label","Changed"),"Edit");p.Sources.Values.First(s=>s.Changed).RelativePath="../outside.xml";
                Reject(()=>SkinCopyPlan.Capture(p,true).CopyTo(root,"Escaping base",CancellationToken.None));NoPartialCopy(root,"Escaping base");Check(!File.Exists(Path.Combine(root,"outside.xml")),"Edited source escaped its copy.");
            });
        }
    }
}
