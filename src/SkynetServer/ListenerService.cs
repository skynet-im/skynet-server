using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SkynetServer.Configuration;
using SkynetServer.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer
{
    internal class ListenerService : IHostedService
    {
        private readonly IConfiguration configuration;
        private readonly VSLListener listener;

        public ListenerService(IConfiguration config)
        {
            configuration = config;
            listener = CreateListener();
            listener.CacheCapacity = 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            listener.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            listener.Stop();
            return Task.CompletedTask;
        }

        private VSLListener CreateListener()
        {
            VslConfig config = configuration.Get<SkynetConfig>().VslConfig;
            IPEndPoint[] endPoints = {
                new IPEndPoint(IPAddress.Any, config.TcpPort),
                new IPEndPoint(IPAddress.IPv6Any, config.TcpPort)
            };

            SocketSettings settings = new SocketSettings()
            {
                LatestProductVersion = config.LatestProductVersion,
                OldestProductVersion = config.OldestProductVersion,
                RsaXmlKey = config.RsaXmlKey,
                CatchApplicationExceptions = !Debugger.IsAttached
            };

            return new VSLListener(endPoints, settings, () => new Client());
        }
    }
}
