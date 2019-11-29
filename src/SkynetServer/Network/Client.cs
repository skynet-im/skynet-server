using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Database.Entities;
using SkynetServer.Network.Model;
using SkynetServer.Services;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client : IPacketHandler
    {
        private readonly DeliveryService delivery;
        private readonly MailingService mailing;
        private readonly IOptions<ProtocolOptions> protocolOptions;

        public Client(DeliveryService delivery, MailingService mailing, IOptions<ProtocolOptions> protocolOptions)
        {
            this.delivery = delivery;
            this.mailing = mailing;
            this.protocolOptions = protocolOptions;
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

        public void OnInstanceCreated()
        {
            delivery.Register(this);
        }

        public async Task OnPacketReceived(byte id, byte[] content)
        {
            if (id >= Packet.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id}");

            Packet prototype = Packet.Packets[id];
            if (prototype == null || !prototype.Policy.HasFlag(PacketPolicies.Receive))
                throw new ProtocolException($"Cannot receive packet {id}");

            if (Session == null && !prototype.Policy.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id}");

            if (Session != null && prototype.Policy.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id}");

            Packet instance = prototype.Create();

            var buffer = new Sockets.PacketBuffer(content);
            instance.ReadPacket(buffer);

            Console.WriteLine($"Starting to handle packet {instance}");

            await instance.Handle(this);
        }

        public void OnConnectionClosed(string message, Exception exception)
        {
            delivery.Unregister(this);
            Console.WriteLine("Connection closed: {0} {1}", message, exception);
        }
    }
}
