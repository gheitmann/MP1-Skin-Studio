using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MP1SkinStudio
{
    // Capture editor bytes on the UI thread, then copy files on a worker.
    public sealed class SkinCopyPlan
    {
        sealed class Edit {public string Path,OriginalHash;public byte[] Bytes;}
        readonly string root,page,theme;readonly Edit[] edits;
        SkinCopyPlan(SkinProject project,bool includeEdits)
        {
            root=Path.GetFullPath(project.Root).TrimEnd('\\','/');page=project.Page;theme=project.Theme;
            edits=includeEdits?project.Sources.Values.Where(s=>s.Changed).Select(s=>new Edit{Path=s.RelativePath,OriginalHash=ProjectFile.Digest(s.Preamble.Concat(s.Encoding.GetBytes(s.Original)).ToArray()),Bytes=s.Bytes()}).ToArray():new Edit[0];
        }
        public static SkinCopyPlan Capture(SkinProject project,bool includeEdits)
        {if(project==null)throw new ArgumentNullException("project");return new SkinCopyPlan(project,includeEdits);}
        public static void ValidateName(string name)
        {
            if(String.IsNullOrWhiteSpace(name)||name.Length>80||name!=name.Trim()||name.EndsWith(".")||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name=="."||name=="..")throw new IOException("Enter a skin name of 1 to 80 characters without path separators, trailing spaces or dots.");
            string first=name.Split('.')[0].ToUpperInvariant();
            if(new[]{"CON","PRN","AUX","NUL","CLOCK$"}.Contains(first)||Enumerable.Range(1,9).Any(i=>first=="COM"+i||first=="LPT"+i))throw new IOException("This name is reserved by Windows. Choose another skin name.");
        }
        static void NoLinks(string path)
        {
            for(var folder=new DirectoryInfo(path);folder!=null;folder=folder.Parent)
                if((folder.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked folders are not supported for skin copies: "+folder.FullName);
        }
        static string Inside(string folder,string relative)
        {
            string prefix=Path.GetFullPath(folder).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
            string full=Path.GetFullPath(Path.Combine(prefix,relative));
            if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("A skin file is outside the skin directory: "+relative);
            return full;
        }
        public string CopyTo(string parent,string name,CancellationToken cancel)
        {
            ValidateName(name);parent=Path.GetFullPath(parent).TrimEnd('\\','/');
            if(!Directory.Exists(parent))throw new DirectoryNotFoundException("Choose an existing destination folder.");
            NoLinks(root);NoLinks(parent);
            if(parent.Equals(root,StringComparison.OrdinalIgnoreCase)||parent.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Choose a destination outside the source skin.");
            string target=Inside(parent,name);
            if(Directory.Exists(target)||File.Exists(target))throw new IOException("A file or skin with this name already exists. Nothing was overwritten.");
            string stage=Inside(parent,".skinstudio-copy-"+Guid.NewGuid().ToString("N"));bool owned=false;
            try
            {
                cancel.ThrowIfCancellationRequested();Directory.CreateDirectory(stage);owned=true;
                var folders=new Stack<string>();folders.Push(root);var buffer=new byte[128*1024];
                while(folders.Count>0)
                {
                    cancel.ThrowIfCancellationRequested();string folder=folders.Pop();
                    if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked folders cannot be copied: "+folder);
                    string relative=folder.Length==root.Length?"":folder.Substring(root.Length+1);
                    string destination=relative.Length==0?stage:Inside(stage,relative);Directory.CreateDirectory(destination);
                    foreach(string file in Directory.EnumerateFiles(folder))
                    {
                        cancel.ThrowIfCancellationRequested();
                        if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked files cannot be copied: "+file);
                        string output=Inside(destination,Path.GetFileName(file));
                        using(var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))using(var copy=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                        {int count;while((count=input.Read(buffer,0,buffer.Length))>0){cancel.ThrowIfCancellationRequested();copy.Write(buffer,0,count);}}
                        File.SetLastWriteTimeUtc(output,File.GetLastWriteTimeUtc(file));
                    }
                    foreach(string child in Directory.EnumerateDirectories(folder))folders.Push(child);
                }
                foreach(var edit in edits)
                {
                    cancel.ThrowIfCancellationRequested();string file=Inside(stage,edit.Path);
                    if(!File.Exists(file)||ProjectFile.Digest(File.ReadAllBytes(file))!=edit.OriginalHash)throw new IOException("The source changed since it was loaded: "+edit.Path+". Reload it before copying editor changes.");
                    File.WriteAllBytes(file,edit.Bytes);
                }
                cancel.ThrowIfCancellationRequested();PreparedSkinSession.Create(new SkinProject(stage){Theme=theme},page);cancel.ThrowIfCancellationRequested();
                Directory.Move(stage,target);owned=false;return target;
            }
            finally{if(owned&&Directory.Exists(stage))Directory.Delete(stage,true);}
        }
    }
}
