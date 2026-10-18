using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MediaPortal.Drawing.Layouts;
using MediaPortal.GUI.Library;
using Size = MediaPortal.Drawing.Size;

class NativeProbe
{
    static readonly List<string> report=new List<string>();
    static void Check(bool value,string name){if(!value)throw new Exception(name);report.Add("PASS  "+name);}
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            report.Add("Native MP1-1.39-Core-Pruefung (Original-Assembly aus offiziellem Release)");
            foreach(bool horizontal in new[]{false,true})
            {
                var group=new GUIGroup(0){XPosition=100,YPosition=200,Width=300,Height=100};
                var first=new GUIGroup(0){Width=80,Height=30};var second=new GUIGroup(0){Width=80,Height=30};
                group.AddControl(first);group.AddControl(second);
                var layout=new StackLayout(7,horizontal?Orientation.Horizontal:Orientation.Vertical);
                var size=layout.Measure(group,new Size(300,100));group.Width=(int)size.Width;group.Height=(int)size.Height;layout.Arrange(group);
                Check(first.XPosition==100&&first.YPosition==200,"Erstes Kind am Gruppenursprung ("+horizontal+")");
                Check(second.XPosition==(horizontal?187:100)&&second.YPosition==(horizontal?200:237),"Reihenfolge und Abstand ("+horizontal+")");
                Check(size.Width==(horizontal?174:80)&&size.Height==(horizontal?30:74),"Messgroesse inklusive abschliessendem Abstand ("+horizontal+")");
                first.Visibility=Visibility.Collapsed;size=layout.Measure(group,new Size(300,100));group.Width=(int)size.Width;group.Height=(int)size.Height;layout.Arrange(group);
                Check(second.XPosition==100&&second.YPosition==200,"Collapsed reserviert keinen Platz ("+horizontal+")");
                first.Visibility=Visibility.Hidden;size=layout.Measure(group,new Size(300,100));group.Width=(int)size.Width;group.Height=(int)size.Height;layout.Arrange(group);
                Check(second.XPosition==(horizontal?187:100)&&second.YPosition==(horizontal?200:237),"Hidden bleibt in statischem StackLayout enthalten ("+horizontal+")");
            }
            report.Add("10 native Pruefungen bestanden. Kein vollstaendiger Client-/Direct3D-Bildvergleich.");
            File.WriteAllLines(args[0],report);return 0;
        }
        catch(Exception ex){report.Add("FAIL  "+ex);File.WriteAllLines(args[0],report);return 1;}
    }
}
