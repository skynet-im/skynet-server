using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Packets;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Services
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
        public async Task<IReadOnlyList<Task>> SendToAccount(Packet packet, long accountId, Client exclude)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task>();

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out Client client) && !ReferenceEquals(client, exclude))
                {
                    operations.Add(client.Send(packet));
                }
            }

            return operations;
        }

        public async Task<IReadOnlyList<Task>> SendToChannel(Packet packet, long channelId, Client exclude)
        {
            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == channelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task>();

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out Client client) && !ReferenceEquals(client, exclude))
                {
                    operations.Add(client.Send(packet));
                }
            }

            return operations;
        }

        public async Task<IReadOnlyList<Task>> SendMessage(Message message, Client exclude)
        {
            bool isLoopback = message.MessageFlags.HasFlag(MessageFlags.Loopback);
            bool isNoSenderSync = message.MessageFlags.HasFlag(MessageFlags.NoSenderSync);

            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId
                    && !isLoopback || m.AccountId == message.SenderId
                    && !isNoSenderSync || m.AccountId != message.SenderId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task>();

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out Client client) && !ReferenceEquals(client, exclude))
                {
                    operations.Add(client.Enqueue(message.ToPacket(client.AccountId)));
                }
            }

            return operations;
        }

        public async Task<IReadOnlyList<Task>> SendPriorityMessage(Message message, Client exclude, long excludeFcmAccountId)
        {
            var sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => new { s.AccountId, s.SessionId })
                .OrderBy(s => s.AccountId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task>();

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
                    operations.Add(lastTimer.Task);
                }

                // Declare a separate variable that can be safely captured and is not changed with the next iteration
                DelayedTask timer = lastTimer;

                void callback(Client client, Packet packet)
                {
                    if (packet is P22MessageReceived received && received.Dependencies[0].MessageId == message.MessageId)
                    {
                        timer.Cancel(); // Cancel timer because message has been received
                        client.PacketReceived -= callback;
                    }
                }

                if (connections.TryGet(session.SessionId, out Client client) && !ReferenceEquals(client, exclude))
                {
                    client.PacketReceived += callback;
                    operations.Add(client.Enqueue(message.ToPacket(client.AccountId)));
                }
            }

            return operations;
        }
        #endregion

        #region channel and message restore
        public async Task<Task> SyncChannels(Client client, List<long> channelState, long lastMessageId)
        {
            return await ExecuteSync(
                client,
                database => database.ChannelMembers.AsQueryable()
                .Where(member => member.AccountId == client.AccountId)
                .Join(database.Messages, member => member.ChannelId, m => m.ChannelId, (member, m) => m)
                .Where(m => (m.MessageId > lastMessageId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == client.AccountId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != client.AccountId)),
                channelState: channelState
            ).ConfigureAwait(false);
        }

        public async Task<Task> SyncMessages(Client client, long channelId, long after, long before, ushort maxCount)
        {
            // The JOIN with ChannelMembers prevents unauthorized access

            return await ExecuteSync(
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
            ).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Task>> SyncMessages(long accountId, long channelId)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            var operations = new List<Task<Task>>();

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out Client client))
                {
                    // The JOIN with ChannelMembers prevents unauthorized access

                    operations.Add(ExecuteSync(
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

            return await Task.WhenAll(operations).ConfigureAwait(false);
        }

        private async Task<Task> ExecuteSync(
            Client client,
            Func<DatabaseContext, IQueryable<Message>> queryBuilder,
            List<long> channelState = null,
            ushort maxCount = default)
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
                await StartSyncChannels(client, channelState).ConfigureAwait(false);

            async Task executeScoped()
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                IQueryable<Message> query = queryBuilder(database).Include(m => m.Dependencies).OrderBy(m => m.MessageId);
                if (maxCount != default)
                    query = query.Take(maxCount);

                Task send = client.Enqueue(query.AsAsyncEnumerable().Select(m => m.ToPacket(client.AccountId)));
                _ = client.Enqueue(packets.New<P0FSyncFinished>());
                await send.ConfigureAwait(false);

                // Independent service scope is disposed after await returns
            }

            return executeScoped();
        }

        private async Task StartSyncChannels(Client client, IReadOnlyList<long> currentState)
        {
            // This query selects all channels of the client's account and performs LEFT JOIN on other channel members
            var query = from m in database.ChannelMembers.AsNoTracking().AsQueryable().Where(m => m.AccountId == client.AccountId)
                        join c in database.Channels
                            on m.ChannelId equals c.ChannelId
                        join other in database.ChannelMembers.AsQueryable().Where(m => m.AccountId != client.AccountId)
                            on c.ChannelId equals other.ChannelId into grouping
                        from other in grouping.DefaultIfEmpty()
                        select new { c.ChannelId, c.ChannelType, c.OwnerId, c.CreationTime, other.AccountId };

            var channels = await query.ToArrayAsync().ConfigureAwait(false);

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
                        packet.CounterpartId = channel.AccountId;
                    _ = client.Send(packet);
                }
            }
        }
        #endregion
    }
}
