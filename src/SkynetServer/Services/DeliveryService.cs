using Microsoft.EntityFrameworkCore;
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
        private readonly PacketService packets;
        private readonly ConnectionsService connections;
        private readonly FirebaseService firebase;
        private readonly IOptions<FcmOptions> fcmOptions;
        private readonly DatabaseContext database;

        public DeliveryService(PacketService packets, ConnectionsService connections,
            FirebaseService firebase, IOptions<FcmOptions> fcmOptions, DatabaseContext database)
        {
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
        public async Task SendPacket(Packet packet, long accountId, Client exclude)
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

            await Task.WhenAll(send()).ConfigureAwait(false);
        }

        public async Task SendMessage(Message message, Client exclude)
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

            await Task.WhenAll(send()).ConfigureAwait(false);
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
    }
}
