using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkynetServer.Configuration;
using SkynetServer.Entities;
using SkynetServer.Network;
using System;
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

        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Clients = ImmutableList.Create<Client>();
            VSLListener listener = CreateListener();
            listener.Start();

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private static VSLListener CreateListener()
        {
            VslConfig config = Configuration.Get<SkynetConfig>().VslConfig;
            IPEndPoint[] endPoints = {
                new IPEndPoint(IPAddress.Any, config.TcpPort),
                new IPEndPoint(IPAddress.IPv6Any, config.TcpPort)
            };

            SocketSettings settings = new SocketSettings()
            {
                LatestProductVersion = config.LatestProductVersion,
                OldestProductVersion = config.OldestProductVersion,
                RsaXmlKey = config.RsaXmlKey,
                CatchApplicationExceptions = false
            };

            return new VSLListener(endPoints, settings, () => new Client());
        }
    }
}
