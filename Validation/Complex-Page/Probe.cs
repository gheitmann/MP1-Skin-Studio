using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MP1SkinStudio;

// Console-only integration gate. No forms, canvas, capture or client automation.
public static class ComplexPageProbe
{
    static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
    static string Entry(ZipArchive archive,string name)
    { using(var reader=new StreamReader(archive.GetEntry(name).Open()))return reader.ReadToEnd(); }
    public static int Main(string[] args)
    {
        var report=new List<string>();
        try
        {
            string root=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);
            Require(!output.Equals(root,StringComparison.OrdinalIgnoreCase)&&!output.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase),"Output must be outside the original skin.");
            Directory.CreateDirectory(output);
            var p=new SkinProject(root);p.OpenPage("myvideo.xml");
            var original=p.Sources.Values.ToDictionary(s=>s.Path,s=>ProjectFile.Digest(File.ReadAllBytes(s.Path)));
            var facade=p.Controls.Single(c=>c.Get("type")=="facadeview"&&c.Get("id")=="50");
            string source=facade.Source.RelativePath;
            Require(source=="common.facade.video.default.xml","Unexpected facade source.");
            var templates=p.Controls.Where(c=>c.RuntimeParent==facade&&CollectionSettings.IsCollection(c)).ToArray();
            Require(templates.Any(c=>c.Get("type")=="listcontrol")&&templates.Any(c=>c.Get("type")=="filmstrip")&&templates.Any(c=>c.Get("type")=="coverflow"),"Missing real view templates.");
            report.Add("PASS: loaded myvideo.xml and imported facade with "+templates.Length+" subviews; "+p.Controls.Count+" controls across "+original.Count+" loaded sources.");
            string xmlSignature=ProjectFile.XmlSignature(p);
            foreach(string mode in new[]{"list","smallicons","largeicons","filmstrip","coverflow"})
            {
                p.Editor.Collection.Mode=mode;
                var active=templates.Where(c=>CollectionSettings.Active(p,c)).ToArray();
                Require(active.Length==1&&CollectionSettings.Kind(active[0],mode)==mode,"Incorrect active template for "+mode);
                var tiles=CollectionGeometry.Tiles(active[0],LayoutTools.Bounds(active[0]),p.Editor.Collection);
                Require(tiles.Length>0&&tiles.Count(t=>t.Selected)==1,"Invalid sample geometry for "+mode);
                Require(tiles.All(t=>t.Bounds.Width>0&&t.Bounds.Height>0&&!Single.IsNaN(t.Bounds.X)&&!Single.IsInfinity(t.Bounds.Y)),"Invalid tile bounds.");
                var state=new PreviewState(p);state.UpdateCollectionValues();
                Require(state.Evaluate("facadeview."+mode,new HashSet<string>())==true,"View condition is stale: "+mode);
                report.Add("PASS: "+mode+" activates one real template and produces "+tiles.Length+" sample tiles with selected content.");
            }
            Require(ProjectFile.XmlSignature(p)==xmlSignature&&!p.Dirty&&p.Undo.Count==0,"View switches changed skin XML.");
            p.Editor.Collection.Mode="list";p.Editor.Collection.Items.Clear();
            for(int i=0;i<25;i++)p.Editor.Collection.Items.Add(new PreviewItem{Title="EDITOR SAMPLE "+i,Detail="Detail "+i,Image=""});
            p.Editor.Collection.Focus=24;
            var preview=new PreviewState(p);preview.UpdateCollectionValues();
            Require(preview.Values["#selecteditem"]=="EDITOR SAMPLE 24"&&preview.Values["#itemcount"]=="25","Selected sample bindings are stale.");
            var list=CollectionSettings.Template(p,facade,"list");string listKey=list.Key;
            var rows=CollectionGeometry.Tiles(list,LayoutTools.Bounds(list),p.Editor.Collection);
            Require(rows.Last().Index==24&&rows.Last().Selected&&rows[0].Index>0,"Last sample item did not scroll into view.");
            Require(ProjectFile.XmlSignature(p)==xmlSignature&&!p.Dirty,"Preview data changed XML.");
            report.Add("PASS: 25 preview items update selected-item/count bindings and scroll the last selection into the real list viewport without XML edits.");
            string before=list.Source.Text;
            var untouched=templates.Where(c=>c.Key!=listKey).ToDictionary(c=>c.Key,c=>c.Node.Element.OuterXml);
            var values=new Dictionary<string,string>{{"width","658"},{"textXOff","60"},{"textureHeight","66"},{"spaceBetweenItems","12"}};
            foreach(var pair in values){var field=CollectionProperties.Find(pair.Key);if(field!=null){Require(field.AppliesTo(list),"Collection field is not available for the real list.");field.Validate(pair.Value);}}
            p.Edit(list,values,"Complex-page list geometry");
            Require(p.Sources.Values.Count(s=>s.Changed)==1&&p.Sources.Values.Single(s=>s.Changed).RelativePath==source,"Edit escaped the imported source.");
            Require(p.Controls.Single(c=>c.Key==listKey).Get("width")=="658","List edit missing.");
            var resized=p.Controls.Single(c=>c.Key==listKey);var resizedRows=CollectionGeometry.Tiles(resized,LayoutTools.Bounds(resized),p.Editor.Collection);
            Require(resizedRows.Length>1&&resizedRows[0].Bounds.Height==66&&resizedRows[1].Bounds.Y-resizedRows[0].Bounds.Y==78,"Row height or spacing edits were not reflected in geometry.");
            Require(untouched.All(pair=>p.Controls.Single(c=>c.Key==pair.Key).Node.Element.OuterXml==pair.Value),"An inactive view changed.");
            string edited=p.Sources.Values.Single(s=>s.Changed).Text;
            p.UndoChange();Require(!p.Dirty&&p.Controls.Single(c=>c.Key==listKey).Source.Text==before,"Undo was not exact.");
            p.RedoChange();Require(p.Controls.Single(c=>c.Key==listKey).Source.Text==edited,"Redo was not exact.");
            report.Add("PASS: width/text-offset/row-height/spacing edits use compatible property definitions, update row geometry, affect only the imported list source, preserve inactive view XML, and undo/redo exactly.");
            string expected=ProjectFile.ContentSignature(p),projectPath=Path.Combine(output,"Complex-Page.mp1skinproj");
            ProjectFile.Save(p,projectPath);p=ProjectFile.Load(projectPath);
            Require(ProjectFile.ContentSignature(p)==expected&&p.Editor.Collection.Focus==24&&p.Editor.Collection.Items.Count==25,"Project round trip lost XML or preview state.");
            report.Add("PASS: project save/reopen preserves imported XML edits and independent collection preview state.");
            string zipPath=Path.Combine(output,"Complex-Page-Export.zip");p.Export(zipPath);
            using(var zip=ZipFile.OpenRead(zipPath))
            {
                Require(zip.Entries.Count==2&&zip.GetEntry(source)!=null&&zip.GetEntry("_SkinStudio-Instructions.txt")!=null,"Unexpected export entries.");
                Require(Entry(zip,source)==edited&&!Entry(zip,source).Contains("EDITOR SAMPLE"),"Export changed XML or leaked preview data.");
            }
            report.Add("PASS: export contains exactly the edited shared XML plus instructions; sample titles are excluded.");
            Require(original.All(pair=>ProjectFile.Digest(File.ReadAllBytes(pair.Key))==pair.Value),"An original source changed.");
            report.Add("PASS: all "+original.Count+" loaded original source hashes remain unchanged.");
            report.Add("LIMIT: no editor GUI, renderer or MediaPortal was launched. Visual fidelity, masks, animations and runtime plugin behavior remain unverified.");
            File.WriteAllLines(Path.Combine(output,"Results.txt"),report,new UTF8Encoding(false));
            foreach(string line in report)Console.WriteLine(line);return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
