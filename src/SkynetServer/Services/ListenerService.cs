using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Services
{
    internal class ListenerService : IHostedService
    {
        private readonly IOptions<VslOptions> vslOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly VSLListener listener;

        public ListenerService(IOptions<VslOptions> vslOptions, IServiceProvider serviceProvider)
        {
            this.vslOptions = vslOptions;
            this.serviceProvider = serviceProvider;
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
            VslOptions config = vslOptions.Value;
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

            return new VSLListener(endPoints, settings, () => ActivatorUtilities.CreateInstance<Client>(serviceProvider));
        }
    }
}
