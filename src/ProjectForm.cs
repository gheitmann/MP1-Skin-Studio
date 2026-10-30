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
        bool projectLocationChanged;
        bool SessionNeedsSaving()
        {
            if(project==null)return false;
            if(PendingProperties()||projectLocationChanged)return true;
            string signature=ProjectFile.ContentSignature(project);
            if(signature==savedProjectSignature)return false;
            bool editorChanged=ProjectFile.EditorSignature(project)!=(currentProjectFile==null?initialEditorSignature:savedEditorSignature);
            return editorChanged||(project.Dirty&&ProjectFile.XmlSignature(project)!=exportedSignature);
        }
        void UpdateProjectCaption()
        {Text="MP1 Skin Studio · Live 1.3 development"+(currentProjectFile==null?"":" · "+Path.GetFileName(currentProjectFile))+(SessionNeedsSaving()?" *":"");ScheduleLivePreview();}
        void SaveSession(string path)
        {
            RequireAppliedProperties();
            ProjectFile.Save(project,path);projectLocationChanged=false;currentProjectFile=path;savedProjectSignature=ProjectFile.ContentSignature(project);savedEditorSignature=ProjectFile.EditorSignature(project);UpdateProjectCaption();
            ShowNotice("Editor project saved: "+path+". Source files in the skin directory remain unchanged.");
        }
        void ResumeSession(string path,string rootOverride=null)
        {
            var next=ProjectFile.Load(path,rootOverride);InstallProject(next,next.Page,path);
            ShowNotice("Project restored. Saved changes can be undone together.");
        }
        bool SaveEditorProject(bool saveAs)
        {
            if(project==null)return false;
            RequireAppliedProperties();
            if(!saveAs&&currentProjectFile!=null){SaveSession(currentProjectFile);return true;}
            using(var dialog=new SaveFileDialog{Filter="MP1 Skin Studio Project|*.mp1skinproj",DefaultExt="mp1skinproj",AddExtension=true,FileName=currentProjectFile==null?"My-Skin.mp1skinproj":Path.GetFileName(currentProjectFile),Title="Save editor project for continued editing"})
                if(dialog.ShowDialog(this)==DialogResult.OK){SaveSession(dialog.FileName);return true;}
            return false;
        }
        void OpenEditorProject()
        {
            using(var dialog=new OpenFileDialog{Filter="MP1 Skin Studio Project|*.mp1skinproj",Title="Open saved editor project"})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK||!CanSwitch())return;
                OpenSessionWithRecovery(dialog.FileName,ChooseProjectRoot);
            }
        }
        string ChooseProjectRoot(string reason)
        {
            using(var folder=new FolderBrowserDialog{Description=reason,ShowNewFolderButton=false})
                return folder.ShowDialog(this)==DialogResult.OK?folder.SelectedPath:null;
        }
        public void ResumeProjectFile(string path)
        {OpenSessionWithRecovery(path,ChooseProjectRoot);}
        public void OpenProjectOnStartup(string path)
        {
            QueueStartupProject(path,ChooseProjectRoot,error=>
                MessageBox.Show(this,error.Message,"Open editor project",MessageBoxButtons.OK,MessageBoxIcon.Warning));
        }
        internal void QueueStartupProject(string path,Func<string,string> chooseRoot,Action<Exception> reportError)
        {
            // Wait for an owned, visible editor before opening native dialogs.
            EventHandler shown=null;
            shown=(sender,args)=>
            {
                Shown-=shown;
                BeginInvoke(new Action(()=>TryOpenStartupProject(path,chooseRoot,reportError)));
            };
            Shown+=shown;
        }
        internal bool TryOpenStartupProject(string path,Func<string,string> chooseRoot,Action<Exception> reportError)
        {
            try
            {
                if(OpenSessionWithRecovery(path,chooseRoot))return true;
                ShowNotice("Project opening canceled. The current skin is still available; use Open Project to try again.");
            }
            catch(Exception error)
            {
                ShowNotice("Project was not opened. The current skin is still available; use Open Project to try again.");
                reportError(error);
            }
            return false;
        }
        internal bool OpenSessionWithRecovery(string path,Func<string,string> chooseRoot)
        {
            var next=ProjectFile.OpenWithRecovery(path,chooseRoot);
            if(next==null)return false;
            InstallProject(next,next.Page,path);
            ShowNotice("Project restored from "+next.Root+". Saved changes can be undone together."+(projectLocationChanged?" Save the project to remember this skin location.":""));
            return true;
        }
        void RunProjectUiSmoke(string directory,Action<bool,string> verify)
        {
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));fields["posX"].Text="260";Apply();
            verify(SessionNeedsSaving()&&Text.EndsWith("*"),"Window titles identify unsaved changes");
            string file=Path.Combine(directory,"UI-Session.mp1skinproj");SaveSession(file);
            verify(!SessionNeedsSaving()&&!Text.EndsWith("*")&&project.Dirty,"Project saving preserves the session and pending export changes");
            LoadProject(DemoRoot(),"demo.xml");ResumeSession(file);
            verify(project.Controls.First(c=>c.Get("description")=="Movies").Get("posX")=="260"&&!SessionNeedsSaving(),"Opening projects restores saved edits");
            string loadedPath=currentProjectFile;var originalProject=project;
            bool rejected=false;try{ResumeSession(Path.Combine(directory,"missing.mp1skinproj"));}catch{rejected=true;}
            verify(rejected&&project==originalProject&&currentProjectFile==loadedPath,"Failed project opening preserves the active session");
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));fields["width"].Text="600";Apply();
            verify(SessionNeedsSaving(),"Changes after project saving are detected as unsaved");
            undo.PerformClick();verify(!SessionNeedsSaving(),"Undoing to the saved state clears the change marker");
            undo.PerformClick();verify(!project.Dirty,"Restored project changes can be fully undone");
            LoadProject(DemoRoot(),"demo.xml");
        }
    }
}
