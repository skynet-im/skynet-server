using SkynetServer.Network.Model;
using SkynetServer.Services;
using SkynetServer.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client : IAsyncDisposable
    {
        private readonly PacketService packets;
        private readonly DeliveryService delivery;

        private readonly PacketStream stream;
        private readonly CancellationToken ct;

        public Client(PacketService packets, DeliveryService delivery, PacketStream stream, CancellationToken ct)
        {
            this.packets = packets;
            this.delivery = delivery;
            this.stream = stream;
            this.ct = ct;
        }

        public string ApplicationIdentifier { get; private set; }
        public int VersionCode { get; private set; }
        public long AccountId { get; private set; }
        public long SessionId { get; private set; }
        public bool Active { get; set; }
        public long FocusedChannelId { get; set; }
        public ChannelAction ChannelAction { get; set; }

        public void Initialize(string applicationIdentifier, int versionCode)
        {
            ApplicationIdentifier = applicationIdentifier;
            VersionCode = versionCode;
        }

        public void Authenticate(long accountId, long sessionId)
        {
            AccountId = accountId;
            SessionId = sessionId;
        }

        public async void Listen()
        {
            while (true)
            {
                (byte id, ReadOnlyMemory<byte> content) = await stream.ReadAsync(ct).ConfigureAwait(false);

                if (id >= packets.Packets.Length)
                    throw new ProtocolException($"Invalid packet ID {id}");

                Packet prototype = packets.Packets[id];
                if (prototype == null || !prototype.Policies.HasFlag(PacketPolicies.Receive))
                    throw new ProtocolException($"Cannot receive packet {id}");

                if (SessionId == default && !prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                    throw new ProtocolException($"Unauthorized packet {id}");

                if (SessionId != default && prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                    throw new ProtocolException($"Authorized clients cannot send packet {id}");

                Packet instance = prototype.Create();

                var buffer = new PacketBuffer(content);
                instance.ReadPacket(buffer);

                Console.WriteLine($"Starting to handle packet {instance}");

                // TODO: Create scope and handler
            }
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                await stream.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
