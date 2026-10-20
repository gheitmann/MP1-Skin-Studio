using System;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static string CatalogSkin(string folder,string name)
        {string path=Path.Combine(folder,name);Directory.CreateDirectory(path);Write(path,"references.xml","<window/>");Write(path,"main.xml","<window><controls/></window>");return path;}
        static void RunSkinCatalogTests(string root)
        {
            RunSkinCopyTests(root);
            Test("Skin discovery finds direct installed children and excludes nested themes and incomplete folders",()=>
            {
                string folder=Path.Combine(root,"catalog-installed");Directory.CreateDirectory(folder);string alpha=CatalogSkin(folder,"Alpha"),beta=CatalogSkin(folder,"Beta");CatalogSkin(Path.Combine(alpha,"Themes"),"Blue");Directory.CreateDirectory(Path.Combine(folder,"Missing"));string metadata=Path.Combine(folder,"Metadata");Directory.CreateDirectory(metadata);Write(metadata,"references.xml","<window/>");
                var found=SkinCatalog.Discover(null,null,new[]{folder});Check(found.Select(c=>c.Root).SequenceEqual(new[]{alpha,beta}),"Discovery lost direct skins or scanned themes/metadata folders.");
            });
            Test("Skin discovery keeps current external skins siblings and the bundled demo without changing files",()=>
            {
                string folder=Path.Combine(root,"catalog-custom");Directory.CreateDirectory(folder);string active=CatalogSkin(folder,"Current"),sibling=CatalogSkin(folder,"Sibling"),demo=CatalogSkin(Path.Combine(root,"catalog-demo"),"Demo");string xml=File.ReadAllText(Path.Combine(active,"main.xml"));
                var found=SkinCatalog.Discover(active,demo,new string[0]);Check(found.Length==3&&found.Any(c=>c.Root==active)&&found.Any(c=>c.Root==sibling)&&found.Any(c=>c.Root==demo)&&File.ReadAllText(Path.Combine(active,"main.xml"))==xml,"External/demo discovery changed data or lost a skin.");
            });
            Test("Skin discovery deduplicates root aliases and disambiguates identical folder names",()=>
            {
                string a=Path.Combine(root,"catalog-a"),b=Path.Combine(root,"catalog-b");string first=CatalogSkin(a,"Same"),second=CatalogSkin(b,"Same");var found=SkinCatalog.Discover(first,null,new[]{a,a.ToUpperInvariant(),b});
                Check(found.Length==2&&found.Select(c=>c.Name).Distinct().Count()==2&&found.Any(c=>c.Root==second),"Ambiguous names or duplicate physical skin paths.");
            });
            Test("An active skin without references stays selectable while missing roots are skipped",()=>
            {
                string active=Path.Combine(root,"catalog-unreferenced");Directory.CreateDirectory(active);Write(active,"main.xml","<window/>");var found=SkinCatalog.Discover(active,null,new[]{Path.Combine(root,"catalog-absent")});Check(found.Count(c=>c.Root==active)==1,"Existing active skin disappeared from its selector.");
            });
        }
    }
}
