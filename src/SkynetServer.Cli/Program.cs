using McMaster.Extensions.CommandLineUtils;
using SkynetServer.Cli.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SkynetServer.Cli
{
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Console.Write("Skynet CLI is running in debug mode. Please enter your command: ");
                args = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine();
                int result = await CommandLineApplication.ExecuteAsync<SkynetCommand>(args);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return result;
            }
            else return await CommandLineApplication.ExecuteAsync<SkynetCommand>(args);
        }
    }
}
