using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class ClientStateService
    {
        private readonly ConnectionsService connections;
        private readonly PacketService packets;
        private readonly DeliveryService delivery;
        private readonly MessageInjectionService injector;
        private readonly DatabaseContext database;

        public ClientStateService(ConnectionsService connections, PacketService packets, 
            DeliveryService delivery, MessageInjectionService injector, DatabaseContext database)
        {
            this.connections = connections;
            this.packets = packets;
            this.delivery = delivery;
            this.injector = injector;
            this.database = database;
        }

        public async Task<Task> ChannelActionChanged(Client client, long channelId, ChannelAction action)
        {
            Task notifyOld = null;
            if (client.FocusedChannelId != channelId)
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = client.FocusedChannelId;
                packet.AccountId = client.AccountId;
                packet.Action = ChannelAction.None;
                notifyOld = await delivery.SendToChannel(packet, client.FocusedChannelId, client).ConfigureAwait(false);
            }

            Task notifyNew = null;
            if (channelId != default)
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = channelId;
                packet.AccountId = client.AccountId;
                packet.Action = action;
                notifyNew = await delivery.SendToChannel(packet, channelId, client).ConfigureAwait(false);
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

            Message message = await injector.CreateMessage(packet, accountChannelId, client.AccountId).ConfigureAwait(false);
            return await delivery.SendMessage(message, client).ConfigureAwait(false);
        }
    }
}
