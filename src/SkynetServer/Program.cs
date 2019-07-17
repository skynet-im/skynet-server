using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Network;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace SkynetServer
{
    internal static class Program
    {
        public static IConfiguration Configuration { get; private set; }
        public static ImmutableList<Client> Clients;

        public static void Main(string[] args)
        {
            // The appsettings.json file contained in this repository lacks some secrets that are necessary for production usage.
            // Our debug keypair "<Modulus>jKoWxmIf..." should be used in all client applications to connect to development servers.
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            DatabaseContext.ConnectionString = Configuration.Get<SkynetOptions>().DatabaseOptions.ConnectionString;

            Clients = ImmutableList.Create<Client>();

            CreateHostBuilder(args).Build().Run();
        }

#pragma warning disable IDE0060 // unused parameter args
        private static IHostBuilder CreateHostBuilder(string[] args)
#pragma warning restore IDE0060
        {
            HostBuilder builder = new HostBuilder();
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(Configuration);
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<FirebaseService>();
                services.AddSingleton<DeliveryService>();
                services.AddHostedService<ListenerService>();
            });
            return builder;
        }
    }
}
