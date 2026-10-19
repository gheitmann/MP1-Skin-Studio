using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MP1SkinStudio;
class ConditionInventory {
 static int Main(string[] args){try{var lines=new List<string>();var unknown=new Dictionary<string,int>();
 foreach(string theme in new[]{"","Christmas"})foreach(string page in new[]{"BasicHome.xml","settings.xml","myvideo.xml","mymusicsongs.xml","mypics.xml"}){
 var p=new SkinProject(args[0]);p.Theme=theme;PreparedSkinSession.Create(p,page);if(page=="BasicHome.xml")BasicHomePreview.Apply(p);else if(p.Controls.Any(c=>c.Get("type")=="listcontrol"))MediaFolderPreview.Apply(p,page=="mymusicsongs.xml"?"music":page=="mypics.xml"?"pictures":"video");else PlaybackPreview.Stopped(p.Editor.Preview);
 var state=new PreviewState(p);var items=p.Controls.Where(c=>state.Visible(c)==null).ToArray();lines.Add((theme.Length==0?"Default":theme)+"/"+page+": "+items.Length+" unresolved / "+p.Controls.Count+" controls");
 foreach(var c in items){string local=c.Get("visible"),terms=String.Join(" && ",(c.VisibilityTerms??new VisibilityTerm[0]).Select(t=>t.Expression));string expression=local+" [parents: "+terms+"]";int count;unknown.TryGetValue(expression,out count);unknown[expression]=count+1;lines.Add("  "+c.Source.RelativePath+" | "+c.Get("id")+" | "+c.Name+" | "+expression);}
 }
 lines.Add("\nUnique unresolved expressions:");foreach(var entry in unknown.OrderByDescending(p=>p.Value))lines.Add(entry.Value+" x "+entry.Key);Directory.CreateDirectory(args[1]);File.WriteAllLines(Path.Combine(args[1],"After.txt"),lines);Console.WriteLine(String.Join("\n",lines.Where(l=>!l.StartsWith("  "))));return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}

