using System;
using System.IO;
using MP1SkinStudio;

// Calls the non-UI suite directly, bypassing the editor program entry point.
public static class ConsoleCoreTests
{
    [STAThread]
    public static int Main(string[] args)
    {
        int result=SelfTests.Run(Path.GetFullPath(args[0]));
        Console.WriteLine(File.ReadAllText(Path.Combine(args[0],"test-results.txt")));
        return result;
    }
}
