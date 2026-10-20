using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using MP1SkinStudio;
class NativeNavigationMath
{
    static int Main(string[] args)
    {
        try
        {
            string install=Path.GetDirectoryName(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{string f=Path.Combine(install,new AssemblyName(e.Name).Name+".dll");return File.Exists(f)?Assembly.LoadFrom(f):null;};
            var assembly=Assembly.LoadFrom(args[0]);var type=assembly.GetType("MediaPortal.GUI.Library.GUIControl",true);
            var native=type.GetMethod("CalcBearing",BindingFlags.NonPublic|BindingFlags.Static);
            var distance=type.GetMethod("CalcDistance",BindingFlags.NonPublic|BindingFlags.Static);
            var editor=typeof(SkinProject).GetMethod("NavigationBearing",BindingFlags.NonPublic|BindingFlags.Static);
            var pointType=native.GetParameters()[0].ParameterType;
            Console.WriteLine(pointType.FullName+" constructors: "+String.Join("; ",Array.ConvertAll(pointType.GetConstructors(),c=>c.ToString())));
            int count=0;var origin=new Point(37,-19);
            for(int x=-400;x<=400;x+=10)for(int y=-400;y<=400;y+=10)
            {
                var target=new Point(origin.X+x,origin.Y+y);
                object a=Activator.CreateInstance(pointType,new object[]{(double)origin.X,(double)origin.Y}),b=Activator.CreateInstance(pointType,new object[]{(double)target.X,(double)target.Y});
                double actual=(double)native.Invoke(null,new object[]{a,b}),preview=(double)editor.Invoke(null,new object[]{origin,target});
                if(actual!=preview)throw new Exception("Bearing mismatch for "+x+","+y);
                double nativeDistance=(double)distance.Invoke(null,new object[]{a,b});
                if(nativeDistance!=Math.Round(Math.Sqrt((double)x*x+(double)y*y)))throw new Exception("Distance mismatch");count++;
            }
            string result="PASS "+count+" native bearing and rounded-distance comparisons against installed original Core.dll "+assembly.GetName().Version+". Reflection invoked only static math; no window, client process or runtime settings were created.";
            File.WriteAllText(args[1],result+Environment.NewLine);Console.WriteLine(result);return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
