using System;
using System.IO;
using System.Reflection;
using System.Xml;
using MediaPortal.Common.Utils;
using MediaPortal.GUI.Library;
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: CompatibleVersion("1.39.0.0")]
namespace MP1SkinStudio.Validation
{
 // Test-only: registered exclusively in the isolated MP client.
 public sealed class CoverageScenePlugin:IPlugin,ISetupForm
 {
  System.Windows.Forms.Timer observer;
  public string PluginName(){return "Skin Studio Coverage Fixture";}public string Description(){return "Isolated live coverage test scene";}public string Author(){return "MP1 Skin Studio";}public bool CanEnable(){return true;}public bool DefaultEnabled(){return true;}public bool HasSetup(){return false;}public void ShowPlugin(){}public int GetWindowId(){return 0;}public bool GetHome(out string name,out string image,out string focus,out string picture){name=image=focus=picture="";return false;}
  public void Start(){GUIWindow scene=new CoverageWindow();if(scene.Init()){GUIWindowManager.Add(ref scene);observer=new System.Windows.Forms.Timer{Interval=250};observer.Tick+=(s,e)=>Observe(scene);observer.Start();}else Log.Error("Skin Studio coverage fixture failed to load.");}public void Stop(){if(observer!=null)observer.Dispose();}
  static void Observe(GUIWindow scene)
  {
   try
   {
    string directory=Environment.GetEnvironmentVariable("MP1_SKIN_STUDIO_LIVE_DIR");if(String.IsNullOrEmpty(directory)||!scene.WindowLoaded||GUIWindowManager.ActiveWindowEx!=scene.GetID)return;
    var list=scene.GetControl(21103) as GUIListControl;var strip=scene.GetControl(21104) as GUIFilmstripControl;var doc=new XmlDocument();var root=doc.CreateElement("state");doc.AppendChild(root);root.SetAttribute("listItems",list==null?"-1":list.Count.ToString());root.SetAttribute("filmstripItems",strip==null?"-1":strip.Count.ToString());root.SetAttribute("rowHeight",list==null?"-1":list.ItemHeight.ToString());root.SetAttribute("iconHeight",list==null?"-1":list.ImageHeight.ToString());root.SetAttribute("selected",list==null?"-1":list.SelectedListItemIndex.ToString());root.SetAttribute("tick",DateTime.UtcNow.Ticks.ToString());
    string target=Path.Combine(directory,"coverage-state.xml"),temp=target+".tmp";doc.Save(temp);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);
   }catch(Exception ex){Log.Warn("Skin Studio fixture observer: {0}",ex.Message);}
  }
 }
 public sealed class CoverageWindow:GUIWindow
 {
  public CoverageWindow(){GetID=90110;}
  public override bool Init(){return Load(Path.Combine(GUIGraphicsContext.Skin,"studio.live.coverage.xml"));}
  protected override void OnPageLoad()
  {
   base.OnPageLoad();var list=GetControl(21103) as GUIListControl;if(list!=null&&list.Count==0)for(int i=0;i<6;i++)list.Add(new GUIListItem("Native row "+(i+1)));
   var strip=GetControl(21104) as GUIFilmstripControl;if(strip!=null&&strip.Count==0)for(int i=0;i<4;i++)strip.Add(new GUIListItem("Native item "+(i+1)));
  }
 }
}
