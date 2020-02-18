using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkynetServer.Extensions;
using SkynetServer.Services;
using System;
using System.Collections.Generic;

namespace SkynetServer
{
    internal static class Program
    {
        public static void Main()
        {
            CreateHostBuilder().Build().Run();
        }

        private static IHostBuilder CreateHostBuilder()
        {
            HostBuilder builder = new HostBuilder();
            builder.ConfigureAppConfiguration(config =>
            {
                // The appsettings.json file contained in this repository lacks some secrets that are necessary for production usage.
                // Our debug keypair "<Modulus>jKoWxmIf..." should be used in all client applications to connect to development servers.
                config.AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true);
            });
            builder.ConfigureServices((context, services) =>
            {
                services.ConfigureSkynet(context.Configuration);
                services.AddSingleton<MailingService>();
                services.AddSingleton<FirebaseService>();
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
    }
}
