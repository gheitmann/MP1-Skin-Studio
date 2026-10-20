using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class SkinChoice
    {
        public string Root,Name;
        public override string ToString(){return Name;}
    }
    public static class SkinCatalog
    {
        static string FullPath(string path)
        {return String.IsNullOrWhiteSpace(path)?null:Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);}
        public static SkinChoice[] Discover(string currentRoot,string demoRoot,IEnumerable<string> installedRoots=null,IEnumerable<string> additionalSkins=null)
        {
            var choices=new Dictionary<string,SkinChoice>(StringComparer.OrdinalIgnoreCase);
            Action<string,bool> add=(path,active)=>
            {
                try
                {
                    string root=FullPath(path);if(root==null||!Directory.Exists(root)||choices.ContainsKey(root))return;
                    if(!active&&(!File.Exists(Path.Combine(root,"references.xml"))||!Directory.EnumerateFiles(root,"*.xml").Any(f=>!new[]{"references.xml","fonts.xml","skin.xml","theme.xml"}.Contains(Path.GetFileName(f).ToLowerInvariant()))))return;
                    choices.Add(root,new SkinChoice{Root=root,Name=Path.GetFileName(root)});
                }
                catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}
            };
            add(currentRoot,true);add(demoRoot,false);
            foreach(string copy in additionalSkins??new string[0])add(copy,true);
            var roots=new List<string>(installedRoots??new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Team MediaPortal","MediaPortal","skin"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Team MediaPortal","MediaPortal","skin"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Team MediaPortal","MediaPortal","skin")});
            if(!String.IsNullOrWhiteSpace(currentRoot)){var parent=Directory.GetParent(FullPath(currentRoot));if(parent!=null&&parent.Name!="Themes")roots.Add(parent.FullName);}
            foreach(string root in roots.Where(p=>!String.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try{if(Directory.Exists(root))foreach(string folder in Directory.EnumerateDirectories(root))add(folder,false);}
                catch(IOException){}catch(UnauthorizedAccessException){}
            }
            foreach(var group in choices.Values.GroupBy(c=>c.Name,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1))foreach(var choice in group)choice.Name+=" · "+choice.Root;
            return choices.Values.OrderBy(c=>c.Name,StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
    public sealed partial class StudioForm
    {
        ChoiceBox skinPicker=new ChoiceBox();
        readonly HashSet<string> copiedSkins=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void RefreshSkinChoices()
        {
            bool previous=loading;loading=true;
            try
            {
                skinPicker.Items.Clear();skinPicker.Items.AddRange(SkinCatalog.Discover(project==null?null:project.Root,DemoRoot(),null,copiedSkins).Cast<object>());
                skinPicker.SelectedIndex=project==null?-1:skinPicker.Items.FindIndex(o=>String.Equals(((SkinChoice)o).Root,project.Root.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase));
                tips.SetToolTip(skinPicker,project==null?"Installed skins and the bundled demo. The list refreshes when opened.":project.Root+"\nChoose an installed skin or a sibling of the current skin. Existing project changes remain protected.");
            }
            finally{loading=previous;}
        }
        void ChooseSkin()
        {
            if(loading||project==null)return;var choice=skinPicker.SelectedItem as SkinChoice;
            if(choice==null||String.Equals(choice.Root,project.Root.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase))return;
            try{if(CanSwitch())LoadProject(choice.Root,project.Page);}
            finally{RefreshSkinChoices();}
        }
    }
}
