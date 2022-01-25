// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;

static int main()
{
    string[] args = Environment.GetCommandLineArgs();
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Argument needed.");
        return 1;
    }
    string arg = Environment.GetCommandLineArgs()[1];

    string url = string.Join(" ", args[1..]);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"This is what I got: {url}\n");

    string[] lines = args[1..];
    string fileName = "/tmp/lines.txt";

    File.WriteAllLines(fileName, lines);
    DateTime lmod = File.GetLastWriteTime(fileName);

    using (Process myProcess = new Process())
    {
        myProcess.StartInfo.UseShellExecute = true;
        // You can start any process, HelloWorld is a do-nothing example.
        myProcess.StartInfo.FileName = "/bin/leafpad";
        myProcess.StartInfo.Arguments = fileName;
        myProcess.StartInfo.CreateNoWindow = true;
        myProcess.Start();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Waiting for process to exit ...");
        myProcess.WaitForExit();
        Console.WriteLine("\n");
    }

    if (File.GetLastWriteTime(fileName) > lmod)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("File Changed....");
        Console.ResetColor();
        Console.WriteLine("I could upload the file now or do anything else.");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("File didn't change!");
        Console.ResetColor();
        Console.WriteLine("Nothing to do. I could just delete it.");
    }

    return 0;
}

main();