using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Network;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class ListenerService : IHostedService, IDisposable
    {
        private readonly IOptions<ListenerOptions> listenerOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly CancellationTokenSource cts;
        private readonly SslListener listener;

        public ListenerService(IOptions<ListenerOptions> listenerOptions, IServiceProvider serviceProvider)
        {
            this.listenerOptions = listenerOptions;
            this.serviceProvider = serviceProvider;
            cts = new CancellationTokenSource();
            listener = new SslListener(listenerOptions.Value.TcpPort, listenerOptions.Value.CertificatePath);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            listener.Start();
            for (int i = 0; i < listenerOptions.Value.Parallelism; i++)
            {
                Loop();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cts.Cancel();
            listener.Dispose();
            return Task.CompletedTask;
        }

        private async void Loop()
        {
            while (!cts.IsCancellationRequested)
            {
                PacketStream stream = await listener.AcceptAsync().ConfigureAwait(false);
                ActivatorUtilities.CreateInstance<Client>(serviceProvider);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Dispose();
                    listener.Dispose();
                }

                disposedValue = true;
            }
        }

        // ~ListenerService()
        // {
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
