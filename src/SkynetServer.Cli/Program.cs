using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkynetServer.Cli.Commands;
using SkynetServer.Extensions;
using SkynetServer.Services;
using System;
using System.Diagnostics;

namespace SkynetServer.Cli
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Console.Write("Skynet CLI is running in debug mode. Please enter your command: ");
                args = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine();
            }

            using CommandLineApplication cli = CreateCommandLineApplication();
            return cli.Execute(args);
        }

        private static CommandLineApplication CreateCommandLineApplication()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection()
                .AddSingleton(PhysicalConsole.Singleton)
                .ConfigureSkynet(configuration)
                .AddSingleton<MailingService>()
                .BuildServiceProvider();

            var application = new CommandLineApplication<SkynetCommand>();
            application.Conventions.UseDefaultConventions();
            application.Conventions.UseConstructorInjection(services);
            return application;
        }
    }
}
