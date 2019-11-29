using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Sockets
{
    internal sealed class SslListener : IDisposable
    {
        private readonly Socket listener;
        private readonly IPEndPoint endPoint;
        private readonly X509Certificate certificate;

        public SslListener(int port, string certificatePath)
        {
            certificate = new X509Certificate(certificatePath);
            endPoint = new IPEndPoint(IPAddress.IPv6Any, port);

            listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true
            };
        }

        public void Start()
        {
            listener.Bind(endPoint);
            listener.Listen(64);
        }

        public async Task<PacketStream> AcceptAsync()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(SslListener));

            // TODO: Handle execeptions to prevent memory leaks
            // TODO: This implementation is vulnerable to slowloris attacks an TLS handshake will take time to time out
            Socket client = await listener.AcceptAsync().ConfigureAwait(false);
            SslStream sslStream = new SslStream(new NetworkStream(client), leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls13, false).ConfigureAwait(false);
            return new PacketStream(sslStream);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    certificate.Dispose();
                    listener.Dispose();
                }

                disposedValue = true;
            }
        }

        // ~SslListener()
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
