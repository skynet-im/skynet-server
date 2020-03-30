using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skynet.Network;
using Skynet.Protocol;
using Skynet.Protocol.Model;
using Skynet.Server.Database;
using Skynet.Server.Services;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Network
{
    internal sealed class Client : IClient
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ConnectionsService connections;
        private readonly PacketService packets;
        private readonly ILogger<Client> logger;

        private readonly PacketStream stream;
        private readonly CancellationToken ct;
        private readonly JobQueue<Packet> sendQueue;
        private readonly Task handler;

        public Client(IServiceProvider serviceProvider, ConnectionsService connections, PacketService packets, ILogger<Client> logger,
            PacketStream stream, CancellationToken ct)
        {
            this.serviceProvider = serviceProvider;
            this.connections = connections;
            this.packets = packets;
            this.logger = logger;

            this.stream = stream;
            this.ct = ct;
            sendQueue = new JobQueue<Packet>(packet =>
            {
                var buffer = new PacketBuffer();
                packet.WritePacket(buffer, PacketRole.Server);
                return stream.WriteAsync(packet.Id, buffer.GetBuffer());
            });
            handler = Listen();
        }

        public string ApplicationIdentifier { get; private set; }
        public int VersionCode { get; private set; }
        public long AccountId { get; private set; }
        public long SessionId { get; private set; }
        public bool SoonActive { get; set; }
        public bool Active { get; set; }
        public long FocusedChannelId { get; set; }
        public ChannelAction ChannelAction { get; set; }

        public event Action<IClient, Packet> PacketReceived;

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

        public Task Send(Packet packet) => sendQueue.Insert(packet);
        public Task Enqueue(Packet packet) => sendQueue.Enqueue(packet);
        public Task Enqueue(IAsyncEnumerable<ChannelMessage> messages) => sendQueue.Enqueue(messages);

        private async Task Listen()
        {
            while (!ct.IsCancellationRequested)
            {
                byte id;
                ReadOnlyMemory<byte> content;

                try
                {
                    bool success;
                    (success, id, content) = await stream.ReadAsync(ct).ConfigureAwait(false);

                    if (!success)
                    {
                        await DisposeAsync(false, true).ConfigureAwait(false);
                        logger.LogInformation("Session {0} disconnected", SessionId.ToString("x8"));
                        return;
                    }
                }
                catch (IOException ex)
                {
                    await DisposeAsync(false, true).ConfigureAwait(false);
                    logger.LogInformation(ex, "Session {0} lost connection", SessionId.ToString("x8"));
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Unexpected exception occurred while receiving a packet from session {0}", SessionId.ToString("x8"));
                    throw;
                }

                try
                {
                    await HandlePacket(id, content).ConfigureAwait(false);
                }
                catch (ProtocolException ex)
                {
                    logger.LogInformation(ex, "Invalid operation of session {0}", SessionId.ToString("x8"));
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Unexpected exception occurred while handling packet {0} of session {0}",
                        id.ToString("x2"), SessionId.ToString("x8"));
                    throw;
                }
            }
        }

        private async ValueTask HandlePacket(byte id, ReadOnlyMemory<byte> content)
        {
            if (id >= this.packets.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id}");

            Packet prototype = this.packets.Packets[id];
            if (prototype == null || !prototype.Policies.HasFlag(PacketPolicies.ClientToServer))
                throw new ProtocolException($"Cannot receive packet {id}");

            if (VersionCode == default && !prototype.Policies.HasFlag(PacketPolicies.Uninitialized))
                throw new ProtocolException($"Uninitialized client sent packet {id}");

            if (SessionId == default && !prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id}");

            if (SessionId != default && prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id}");

            Packet instance = prototype.Create();

            var buffer = new PacketBuffer(content);
            instance.ReadPacket(buffer, PacketRole.Server);

            Console.WriteLine($"Starting to handle packet {instance}");
            PacketReceived?.Invoke(this, instance);

            using IServiceScope scope = serviceProvider.CreateScope();

            var handler = (IPacketHandler)ActivatorUtilities.CreateInstance(serviceProvider, this.packets.Handlers[id]);
            DatabaseContext database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            PacketService packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            DeliveryService delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();
            handler.Init(this, database, packets, delivery);

            await handler.Handle(instance).ConfigureAwait(false);
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Gracefully shuts down this client's operations, updates its state and releases all unmanaged resources.
        /// This method waits for all handling operations to finish which can lead to dead locks.
        /// </summary>
        public ValueTask DisposeAsync() => DisposeAsync(true, true);

        public async ValueTask DisposeAsync(bool waitForHandling, bool updateState)
        {
            if (!disposedValue)
            {
                if (SessionId != default)
                    connections.TryRemove(SessionId, out _);

                if (waitForHandling)
                    await handler.ConfigureAwait(false);

                if (updateState && SessionId != default)
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var clientState = scope.ServiceProvider.GetRequiredService<ClientStateService>();
                    _ = await clientState.SetChannelAction(this, default, ChannelAction.None).ConfigureAwait(false);
                    _ = await clientState.SetActive(this, false).ConfigureAwait(false);
                }

                await sendQueue.DisposeAsync().ConfigureAwait(false);
                await stream.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
