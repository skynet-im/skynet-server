using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkynetServer.Configuration;
using SkynetServer.Network;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer
{
    internal static class Program
    {
        public static IConfiguration Configuration { get; private set; }
        public static ImmutableList<Client> Clients;

        private static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            HostBuilder builder = new HostBuilder();
            builder.ConfigureAppConfiguration(config =>
            {
                // The appsettings.json file contained in this repository lacks some secrets that are necessary for production usage.
                // Our debug keypair "<Modulus>jKoWxmIf..." should be used in all client applications to connect to development servers.
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            });
            builder.ConfigureServices(services =>
            {
                services.AddHostedService<ListenerService>();
            });
            return builder;
        }
    }
}
