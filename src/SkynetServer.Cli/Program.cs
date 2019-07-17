using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using SkynetServer.Cli.Commands;
using SkynetServer.Configuration;
using SkynetServer.Database;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SkynetServer.Cli
{
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            DatabaseContext.ConnectionString = configuration.Get<SkynetOptions>().DatabaseOptions.ConnectionString;

            if (Debugger.IsAttached)
            {
                Console.Write("Skynet CLI is running in debug mode. Please enter your command: ");
                args = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine();
            }
            return await CommandLineApplication.ExecuteAsync<SkynetCommand>(args);
        }
    }
}
