﻿using Microsoft.Extensions.DependencyInjection;
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
using System.Net.Sockets;
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
        private readonly JobQueue<Packet, bool> sendQueue;
        private readonly Task handler;
        private int disposing;

        public Client(IServiceProvider serviceProvider, ConnectionsService connections, PacketService packets, ILogger<Client> logger,
            PacketStream stream, CancellationToken ct)
        {
            this.serviceProvider = serviceProvider;
            this.connections = connections;
            this.packets = packets;
            this.logger = logger;

            this.stream = stream;
            this.ct = ct;
            sendQueue = new JobQueue<Packet, bool>(async (packet, dispose) =>
            {
                try
                {
                    using var buffer = new PacketBuffer();
                    packet.WritePacket(buffer, PacketRole.Server);
                    await stream.WriteAsync(packet.Id, buffer.GetBuffer()).ConfigureAwait(false);
                    logger.LogInformation("Successfully sent packet {0} to session {1}", packet, SessionId.ToString("x8"));
                }
                catch (IOException ex) when (ex.InnerException is SocketException socketEx)
                {
                    await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                    if (socketEx.SocketErrorCode == SocketError.TimedOut)
                        logger.LogInformation("Session {0} timed out", SessionId.ToString("x8"));
                    else
                        logger.LogInformation("Session {0} lost connection", SessionId.ToString("x8"));
                }
                catch (Exception ex)
                {
                    await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                    logger.LogCritical(ex, "Unexpected exception occurred while sending packet {0} to session {1}",
                        packet.Id.ToString("x2"), SessionId.ToString("x8"));
                }
                finally
                {
                    if (dispose) (packet as IDisposable)?.Dispose();
                }
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

        public Task Send(Packet packet) => sendQueue.Enqueue(packet, false, priority: true);
        public Task Send(IAsyncEnumerable<Packet> packets) => sendQueue.Enqueue(packets, false, priority: true);
        public Task Enqueue(Packet packet) => sendQueue.Enqueue(packet, false, priority: false);
        public Task Enqueue(ChannelMessage message) => sendQueue.Enqueue(message, true, priority: false);
        public Task Enqueue(IAsyncEnumerable<ChannelMessage> messages) => sendQueue.Enqueue(messages, true, priority: false);

        /// <summary>
        /// Gracefully shuts down this client's operations and releases all unmanaged resources. This method is thread safe.
        /// </summary>
        public async ValueTask DisposeAsync(bool unregister = true, bool waitForHandling = true, bool updateState = true)
        {
            if (Interlocked.Exchange(ref disposing, 1) == 0)
            {
                if (unregister && SessionId != default)
                    connections.TryRemove(SessionId, out _);

                if (waitForHandling)
                    await handler.ConfigureAwait(false);

                if (updateState && SessionId != default)
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var clientState = scope.ServiceProvider.GetRequiredService<ClientStateService>();
                    await clientState.StartSetChannelAction(this, default, ChannelAction.None).ConfigureAwait(false);
                    if (Active != false) await clientState.StartSetActive(this, false).ConfigureAwait(false);
                }

                await sendQueue.DisposeAsync().ConfigureAwait(false);
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task Listen()
        {
            connections.ClientConnected();

            while (true)
            {
                byte id;
                PoolableMemory content;

                try
                {
                    bool success;
                    (success, id, content) = await stream.ReadAsync(ct).ConfigureAwait(false);

                    if (!success)
                    {
                        await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                        logger.LogInformation("Session {0} disconnected", SessionId.ToString("x8"));
                        break;
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException socketEx)
                {
                    await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                    if (socketEx.SocketErrorCode == SocketError.TimedOut)
                        logger.LogInformation("Session {0} timed out", SessionId.ToString("x8"));
                    else
                        logger.LogInformation("Session {0} lost connection", SessionId.ToString("x8"));
                    break;
                }
                catch (OperationCanceledException)
                {
                    await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                    logger.LogInformation("Session {0} kicked by server", SessionId.ToString("x8"));
                    break;
                }
                catch (Exception ex)
                {
                    await DisposeAsync(waitForHandling: false).ConfigureAwait(false);
                    logger.LogCritical(ex, "Unexpected exception occurred while receiving a packet from session {0}", SessionId.ToString("x8"));
                    break;
                }

                try
                {
                    await HandlePacket(id, content).ConfigureAwait(false);
                }
                catch (ProtocolException ex)
                {
                    await DisposeAsync(waitForHandling: false);
                    logger.LogInformation(ex, "Invalid operation of session {0}", SessionId.ToString("x8"));
                    break;
                }
                catch (Exception ex)
                {
                    await DisposeAsync(waitForHandling: false);
                    logger.LogCritical(ex, "Unexpected exception occurred while handling packet {0} of session {0}",
                        id.ToString("x2"), SessionId.ToString("x8"));
                    break;
                }
                finally
                {
                    content.Return(false);
                }
            }

            connections.ClientDisconnected();
        }

        private async ValueTask HandlePacket(byte id, PoolableMemory content)
        {
            if (id >= packets.Packets.Length)
                throw new ProtocolException($"Invalid packet ID {id:x2}");

            Packet prototype = packets.Packets[id];
            if (prototype == null || !prototype.Policies.HasFlag(PacketPolicies.ClientToServer))
                throw new ProtocolException($"Cannot receive packet {id:x2}");

            if (VersionCode == default && !prototype.Policies.HasFlag(PacketPolicies.Uninitialized))
                throw new ProtocolException($"Uninitialized client sent packet {id:x2}");

            if (SessionId == default && !prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Unauthorized packet {id:x2}");

            if (SessionId != default && prototype.Policies.HasFlag(PacketPolicies.Unauthenticated))
                throw new ProtocolException($"Authorized clients cannot send packet {id:x2}");

            Packet instance = prototype.Create();

            try
            {
                using (var buffer = new PacketBuffer(content.Memory))
                    instance.ReadPacket(buffer, PacketRole.Server);

                logger.LogInformation("Starting to handle packet {0} from session {1}", instance, SessionId.ToString("x8"));
                PacketReceived?.Invoke(this, instance);

                using IServiceScope scope = serviceProvider.CreateScope();

                var handler = (IPacketHandler)ActivatorUtilities.CreateInstance(serviceProvider, this.packets.Handlers[id]);
                DatabaseContext database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                PacketService packets = scope.ServiceProvider.GetRequiredService<PacketService>();
                DeliveryService delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();
                handler.Init(this, database, packets, delivery);

                await handler.Handle(instance).ConfigureAwait(false);
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }
        }
    }
}
