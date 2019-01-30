using McMaster.Extensions.CommandLineUtils;
using SkynetServer.Cli.Commands;
using System;
using System.Diagnostics;

namespace SkynetServer.Cli
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                args = new string[] { };
                int result = CommandLineApplication.Execute<SkynetCommand>(args);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return result;
            }
            else return CommandLineApplication.Execute<SkynetCommand>(args);
        }
    }
}
