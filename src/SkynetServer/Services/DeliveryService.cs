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
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class DeliveryService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PacketService packets;
        private readonly ConnectionsService connections;
        private readonly IOptions<FcmOptions> fcmOptions;
        private readonly DatabaseContext database;

        public DeliveryService(IServiceProvider serviceProvider, PacketService packets,
            ConnectionsService connections, IOptions<FcmOptions> fcmOptions, DatabaseContext database)
        {
            this.serviceProvider = serviceProvider;
            this.packets = packets;
            this.connections = connections;
            this.fcmOptions = fcmOptions;
            this.database = database;
        }

        #region online state
        public async Task<Task> ChannelActionChanged(Client client, long channelId, ChannelAction action)
        {
            Task notifyOld = null;
            if (client.FocusedChannelId != channelId)
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = client.FocusedChannelId;
                packet.AccountId = client.AccountId;
                packet.Action = ChannelAction.None;
                notifyOld = await SendToChannel(packet, client.FocusedChannelId, client).ConfigureAwait(false);
            }

            Task notifyNew = null;
            if (channelId != default)
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = channelId;
                packet.AccountId = client.AccountId;
                packet.Action = action;
                notifyNew = await SendToChannel(packet, channelId, client).ConfigureAwait(false);
            }

            client.FocusedChannelId = channelId;
            client.ChannelAction = action;

            return (notifyOld, notifyNew) switch
            {
                (null, null) => Task.CompletedTask,
                (Task x, null) => x,
                (null, Task y) => y,
                (Task x, Task y) => Task.WhenAll(x, y)
            };
        }

        public async Task<Task> ActiveChanged(Client client, bool active)
        {
            if (client.Active == active)
                throw new InvalidOperationException("You must not call this method if the active state has not changed");

            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == client.AccountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            client.SoonActive = active;

            bool notify = true;

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out Client _client)
                    && !ReferenceEquals(_client, client)
                    && _client.SoonActive && (_client.Active || !active))
                {
                    // If going online: No need to notify if another session is already online and staying online
                    // If going offline: No need to notify if another session is online or coming online in the meantime
                    notify = false;
                }
            }

            client.Active = active;

            if (!notify) return Task.CompletedTask;

            long accountChannelId = await database.Channels.AsQueryable()
                .Where(c => c.OwnerId == client.AccountId && c.ChannelType == ChannelType.AccountData)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);

            var packet = packets.New<P2BOnlineState>();
            packet.OnlineState = active ? OnlineState.Active : OnlineState.Inactive;
            packet.LastActive = DateTime.Now;
            packet.MessageFlags = MessageFlags.Unencrypted | MessageFlags.NoSenderSync;
            
            var injector = new MessageInjectionService(database, packets);
            Message message = await injector.CreateMessage(packet, accountChannelId, client.AccountId).ConfigureAwait(false);
            return await SendMessage(message, client).ConfigureAwait(false);
        }
        #endregion

        #region packet and message broadcast
        public async Task<Task> SendToAccount(Packet packet, long accountId, Client exclude)
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

        public async Task<Task> SendToChannel(Packet packet, long channelId, Client exclude)
        {
            long[] sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == channelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s.SessionId)
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

        public async Task<Task> SendPriorityMessage(Message message, Client exclude, long excludeFcmAccountId)
        {
            var sessions = await database.ChannelMembers.AsQueryable()
                .Where(m => m.ChannelId == message.ChannelId)
                .Join(database.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => new { s.AccountId, s.SessionId })
                .OrderBy(s => s.AccountId)
                .ToArrayAsync().ConfigureAwait(false);

            List<Task> sendAndWaitTasks = new List<Task>();

            long lastAccountId = default;
            DelayedTask lastTimer = null;

            foreach (var session in sessions)
            {
                if (session.AccountId != lastAccountId)
                {
                    lastAccountId = session.AccountId;
                    lastTimer = new DelayedTask(() => SendFcmNotification(session.AccountId), fcmOptions.Value.PriorityMessageAckTimeout);
                    sendAndWaitTasks.Add(lastTimer.Task);
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
                    sendAndWaitTasks.Add(client.Send(message.ToPacket(client.AccountId)));
                }
            }

            return Task.WhenAll(sendAndWaitTasks);
        }

        private async Task SendFcmNotification(long accountId)
        {
            Session[] sessions;

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                sessions = await database.Sessions.AsQueryable()
                    .Where(s => s.AccountId == accountId && s.FcmToken != null
                        && (s.LastFcmMessage < s.LastConnected || fcmOptions.Value.NotifyForEveryMessage))
                    .ToArrayAsync().ConfigureAwait(false);
            }

            async Task process(Session session)
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var firebase = scope.ServiceProvider.GetRequiredService<FirebaseService>();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                try
                {
                    await firebase.SendAsync(session.FcmToken).ConfigureAwait(false);

                    session.LastFcmMessage = DateTime.Now;
                    database.Entry(session).Property(s => s.LastFcmMessage).IsModified = true;
                    await database.SaveChangesAsync().ConfigureAwait(false);
                    Console.WriteLine($"Successfully sent FCM message to {session.FcmToken.Remove(16)} last connected {session.LastConnected}");
                }
                catch (FirebaseAdmin.FirebaseException ex)
                {
                    Console.WriteLine($"Failed to send FCM message to {session.FcmToken.Remove(16)}... {ex.Message}");
                    if (fcmOptions.Value.DeleteSessionOnError)
                    {
                        // Kick client if connected to avoid conflicting information in RAM vs DB
                        if (connections.TryGet(session.SessionId, out Client client))
                        {
                            await client.DisposeAsync().ConfigureAwait(false);
                        }
                        database.Sessions.Remove(session);
                        await database.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }

            await Task.WhenAll(sessions.Select(s => process(s))).ConfigureAwait(false);
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
