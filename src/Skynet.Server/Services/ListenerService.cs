using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skynet.Network;
using Skynet.Server.Configuration;
using Skynet.Server.Extensions;
using Skynet.Server.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    internal sealed class ListenerService : IHostedService, IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<ListenerOptions> listenerOptions;
        private readonly ILogger<ListenerService> logger;
        private readonly CancellationTokenSource cts;

        private readonly Socket listener;
        private readonly IPEndPoint endPoint;
        private readonly X509Certificate2 certificate;

        public ListenerService(IServiceProvider serviceProvider, IOptions<ListenerOptions> listenerOptions, ILogger<ListenerService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.listenerOptions = listenerOptions;
            this.logger = logger;

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
            LoopAsync().CatchExceptions(logger);
            logger.LogInformation("Listening on {0}", endPoint);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cts.Cancel();
            listener.Dispose();
            return Task.CompletedTask;
        }

        private async Task LoopAsync()
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
                        AuthenticateAsync(client).CatchExceptions(logger);
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                // Server is shutting down
                logger.LogInformation(ex, "Listener exited");
            }
        }

        private async Task AuthenticateAsync(Socket socket)
        {
            NetworkStream networkStream = new NetworkStream(socket, ownsSocket: true);
            SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
            }
            catch (AuthenticationException ex)
            {
                logger.LogInformation(ex, "TLS authentication failed");
                await sslStream.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (IOException ex)
            {
                logger.LogInformation(ex, "TLS authentication aborted");
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
