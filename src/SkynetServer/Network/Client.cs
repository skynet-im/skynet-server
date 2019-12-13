using SkynetServer.Database.Entities;
using SkynetServer.Network.Model;
using SkynetServer.Services;
using SkynetServer.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        private readonly PacketStream stream;
        private readonly DeliveryService delivery;
        private readonly CancellationToken ct;

        public Client(DeliveryService delivery, PacketStream stream, CancellationToken ct)
        {
            this.delivery = delivery;
            this.stream = stream;
            this.ct = ct;
        }

        public async Task<bool> SendPacket(Packet packet)
        {
            var buffer = new PacketBuffer();
            packet.WritePacket(buffer);
            return true;
        }

        public string ApplicationIdentifier { get; private set; }
        public int VersionCode { get; private set; }
        public long AccountId { get; private set; }
        public long SessionId { get; private set; }
        public Account Account { get; private set; }
        public Session Session { get; private set; }
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
                (byte id, ReadOnlyMemory<byte> content) = await stream.ReadAsync(ct);

                if (id >= Packet.Packets.Length)
                    throw new ProtocolException($"Invalid packet ID {id}");

                Packet prototype = Packet.Packets[id];
                if (prototype == null || !prototype.Policies.HasFlag(PacketPolicies.Receive))
                    throw new ProtocolException($"Cannot receive packet {id}");

                if (Session == null && !prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                    throw new ProtocolException($"Unauthorized packet {id}");

                if (Session != null && prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                    throw new ProtocolException($"Authorized clients cannot send packet {id}");

                Packet instance = prototype.Create();

                var buffer = new PacketBuffer(content);
                instance.ReadPacket(buffer);

                Console.WriteLine($"Starting to handle packet {instance}");
            }
        }

        public void OnConnectionClosed(string message, Exception exception)
        {
            delivery.Unregister(this);
            Console.WriteLine("Connection closed: {0} {1}", message, exception);
        }
    }
}
