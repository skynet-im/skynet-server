using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Database.Entities;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network
{
    internal partial class Client : IVSLCallback, IPacketHandler
    {
        private readonly DeliveryService delivery;
        private readonly MailingService mailing;
        private readonly IOptions<ProtocolOptions> protocolOptions;

        private VSLServer socket;

        public Client(DeliveryService delivery, MailingService mailing, IOptions<ProtocolOptions> protocolOptions)
        {
            this.delivery = delivery;
            this.mailing = mailing;
            this.protocolOptions = protocolOptions;
        }

        public async Task SendPacket(Packet packet)
        {
            using (var buffer = PacketBuffer.CreateDynamic())
            {
                packet.WritePacket(buffer);
                bool success = await socket.SendPacketAsync(packet.Id, buffer.ToArray());
                if (success)
                    Console.WriteLine($"Successfully sent packet {packet}");
                else
                    Console.WriteLine($"Failed to send packet {packet}");
            }
        }

        public Account Account { get; private set; }
        public Session Session { get; private set; }

        public void OnInstanceCreated(VSLSocket socket)
        {
            this.socket = (VSLServer)socket;
            delivery.Register(this);
        }

        public Task OnConnectionEstablished()
        {
            Console.WriteLine($"Client connection established with version {socket.ConnectionVersionString}");
            return Task.CompletedTask;
        }

        public async Task OnPacketReceived(byte id, byte[] content)
        {
            if (id >= Packet.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id}");

            Packet prototype = Packet.Packets[id];
            if (prototype == null || !prototype.Policy.HasFlag(PacketPolicy.Receive))
                throw new ProtocolException($"Cannot receive packet {id}");

            if (Session == null && !prototype.Policy.HasFlag(PacketPolicy.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id}");

            if (Session != null && prototype.Policy.HasFlag(PacketPolicy.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id}");

            Packet instance = prototype.Create();

            using (var buffer = PacketBuffer.CreateStatic(content))
            {
                instance.ReadPacket(buffer);
            }

            Console.WriteLine($"Starting to handle packet {instance}");

            await instance.Handle(this);
        }

        public void OnConnectionClosed(ConnectionCloseReason reason, string message, Exception exception)
        {
            delivery.Unregister(this);
            Console.WriteLine("Connection closed: {0} {1}", message, exception);
            socket.Dispose();
        }
    }
}
