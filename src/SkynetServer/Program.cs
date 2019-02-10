using Microsoft.Extensions.Configuration;
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

        private static void Main(string[] args)
        {
            // The appsettings.json file contained in this repository lacks some secrets that are necessary for production usage.
            // Our debug keypair "<Modulus>jKoWxmIf..." should be used in all client applications to connect to development servers.
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

        public static Task SendAllExcept(Packet packet, IEnumerable<long> accounts, Client exclude)
        {
            return Task.WhenAll(Clients
                .Where(c => c.Account != null && accounts.Contains(c.Account.AccountId) && !ReferenceEquals(c, exclude))
                .Select(c => c.SendPacket(packet)));
        }
    }
}
