using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using MediaPortal.Drawing.Layouts;
using MediaPortal.GUI.Library;
using Size = MediaPortal.Drawing.Size;
using Thickness = MediaPortal.Drawing.Thickness;
using HorizontalAlignment = MediaPortal.Drawing.HorizontalAlignment;
using VerticalAlignment = MediaPortal.Drawing.VerticalAlignment;

class NativeGridProbe
{
    static readonly List<string> report=new List<string>();
    static void Check(bool value,string name){if(!value)throw new Exception(name);report.Add("PASS  "+name);}
    static GridLayout Parse(string text){return (GridLayout)typeof(GUIControlFactory).GetMethod("ParseLayout",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{text});}
    static GUIGroup Group(){var g=new GUIGroup(0){XPosition=100,YPosition=200,Width=300,Height=100};for(int i=0;i<5;i++)g.AddControl(new GUIGroup(0){Width=80,Height=30});return g;}
    static Size Arrange(GUIGroup g,GridLayout layout){var size=layout.Measure(g,new Size(300,100));g.Width=(int)size.Width;g.Height=(int)size.Height;layout.Arrange(g);return size;}
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            report.Add("Native GridLayout checks against original MP1 Core.dll and GUIControlFactory");
            report.Add("Core assembly: "+typeof(GUIGroup).Assembly.GetName().Version);
            var g=Group();var size=Arrange(g,Parse("GridLayout(2,0,5,7)"));
            Check(size.Width==165&&size.Height==104,"Two columns measure 165 x 104 without trailing spacing");
            Check(g.Children[2].XPosition==100&&g.Children[2].YPosition==237,"Horizontal fill wraps the third child");
            Check(g.Children[4].YPosition==274,"Incomplete final row retains grid height");
            g=Group();Arrange(g,Parse("GridLayout(2,0,5,7,1)"));
            Check(g.Children[1].YPosition==237&&g.Children[3].XPosition==185&&g.Children[3].YPosition==200,"Titan numeric orientation fills columns first");
            g=Group();size=Arrange(g,Parse("GridLayout(99,2,5,7,Vertical)"));
            Check(size.Width==250&&g.Children[4].XPosition==270,"Positive row count overrides explicit columns");
            g=Group();size=Arrange(g,Parse("GridLayout(1,0,999)"));
            Check(size.Height==150&&g.Children[1].YPosition==230,"XML factory ignores the third token when only three are supplied");
            g=Group();g.Children[0].Visibility=Visibility.Collapsed;size=Arrange(g,Parse("GridLayout(2,0,5,7)"));
            Check(g.Children[1].XPosition==100&&g.Children[2].YPosition==237&&size.Height==104,"Collapsed children keep row count but skip horizontal advance");
            g=Group();g.Children[0].Visibility=Visibility.Hidden;Arrange(g,Parse("GridLayout(2,0,5,7)"));
            Check(g.Children[1].XPosition==185,"Hidden children retain grid slots");
            g=Group();g.Margin=new Thickness(10,20,30,40);g.Children[0].Width=160;g.Children[0].Height=60;
            g.Children[1].HorizontalAlignment=HorizontalAlignment.Right;g.Children[1].VerticalAlignment=VerticalAlignment.Bottom;
            g.Children[2].HorizontalAlignment=HorizontalAlignment.Stretch;g.Children[2].VerticalAlignment=VerticalAlignment.Stretch;
            Arrange(g,Parse("GridLayout(2,0,5,7)"));
            Check(g.Children[1].XPosition==355&&g.Children[1].YPosition==250,"Margins and right/bottom alignment match preview");
            Check(g.Children[2].XPosition==110&&g.Children[2].YPosition==287&&g.Children[2].Width==160&&g.Children[2].Height==60,"Stretch uses maximum cell dimensions");
            report.Add("10 native checks passed. No full client/Direct3D image comparison.");
            File.WriteAllLines(args[0],report);return 0;
        }
        catch(Exception ex){report.Add("FAIL  "+ex);File.WriteAllLines(args[0],report);return 1;}
    }
}
