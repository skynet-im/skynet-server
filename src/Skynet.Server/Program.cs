using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skynet.Server.Commands;
using Skynet.Server.Extensions;
using Skynet.Server.Services;
using Skynet.Server.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Skynet.Server
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                Console.Write("SkynetServer is running in debug mode. Please enter your command: ");
                args = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine();
            }
#endif

            if (args.Length == 0)
            {
                CreateHostBuilder().Build().Run();
            }
            else
            {
                CreateCommandLineApplication().Execute(args);
            }
        }

        private static IHostBuilder CreateHostBuilder()
        {
            HostBuilder builder = new HostBuilder();

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                // The appsettings.json file contained in this repository lacks some secrets that are necessary for production usage.
                config.AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true);
            });

            builder.ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });

            builder.ConfigureServices((context, services) =>
            {
                services.ConfigureSkynet(context.Configuration);
                services.AddSingleton<ConfirmationMailService>();
                services.AddSingleton<IFirebaseService, FirebaseService>();
                services.AddSingleton<ConnectionsService>();
                services.AddSingleton<PacketService>();
                services.AddSingleton<NotificationService>();
                services.AddDatabaseContext(context.Configuration);
                services.AddScoped<DeliveryService>();
                services.AddScoped<MessageInjectionService>();
                services.AddScoped<ClientStateService>();
                services.AddHostedService<ListenerService>();
            });

            return builder;
        }

        private static CommandLineApplication CreateCommandLineApplication()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection()
                .AddSingleton(PhysicalConsole.Singleton)
                .AddLogging(logging => logging.AddConsole())
                .ConfigureSkynet(configuration)
                .AddSingleton<ConfirmationMailService>()
                .AddDatabaseContext(configuration)
                .BuildServiceProvider();

            var application = new CommandLineApplication<SkynetCommand>();
            application.Conventions.UseDefaultConventions();
            application.Conventions.UseConstructorInjection(services);
            application.MakeSuggestionsInErrorMessage = true;
            return application;
        }
    }
}
