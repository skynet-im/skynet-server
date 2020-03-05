using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Network;
using SkynetServer.Sockets;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal sealed class ListenerService : IHostedService, IDisposable
    {
        private readonly IOptions<ListenerOptions> listenerOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly CancellationTokenSource cts;

        private readonly Socket listener;
        private readonly IPEndPoint endPoint;
        private readonly X509Certificate2 certificate;

        public ListenerService(IOptions<ListenerOptions> listenerOptions, IServiceProvider serviceProvider)
        {
            this.listenerOptions = listenerOptions;
            this.serviceProvider = serviceProvider;

            certificate = new X509Certificate2(listenerOptions.Value.CertificatePath);
            endPoint = new IPEndPoint(IPAddress.IPv6Any, listenerOptions.Value.Port);

            listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true
            };

            cts = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            listener.Bind(endPoint);
            listener.Listen(listenerOptions.Value.Backlog);
            Loop();

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
            try
            {
                while (true)
                {
                    Socket client = await listener.AcceptAsync().ConfigureAwait(false);
                    if (cts.IsCancellationRequested)
                    {
                        client.Dispose();
                        break;
                    }
                    else
                    {
                        Authenticate(client);
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                // Server is shutting down
            }
        }

        private async void Authenticate(Socket socket)
        {
            NetworkStream networkStream = new NetworkStream(socket, ownsSocket: true);
            SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls13, false).ConfigureAwait(false);
            }
            catch (AuthenticationException)
            {
                // TODO: Write failed authentication to logs
                await sslStream.DisposeAsync().ConfigureAwait(false);
                return;
            }

            if (cts.IsCancellationRequested)
            {
                await sslStream.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                PacketStream stream = new PacketStream(sslStream, leaveInnerStreamOpen: false);
                _ = ActivatorUtilities.CreateInstance<Client>(serviceProvider, stream, cts.Token);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            if (!disposedValue)
            {
                cts.Dispose();
                listener.Dispose();
                certificate.Dispose();

                disposedValue = true;
            }
        }
        #endregion
    }
}
