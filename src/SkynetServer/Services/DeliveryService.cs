using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class DeliveryService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PacketService packets;
        private readonly ConnectionsService connections;
        private readonly FirebaseService firebase;
        private readonly IOptions<FcmOptions> fcmOptions;
        private readonly DatabaseContext database;

        public DeliveryService(IServiceProvider serviceProvider, PacketService packets, ConnectionsService connections,
            FirebaseService firebase, IOptions<FcmOptions> fcmOptions, DatabaseContext database)
        {
            this.serviceProvider = serviceProvider;
            this.packets = packets;
            this.connections = connections;
            this.firebase = firebase;
            this.fcmOptions = fcmOptions;
            this.database = database;
        }

        public void Unregister(Client client)
        {
            if (client.ChannelAction != ChannelAction.None) OnChannelActionChanged(client, 0, ChannelAction.None);
            if (client.Active) OnActiveChanged(client, active: false);
        }

        public void OnChannelActionChanged(Client client, long channelId, ChannelAction action)
        {
            if (client.Account == null) throw new ArgumentNullException($"{nameof(client)}.{nameof(Client.Account)}");

            if (client.FocusedChannelId != channelId)
                notifyChannel(client.FocusedChannelId, ChannelAction.None);

            notifyChannel(channelId, action);

            client.FocusedChannelId = channelId;
            client.ChannelAction = action;

            void notifyChannel(long _channelId, ChannelAction _action) => Task.Run(async () =>
            {
                using DatabaseContext ctx = new DatabaseContext();
                long[] members = await ctx.ChannelMembers.Where(m => m.ChannelId == _channelId).Select(m => m.AccountId).ToArrayAsync();
                if (!members.Contains(client.Account.AccountId))
                    return; // This is a protocol violation but throwing an exception would be useless in an async context.
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = _channelId;
                packet.AccountId = client.Account.AccountId;
                packet.Action = _action;
                await Task.WhenAll(clients
                    .Where(c => c.Account != null && members.Contains(c.Account.AccountId) && !ReferenceEquals(c, client))
                    .Select(c => c.SendPacket(packet)));
            });
        }

        public void OnActiveChanged(Client client, bool active)
        {
            if (client.Account == null) throw new ArgumentNullException($"{nameof(client)}.{nameof(Client.Account)}");

            bool notify = !clients.Any(c => c.Account != null && c.Account.AccountId == client.Account.AccountId
                && !ReferenceEquals(c, client) && c.Active);
            client.Active = active;

            Task.Run(async () =>
            {
                using DatabaseContext ctx = new DatabaseContext();
                Channel channel = await ctx.Channels.SingleAsync(c => c.OwnerId == client.Account.AccountId && c.ChannelType == ChannelType.AccountData);
                var packet = packets.New<P2BOnlineState>();
                packet.OnlineState = active ? OnlineState.Active : OnlineState.Inactive;
                packet.LastActive = DateTime.Now;
                packet.MessageFlags = MessageFlags.Unencrypted | MessageFlags.NoSenderSync;
                await CreateMessage(packet, channel, client.Account.AccountId);
            });
        }

        #region packet and message broadcast
        public async Task<Task> SendPacket(Packet packet, long accountId, Client exclude)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            IEnumerable<Task> send()
            {
                foreach (long sessionId in sessions)
                {
                    if (connections.TryGet(sessionId, out Client client) && !ReferenceEquals(client, exclude))
                    {
                        yield return client.Send(packet);
                    }
                }
            }

            return Task.WhenAll(send());
        }

        public async Task<Task> SendMessage(Message message, Client exclude)
        {
            bool isLoopback = message.MessageFlags.HasFlag(MessageFlags.Loopback);
            bool isNoSenderSync = message.MessageFlags.HasFlag(MessageFlags.NoSenderSync);

            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId
                    && !isLoopback || m.AccountId == message.SenderId
                    && !isNoSenderSync || m.AccountId != message.SenderId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            IEnumerable<Task> send()
            {
                foreach (long sessionId in sessions)
                {
                    if (connections.TryGet(sessionId, out Client client) && !ReferenceEquals(client, exclude))
                    {
                        yield return client.Send(message.ToPacket(client.AccountId));
                    }
                }
            }

            return Task.WhenAll(send());
        }

        public async Task SendPriorityMessage(Message message, Client exclude, Account excludeFcm)
        {
            long[] accounts;
            FcmOptions options = fcmOptions.Value;

            using (DatabaseContext ctx = new DatabaseContext())
            {
                accounts = await ctx.ChannelMembers.Where(m => m.ChannelId == message.ChannelId)
                    .Select(m => m.AccountId).ToArrayAsync();
            }

            await Task.WhenAll(accounts.Select(accountId => Task.Run(async () =>
            {
                bool found = false;

                await Task.WhenAll(clients.Where(c => c.Account != null && c.Account.AccountId == accountId)
                    .Select(async c =>
                    {
                        if (ReferenceEquals(c, exclude) || await c.SendPacket(message.ToPacket(accountId)))
                            found = true;
                    }));

                if ((!found && accountId != excludeFcm.AccountId) || options.NotifyAllDevices)
                {
                    using DatabaseContext ctx = new DatabaseContext();
                    Session[] sessions = await ctx.Sessions
                        .Where(s => s.AccountId == accountId && s.FcmToken != null
                            && (s.LastFcmMessage < s.LastConnected || options.NotifyForEveryMessage))
                        .ToArrayAsync();
                    foreach (Session session in sessions)
                    {
                        try
                        {
                            await firebase.SendAsync(session.FcmToken);

                            session.LastFcmMessage = DateTime.Now;
                            ctx.Entry(session).Property(s => s.LastFcmMessage).IsModified = true;
                            await ctx.SaveChangesAsync();
                            Console.WriteLine($"Successfully sent FCM message to {session.FcmToken.Remove(16)} last connected {session.LastConnected}");
                        }
                        catch (FirebaseAdmin.FirebaseException ex)
                        {
                            Console.WriteLine($"Failed to send FCM message to {session.FcmToken.Remove(16)}... {ex.Message}");
                            if (options.DeleteSessionOnError)
                            {
                                foreach (Client client in clients)
                                {
                                    if (client.Session != null && client.Session.AccountId == session.AccountId && client.Session.SessionId == session.SessionId)
                                        client.CloseConnection("Session deleted");
                                }
                                ctx.Sessions.Remove(session);
                                await ctx.SaveChangesAsync();
                            }
                        }
                    }
                }
            })));
        }
        #endregion

        #region channel and message restore
        public async Task<Task> SyncChannels(Client client, List<long> currentState)
        {
            Channel[] channels = await database.ChannelMembers.AsQueryable()
                .Where(m => m.AccountId == client.AccountId)
                .Join(database.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .ToArrayAsync().ConfigureAwait(false);

            List<Task> sendTasks = new List<Task>();

            foreach (Channel channel in channels)
            {
                if (!currentState.Contains(channel.ChannelId))
                {
                    // Notify client about new channels
                    var packet = packets.New<P0ACreateChannel>();
                    packet.ChannelId = channel.ChannelId;
                    packet.ChannelType = channel.ChannelType;
                    packet.OwnerId = channel.OwnerId ?? 0;
                    if (packet.ChannelType == ChannelType.Direct)
                        packet.CounterpartId = await database.ChannelMembers.AsQueryable()
                            .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != client.AccountId)
                            .Select(m => m.AccountId)
                            .SingleAsync().ConfigureAwait(false);
                    sendTasks.Add(client.Send(packet));
                    currentState.Add(channel.ChannelId);
                }
            }

            return Task.WhenAll(sendTasks);
        }

        public async Task SyncMessages(Client client, long lastMessageId)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            var query = database.ChannelMembers.AsQueryable()
                .Where(member => member.AccountId == client.AccountId)
                .Join(database.Messages, member => member.ChannelId, m => m.ChannelId, (member, m) => m)
                .Where(m => (m.MessageId > lastMessageId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == client.AccountId)
                    && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != client.AccountId))
                .Include(m => m.Dependencies).OrderBy(m => m.MessageId);

            await client.Send(query.AsAsyncEnumerable().Select(m => m.ToPacket(client.AccountId))).ConfigureAwait(false);

            // Independent service scope is disposed after await return
        }

        public async Task<Task> SyncMessages(long accountId, long channelId)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            async Task sendTo(Client client)
            {
                IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                var query = database.Messages.AsQueryable()
                    .Where(m => (m.ChannelId == channelId)
                        && (!m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == accountId)
                        && (!m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != accountId))
                    .Include(m => m.Dependencies).OrderBy(m => m.MessageId);

                await client.Send(query.AsAsyncEnumerable().Select(m => m.ToPacket(accountId))).ConfigureAwait(false);

                // Independent service scope is disposed after await return
            }

            IEnumerable<Task> send()
            {
                foreach (long sessionId in sessions)
                {
                    if (connections.TryGet(sessionId, out Client client))
                    {
                        yield return sendTo(client);
                    }
                }
            }

            return Task.WhenAll(send());
        }
        #endregion
    }
}
