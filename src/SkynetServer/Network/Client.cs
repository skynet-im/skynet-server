using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network
{
    internal partial class Client : IPacketHandler
    {
        private readonly VSLServer socket;
        private Account account;
        private Session session;

        public Client(VSLServer socket)
        {
            this.socket = socket;
            socket.ConnectionEstablished += Socket_ConnectionEstablished;
            socket.PacketReceived += Socket_PacketReceived;
            socket.ConnectionClosed += Socket_ConnectionClosed;
        }

        public void Start()
        {
            socket.Start();
        }

        public void SendPacket(Packet packet)
        {
            using (var buffer = PacketBuffer.CreateDynamic())
            {
                packet.WritePacket(buffer);
                socket.SendPacketAsync(packet.Id, buffer.ToArray());
            }
        }

        private void Socket_ConnectionEstablished(object sender, EventArgs e)
        {

        }

        private void Socket_PacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Id > 0x31)
            {
                socket.CloseConnection("Invalid packet id", null);
                return;
            }

            var packet = Packet.Packets[e.Id];
            if (packet == null || !packet.Policy.HasFlag(PacketPolicy.Receive))
            {
                socket.CloseConnection("Invalid packet", null);
                return;
            }

            if (session == null && !packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
            {
                socket.CloseConnection("Unauthorized", null);
                return;
            }

            if (session != null && packet.Policy.HasFlag(PacketPolicy.Unauthenticated))
            {
                socket.CloseConnection("Packet not allowed in current state", null);
                return;
            }

            using (var buffer = PacketBuffer.CreateStatic(e.Content))
            {
                packet.ReadPacket(buffer);
            }

            packet.Handle(this);
        }

        private void Socket_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            socket.Dispose();
        }
    }
}
