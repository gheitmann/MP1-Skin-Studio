using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static SkinProject ResetFixture(string root,string name)
        {
            string folder=Path.Combine(root,name);Directory.CreateDirectory(folder);
            Write(folder,"references.xml","<controls><control><type>label</type><width>100</width><font>font22</font></control><style Name=\"Title\"><width>200</width><textcolor>ff112233</textcolor></style></controls>");
            Write(folder,"main.xml","<window><controls><control Style=\"Title\"><type>label</type><id>0</id><width>300</width><label>Text</label><!--keep--><extension keep=\"yes\"/></control><import>part.xml</import></controls></window>");
            Write(folder,"part.xml","<window><controls><control><type>label</type><id>0</id><width>400</width></control></controls></window>");
            var project=new SkinProject(folder);project.OpenPage("main.xml");return project;
        }
        static void RunPropertyResetTests(string root)
        {
            Test("Reset removes only the local XML span and its own blank line",()=>
            {
                string text="<window>\r\n  <controls>\r\n    <control>\r\n      <type>label</type>\r\n      <!--outside-->\r\n      <width>300</width>\r\n      <unknown value=\"keep\"/>\r\n    </control>\r\n  </controls>\r\n</window>";
                var source=SkinSource.FromText(text,"a.xml");source.ResetControlProperty(0,"width");Check(source.Text==text.Replace("      <width>300</width>\r\n",""),"Other source spans were modified.");
            });
            Test("Removal preserves comments and processing instructions without misreading CDATA",()=>
            {
                string text="<window><controls><control><type>label</type><label><!--keep--><?studio keep?><![CDATA[<!--value-only-->]]>Text<!--last--></label><other>keep</other></control></controls></window>";
                var source=SkinSource.FromText(text,"a.xml");source.ResetControlProperty(0,"label");
                Check(source.Text=="<window><controls><control><type>label</type><!--keep--><?studio keep?><!--last--><other>keep</other></control></controls></window>","Comments, processing instructions or CDATA were handled incorrectly.");
            });
            Test("Reset previews prefer styles over references without changing the session",()=>
            {
                var p=ResetFixture(root,"reset-style");string signature=ProjectFile.ContentSignature(p);var info=p.PreviewPropertyReset(p.Controls[0],"width");
                Check(info.HasLocal&&info.Allowed&&info.Inherited.Value=="200"&&info.Inherited.Origin=="Style Title"&&ProjectFile.ContentSignature(p)==signature&&!p.Dirty,"Preview is incorrect or writes changes.");
                p.ResetProperty(new[]{p.Controls[0]},"width",false);Check(p.Controls[0].Get("width")=="200"&&!p.Controls[0].Properties["width"].Local&&p.Controls[0].Node.Child("extension")!=null,"Inherited style or extension was lost.");
            });
            Test("Multiple sources inherit reference and style values in one exact undo/redo step",()=>
            {
                var p=ResetFixture(root,"reset-many");string[] before=p.Controls.Select(c=>c.Source.Text).ToArray();p.ResetProperty(p.Controls,"width",false);
                Check(p.Controls[0].Get("width")=="200"&&p.Controls[1].Get("width")=="100"&&p.Undo.Count==1,"Incorrect inheritance differences or transaction.");
                string[] after=p.Controls.Select(c=>c.Source.Text).ToArray();p.UndoChange();Check(p.Controls.Select(c=>c.Source.Text).SequenceEqual(before)&&!p.Dirty,"Undo is not exact.");p.RedoChange();Check(p.Controls.Select(c=>c.Source.Text).SequenceEqual(after),"Redo is not exact.");
            });
            Test("Reset without an inherited value removes the property instead of writing a substitute",()=>
            {
                var p=ResetFixture(root,"reset-absent");var info=p.PreviewPropertyReset(p.Controls[0],"label");Check(info.Inherited==null,"Unexpected inheritance.");p.ResetProperty(new[]{p.Controls[0]},"label",false);
                Check(!p.Controls[0].Properties.ContainsKey("label")&&p.Controls[0].Node.Child("label")==null,"Artificial substitute value was inserted.");
            });
            Test("Non-local values are a no-op without an undo step",()=>
            {
                var p=ResetFixture(root,"reset-noop");string signature=ProjectFile.ContentSignature(p);p.ResetProperty(p.Controls,"font",false);
                Check(!p.PreviewPropertyReset(p.Controls[0],"font").HasLocal&&p.Undo.Count==0&&ProjectFile.ContentSignature(p)==signature,"Inherited value was changed.");
            });
            Test("Attributes, nested or duplicate properties and identities remain protected",()=>
            {
                foreach(string extra in new[]{"<width mode=\"keep\">300</width>","<width><extension>300</extension></width>","<width>300</width><width>400</width>"})
                {var source=SkinSource.FromText("<window><controls><control><type>label</type><id>10</id>"+extra+"</control></controls></window>","a.xml");string before=source.Text;Reject(()=>source.ResetControlProperty(0,"width"));Reject(()=>source.ResetControlProperty(0,"id"));Reject(()=>source.ResetControlProperty(0,"type"));Check(source.Text==before,"Protected structure was changed.");}
            });
            Test("Protected bindings block the whole selection until replacement is allowed",()=>
            {
                var p=ResetFixture(root,"reset-bindings");p.Edit(p.Controls[1],Set("width","#plugin.width"),"Bindung");string signature=ProjectFile.ContentSignature(p);int steps=p.Undo.Count;
                Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(ProjectFile.ContentSignature(p)==signature&&p.Undo.Count==steps,"Partial change occurred despite a binding.");
                p.ResetProperty(p.Controls,"width",true);Check(p.Controls[0].Get("width")=="200"&&p.Controls[1].Get("width")=="100","Approval had no effect.");
            });
            Test("Locked or structurally protected selections are rejected before partial edits",()=>
            {
                var p=ResetFixture(root,"reset-atomic");p.Editor.Locked.Add(p.Controls[1].Key);string before=ProjectFile.ContentSignature(p);Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(before==ProjectFile.ContentSignature(p)&&!p.Dirty,"Partial change occurred despite a lock.");p.Editor.Locked.Clear();
                p.Controls[1].Source.Text=p.Controls[1].Source.Text.Replace("<width>","<width keep=\"yes\">");p.Controls[1].Source.Parse();p.OpenPage("main.xml");before=ProjectFile.ContentSignature(p);
                Reject(()=>p.ResetProperty(p.Controls,"width",false));Check(before==ProjectFile.ContentSignature(p)&&p.Undo.Count==0,"Partial change occurred before a structural error.");
            });
            Test("Reset preserves UTF-16 encoding and BOM",()=>
            {
                string path=Path.Combine(root,"reset-utf16.xml");File.WriteAllText(path,"<?xml version=\"1.0\" encoding=\"utf-16\"?><window><controls><control><type>label</type><width>300</width><label>Äöß</label></control></controls></window>",new UnicodeEncoding(false,true,true));
                var source=SkinSource.Read(path,"reset-utf16.xml");byte[] disk=File.ReadAllBytes(path);source.ResetControlProperty(0,"width");Check(source.Bytes()[0]==255&&source.Bytes()[1]==254&&source.Text.Contains("Äöß")&&disk.SequenceEqual(File.ReadAllBytes(path)),"BOM, text or original file was changed.");
            });
            Test("Projects and ZIP exports preserve targeted resets without changing style or reference files",()=>
            {
                var p=ResetFixture(root,"reset-save");byte[] reference=File.ReadAllBytes(Path.Combine(p.Root,"references.xml"));p.ResetProperty(p.Controls,"width",false);
                string file=Path.Combine(root,"reset.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);Check(ProjectFile.ContentSignature(p)==ProjectFile.ContentSignature(loaded)&&loaded.Controls[0].Get("width")=="200","Project loses the reset.");
                string zip=Path.Combine(root,"reset.zip");loaded.Export(zip);using(var archive=ZipFile.OpenRead(zip))Check(archive.GetEntry("references.xml")==null&&archive.GetEntry("main.xml")!=null&&archive.GetEntry("part.xml")!=null,"Wrong sources were exported.");
                Check(reference.SequenceEqual(File.ReadAllBytes(Path.Combine(p.Root,"references.xml"))),"Reference file was overwritten.");
            });
        }
    }
}
