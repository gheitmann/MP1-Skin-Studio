using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MP1SkinStudio;

// Bounded console-only model benchmark; no forms, rendering or window access.
public static class ModelPerformanceProbe
{
    public static int Main(string[] args)
    {
        try
        {
            string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
            const int count=1600;var xml=new StringBuilder("<window><id>900</id><controls>");
            for(int i=0;i<count;i++)xml.Append("<control><type>label</type><id>").Append(10000+i).Append("</id><posX>").Append(i%1200).Append("</posX><posY>100</posY><label>Item ").Append(i).Append("</label></control>");
            xml.Append("</controls></window>");File.WriteAllText(Path.Combine(output,"main.xml"),xml.ToString());
            var p=new SkinProject(output);p.OpenPage("main.xml");var times=new double[5];
            for(int run=0;run<times.Length;run++)
            {
                var timer=Stopwatch.StartNew();p.OpenPage("main.xml");timer.Stop();times[run]=timer.Elapsed.TotalMilliseconds;
                if(p.Controls.Count!=count||p.Controls.Where((c,i)=>c.Ordinal!=i).Any())throw new Exception("Control mapping changed during loading.");
            }
            Console.WriteLine("Synthetic page controls: "+count);
            Console.WriteLine("Cached page-load milliseconds: "+String.Join(", ",times.Select(t=>t.ToString("F2",System.Globalization.CultureInfo.InvariantCulture))));
            Console.WriteLine("Median milliseconds: "+times.OrderBy(t=>t).ElementAt(2).ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
            Console.WriteLine("PASS: every control retains its source ordinal on all five loads; no original installed skin is used.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
