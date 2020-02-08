using Microsoft.Extensions.DependencyInjection;
using SkynetServer.Database;
using SkynetServer.Network.Model;
using SkynetServer.Services;
using SkynetServer.Sockets;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client : IAsyncDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PacketService packets;
        private readonly PacketStream stream;
        private readonly CancellationToken ct;
        private readonly JobQueue<Packet> sendQueue;

        public Client(IServiceProvider serviceProvider, PacketService packets, PacketStream stream, CancellationToken ct)
        {
            this.serviceProvider = serviceProvider;
            this.packets = packets;
            this.stream = stream;
            this.ct = ct;
            sendQueue = new JobQueue<Packet>(packet =>
            {
                PacketBuffer buffer = new PacketBuffer();
                packet.WritePacket(buffer);
                return stream.WriteAsync(packet.Id, buffer.GetBuffer());
            });
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
            if (accountId == default) throw new ArgumentOutOfRangeException(nameof(accountId));
            if (sessionId == default) throw new ArgumentOutOfRangeException(nameof(sessionId));

            // Prevent accidential reauthentication of an authenticated client
            if (AccountId != default || SessionId != default) throw new InvalidOperationException();

            AccountId = accountId;
            SessionId = sessionId;
        }

        public async void Listen()
        {
            try
            {
                while (true)
                {
                    (byte id, ReadOnlyMemory<byte> content) = await stream.ReadAsync(ct).ConfigureAwait(false);
                    await HandlePacket(id, content).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // TODO: Handle disconnect
            }
        }

        public Task Send(Packet packet) => sendQueue.Insert(packet);
        public Task Send(ChannelMessage message) => sendQueue.Enqueue(message);
        public Task Send(IAsyncEnumerable<ChannelMessage> messages) => sendQueue.Enqueue(messages);

        private async ValueTask HandlePacket(byte id, ReadOnlyMemory<byte> content)
        {
            if (id >= this.packets.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id}");

            Packet prototype = this.packets.Packets[id];
            if (prototype == null || !prototype.Policies.HasFlag(PacketPolicies.Receive))
                throw new ProtocolException($"Cannot receive packet {id}");

            if (VersionCode == default && !prototype.Policies.HasFlag(PacketPolicies.Uninitialized))
                throw new ProtocolException($"Uninitialized client sent packet {id}");

            if (SessionId == default && !prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id}");

            if (SessionId != default && prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id}");

            Packet instance = prototype.Create();

            var buffer = new PacketBuffer(content);
            instance.ReadPacket(buffer);

            Console.WriteLine($"Starting to handle packet {instance}");

            using IServiceScope scope = serviceProvider.CreateScope();

            var handler = (IPacketHandler)ActivatorUtilities.CreateInstance(serviceProvider, this.packets.Handlers[id]);
            DatabaseContext database = scope.ServiceProvider.GetService<DatabaseContext>();
            PacketService packets = scope.ServiceProvider.GetService<PacketService>();
            DeliveryService delivery = scope.ServiceProvider.GetService<DeliveryService>();
            handler.Init(this, database, packets, delivery);

            await handler.Handle(instance).ConfigureAwait(false);
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                // TODO: Finish all pending handling operations
                await stream.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
