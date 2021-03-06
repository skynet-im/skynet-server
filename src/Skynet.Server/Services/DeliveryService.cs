﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skynet.Model;
using Skynet.Protocol;
using Skynet.Protocol.Packets;
using Skynet.Server.Configuration;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Network;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    internal sealed class DeliveryService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PacketService packets;
        private readonly ConnectionsService connections;
        private readonly NotificationService notification;
        private readonly IOptions<FcmOptions> fcmOptions;
        private readonly IOptions<ProtocolOptions> protocolOptions;
        private readonly DatabaseContext database;

        public DeliveryService(IServiceProvider serviceProvider, PacketService packets, ConnectionsService connections,
            NotificationService notification, IOptions<FcmOptions> fcmOptions, IOptions<ProtocolOptions> protocolOptions, DatabaseContext database)
        {
            this.serviceProvider = serviceProvider;
            this.packets = packets;
            this.connections = connections;
            this.notification = notification;
            this.fcmOptions = fcmOptions;
            this.protocolOptions = protocolOptions;
            this.database = database;
        }

        #region packet and message broadcast
        public async Task StartSendToAccount(Packet packet, long accountId, IClient exclude)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient client) && !ReferenceEquals(client, exclude))
                {
                    _ = client.Send(packet);
                }
            }
        }

        public async Task StartSendToAccount(IAsyncEnumerable<Packet> packets, long accountId, IClient exclude)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient client) && !ReferenceEquals(client, exclude))
                {
                    _ = client.Send(packets);
                }
            }
        }

        public async Task StartSendToChannel(Packet packet, long channelId, IClient exclude)
        {
            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == channelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient client) && !ReferenceEquals(client, exclude))
                {
                    _ = client.Send(packet);
                }
            }
        }

        public async Task StartSendMessage(Message message, IClient exclude)
        {
            bool isLoopback = message.MessageFlags.HasFlag(MessageFlags.Loopback);
            bool isNoSenderSync = message.MessageFlags.HasFlag(MessageFlags.NoSenderSync);

            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId
                    && (!isLoopback || m.AccountId == message.SenderId)
                    && (!isNoSenderSync || m.AccountId != message.SenderId))
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient client) && !ReferenceEquals(client, exclude))
                {
                    _ = client.Enqueue(message.ToPacket(packets, client.AccountId));
                }
            }
        }

        public async Task StartSendPriorityMessage(Message message, IClient exclude, long excludeFcmAccountId)
        {
            var sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => new { s.AccountId, s.SessionId })
                .OrderBy(s => s.AccountId)
                .ToArrayAsync().ConfigureAwait(false);

            long lastAccountId = default;
            DelayedTask lastTimer = null;

            foreach (var session in sessions)
            {
                if (session.AccountId != lastAccountId)
                {
                    lastAccountId = session.AccountId;
                    lastTimer = new DelayedTask(() =>
                    {
                        if (session.AccountId != excludeFcmAccountId)
                            return notification.SendFcmNotification(session.AccountId);
                        else
                            return Task.CompletedTask;
                    }, fcmOptions.Value.PriorityMessageAckTimeout);
                }

                // Declare a separate variable that can be safely captured and is not changed with the next iteration
                DelayedTask timer = lastTimer;

                void callback(IClient client, Packet packet)
                {
                    if (packet is P22MessageReceived received && received.Dependencies[0].MessageId == message.MessageId)
                    {
                        timer.Cancel(); // Cancel timer because message has been received
                        client.PacketReceived -= callback;
                    }
                }

                if (connections.TryGet(session.SessionId, out IClient client) && !ReferenceEquals(client, exclude))
                {
                    client.PacketReceived += callback;
                    _ = client.Enqueue(message.ToPacket(packets, client.AccountId));
                }
            }
        }
        #endregion

        #region channel and message restore
        public Task StartSyncChannels(IClient client, List<long> channelState, long lastMessageId)
        {
            return StartSync(
                client,
                database => database.ChannelMembers.AsQueryable()
                .Where(member => member.AccountId == client.AccountId)
                .Join(database.Messages, member => member.ChannelId, m => m.ChannelId, (member, m) => m)
                .Where(m => (m.MessageId > lastMessageId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == client.AccountId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != client.AccountId)),
                channelState: channelState
            );
        }

        public Task StartSyncMessages(IClient client, long channelId, long after, long before, ushort maxCount)
        {
            // The JOIN with ChannelMembers prevents unauthorized access

            return StartSync(
                client,
                database => database.ChannelMembers.AsQueryable()
                    .Where(member => member.AccountId == client.AccountId)
                    .Join(database.Messages, member => member.ChannelId, m => m.ChannelId, (member, m) => m)
                    .Where(m => (channelId == default || m.ChannelId == channelId)
                        && m.MessageId > after
                        && m.MessageId < before
                        && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == client.AccountId)
                        && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != client.AccountId)),
                maxCount: maxCount
            );
        }

        public async Task StartSyncMessages(long accountId, long channelId)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task>();

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient client))
                {
                    // The JOIN with ChannelMembers prevents unauthorized access

                    operations.Add(StartSync(
                        client,
                        database => database.ChannelMembers.AsQueryable()
                            .Where(member => member.AccountId == client.AccountId)
                            .Join(database.Messages, member => member.ChannelId, m => m.ChannelId, (member, m) => m)
                            .Where(m => (m.ChannelId == channelId)
                                && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == accountId)
                                && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != accountId))
                    ));
                }
            }

            await Task.WhenAll(operations).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts a sync operation by enqueuing all packets and packet sources asynchronously. This method is thread safe.
        /// </summary>
        private async Task StartSync(
            IClient client,
            Func<DatabaseContext, IQueryable<Message>> queryBuilder,
            List<long> channelState = null,
            ushort maxCount = default)
        {
            // Create an independent service scope right away to avoid concurrent access to the same DatabaseContext
            // No using-blocks here because this scope is accessed from an async void and disposed manually!
            IServiceScope scope = serviceProvider.CreateScope();
            var packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeliveryService>>();

            try
            {
                var start = packets.New<P0BSyncStarted>();

                if (protocolOptions.Value.CountMessagesBeforeSync)
                {
                    IQueryable<Message> countQuery = queryBuilder(database);
                    if (maxCount != default)
                        countQuery = countQuery.Take(maxCount);

                    start.MinCount = await countQuery.CountAsync().ConfigureAwait(false);
                }
                else
                {
                    start.MinCount = -1;
                }

                _ = client.Send(start);

                if (channelState != null)
                    await StartSyncChannels(client, channelState, database).ConfigureAwait(false);
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            async void executeScoped()
            {
                try
                {
                    IQueryable<Message> query = queryBuilder(database).Include(m => m.Dependencies).OrderBy(m => m.MessageId);
                    if (maxCount != default)
                        query = query.Take(maxCount);

                    Task send = client.Enqueue(query.AsAsyncEnumerable().Select(m => m.ToPacket(packets, client.AccountId)));
                    _ = client.Enqueue(packets.New<P0FSyncFinished>());
                    await send.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "An unexpected exception occurred while delivering messages to client {0}", client.SessionId.ToString("x8"));
                }
                finally
                {
                    scope.Dispose();
                }
            }

            executeScoped();
        }

        private async Task StartSyncChannels(IClient client, IReadOnlyList<long> currentState, DatabaseContext database)
        {
            Channel[] channels = await database.ChannelMembers.AsQueryable()
                .Where(m => m.AccountId == client.AccountId)
                .Join(database.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (var channel in channels)
            {
                if (!currentState.Contains(channel.ChannelId))
                {
                    // Notify client about new channels
                    var packet = packets.New<P0ACreateChannel>();
                    packet.ChannelId = channel.ChannelId;
                    packet.ChannelType = channel.ChannelType;
                    packet.OwnerId = channel.OwnerId ?? 0;
                    packet.CreationTime = channel.CreationTime;
                    if (packet.ChannelType == ChannelType.Direct)
                        packet.CounterpartId = channel.OwnerId == client.AccountId ? channel.CounterpartId.Value : channel.OwnerId.Value;
                    _ = client.Send(packet);
                }
            }

            foreach (long channelId in currentState)
            {
                if (!channels.Any(c => c.ChannelId == channelId))
                {
                    // Notify client about deleted channels
                    var packet = packets.New<P0DDeleteChannel>();
                    packet.ChannelId = channelId;
                    _ = client.Send(packet);
                }
            }
        }
        #endregion
    }
}
