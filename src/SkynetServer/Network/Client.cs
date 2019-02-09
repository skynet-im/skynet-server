using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network
{
    internal partial class Client : IVSLCallback, IPacketHandler
    {
        private VSLServer socket;
        private Account account;
        private Session session;

        public async Task SendPacket(Packet packet)
        {
            using (var buffer = PacketBuffer.CreateDynamic())
            {
                packet.WritePacket(buffer);
                bool success = await socket.SendPacketAsync(packet.Id, buffer.ToArray());
                if (!success)
                    Console.WriteLine($"Failed to send packet {packet.Id}");
            }
        }

        public void OnInstanceCreated(VSLSocket socket)
        {
            this.socket = (VSLServer)socket;
            ImmutableInterlocked.Update(ref Program.Clients, list => list.Add(this));
        }

        public Task OnConnectionEstablished() => Task.CompletedTask;

        public async Task OnPacketReceived(byte id, byte[] content)
        {
            if (id >= Packet.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id}");

            Packet packet = Packet.Packets[id];
            if (packet == null || !packet.Policy.HasFlag(PacketPolicy.Receive))
                throw new ProtocolException($"Cannot receive packet {id}");

            if (session == null && !packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id}");

            if (session != null && packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id}");

            Packet instance = packet.Create();

            using (var buffer = PacketBuffer.CreateStatic(content))
            {
                instance.ReadPacket(buffer);
            }

            await instance.Handle(this);
        }

        public void OnConnectionClosed(ConnectionCloseReason reason, string message, Exception exception)
        {
            ImmutableInterlocked.Update(ref Program.Clients, list => list.Remove(this));
            Console.WriteLine("Connection closed: {0} {1}", message, exception);
            socket.Dispose();
        }
    }
}
