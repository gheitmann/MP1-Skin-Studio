using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        string currentProjectFile,savedProjectSignature,exportedSignature,initialEditorSignature,savedEditorSignature;
        bool SessionNeedsSaving()
        {
            if(project==null)return false;
            string signature=ProjectFile.ContentSignature(project);
            if(signature==savedProjectSignature)return false;
            bool editorChanged=ProjectFile.EditorSignature(project)!=(currentProjectFile==null?initialEditorSignature:savedEditorSignature);
            return editorChanged||(project.Dirty&&ProjectFile.XmlSignature(project)!=exportedSignature);
        }
        void UpdateProjectCaption()
        {Text="MP1 Skin Studio · Prototyp 0.9"+(currentProjectFile==null?"":" · "+Path.GetFileName(currentProjectFile))+(SessionNeedsSaving()?" *":"");}
        void SaveSession(string path)
        {
            ProjectFile.Save(project,path);currentProjectFile=path;savedProjectSignature=ProjectFile.ContentSignature(project);savedEditorSignature=ProjectFile.EditorSignature(project);UpdateProjectCaption();
            ShowNotice("Editorprojekt gespeichert: "+path+". Quelldateien im Skinordner bleiben unverändert.");
        }
        void ResumeSession(string path,string rootOverride=null)
        {
            var next=ProjectFile.Load(path,rootOverride);InstallProject(next,next.Page,path);
            ShowNotice("Projekt wiederhergestellt. Die gespeicherten Änderungen lassen sich gemeinsam rückgängig machen.");
        }
        void SaveEditorProject(bool saveAs)
        {
            if(project==null)return;
            if(!saveAs&&currentProjectFile!=null){SaveSession(currentProjectFile);return;}
            using(var dialog=new SaveFileDialog{Filter="MP1 Skin Studio Projekt|*.mp1skinproj",DefaultExt="mp1skinproj",AddExtension=true,FileName=currentProjectFile==null?"Mein-Skin.mp1skinproj":Path.GetFileName(currentProjectFile),Title="Editorprojekt zum Weiterbearbeiten speichern"})
                if(dialog.ShowDialog(this)==DialogResult.OK)SaveSession(dialog.FileName);
        }
        void OpenEditorProject()
        {
            using(var dialog=new OpenFileDialog{Filter="MP1 Skin Studio Projekt|*.mp1skinproj",Title="Gespeichertes Editorprojekt öffnen"})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK||!CanSwitch())return;
                var location=ProjectFile.Inspect(dialog.FileName);string rootOverride=null;
                if(!ProjectFile.LocalRoot(location.Root)||!Directory.Exists(location.Root))
                {
                    using(var folder=new FolderBrowserDialog{Description="Der ursprüngliche Skinordner fehlt. Den passenden vollständigen Skinordner wählen.",ShowNewFolderButton=false})
                    {if(folder.ShowDialog(this)!=DialogResult.OK)return;rootOverride=folder.SelectedPath;}
                }
                ResumeSession(dialog.FileName,rootOverride);
            }
        }
        void RunProjectUiSmoke(string directory,Action<bool,string> verify)
        {
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));fields["posX"].Text="260";Apply();
            verify(SessionNeedsSaving()&&Text.EndsWith("*"),"Fenstertitel kennzeichnet noch nicht gesicherte Änderungen");
            string file=Path.Combine(directory,"UI-Sitzung.mp1skinproj");SaveSession(file);
            verify(!SessionNeedsSaving()&&!Text.EndsWith("*")&&project.Dirty,"Projekt speichern sichert Sitzung, ohne Exportänderungen zu verwerfen");
            LoadProject(DemoRoot(),"demo.xml");ResumeSession(file);
            verify(project.Controls.First(c=>c.Get("description")=="Filme").Get("posX")=="260"&&!SessionNeedsSaving(),"Projekt öffnen stellt gespeicherte Bearbeitung wieder her");
            string loadedPath=currentProjectFile;var originalProject=project;
            bool rejected=false;try{ResumeSession(Path.Combine(directory,"nicht-vorhanden.mp1skinproj"));}catch{rejected=true;}
            verify(rejected&&project==originalProject&&currentProjectFile==loadedPath,"Fehlgeschlagenes Öffnen erhält laufende Sitzung");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));fields["width"].Text="600";Apply();
            verify(SessionNeedsSaving(),"Änderung nach Projektspeicherung wird erneut als ungesichert erkannt");
            undo.PerformClick();verify(!SessionNeedsSaving(),"Undo auf gesicherten Stand entfernt die Änderungsmarkierung");
            undo.PerformClick();verify(!project.Dirty,"Wiederhergestellte Projektänderungen sind vollständig rückgängig");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
