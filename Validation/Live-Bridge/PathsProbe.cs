using System;
using System.IO;
using MediaPortal.Configuration;
class PathsProbe
{
 static int Main(string[] args)
 {
  bool valid=true;foreach(var name in new[]{"Config","Plugins","Log","Skin","Language","Database","Thumbs","Cache"})
  {string path=Config.GetFolder((Config.Dir)Enum.Parse(typeof(Config.Dir),name));Console.WriteLine(name+"="+path);if(!Path.GetFullPath(path).StartsWith(Path.GetFullPath(args[0]).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))valid=false;}
  return valid?0:1;
 }
}
