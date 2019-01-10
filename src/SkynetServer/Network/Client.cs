using SkynetServer.Entities;
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

        public Task SendPacket(Packet packet)
        {
            using (var buffer = PacketBuffer.CreateDynamic())
            {
                packet.WritePacket(buffer);
                return socket.SendPacketAsync(packet.Id, buffer.ToArray());
            }
        }

        public void OnInstanceCreated(VSLSocket socket)
        {
            this.socket = (VSLServer)socket;
            ImmutableInterlocked.Update(ref Program.Clients, x => x.Add(this));
        }

        public Task OnConnectionEstablished() => Task.CompletedTask;

        public async Task OnPacketReceived(byte id, byte[] content)
        {
            if (id > 0x31)
            {
                socket.CloseConnection("Invalid packet id");
                return;
            }

            var packet = Packet.Packets[id];
            if (packet == null || !packet.Policy.HasFlag(PacketPolicy.Receive))
            {
                socket.CloseConnection("Invalid packet");
                return;
            }

            if (session == null && !packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
            {
                socket.CloseConnection("Unauthorized");
                return;
            }

            if (session != null && packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
            {
                socket.CloseConnection("Packet not allowed in current state");
                return;
            }

            using (var buffer = PacketBuffer.CreateStatic(content))
            {
                packet.ReadPacket(buffer);
            }

            await packet.Handle(this);
        }

        public void OnConnectionClosed(ConnectionCloseReason reason, string message, Exception exception)
        {
            ImmutableInterlocked.Update(ref Program.Clients, x => x.Remove(this));
            socket.Dispose();
        }
    }
}
