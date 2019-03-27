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
            ImmutableInterlocked.Update(ref Program.Clients, list => list.Add(this));
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
            ImmutableInterlocked.Update(ref Program.Clients, list => list.Remove(this));
            Console.WriteLine("Connection closed: {0} {1}", message, exception);
            socket.Dispose();
        }
    }
}
