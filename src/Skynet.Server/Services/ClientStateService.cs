using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    internal sealed class ClientStateService
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

        public async Task StartSetChannelAction(IClient client, long channelId, ChannelAction action)
        {
            if (client.FocusedChannelId != default && client.FocusedChannelId != channelId)
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = client.FocusedChannelId;
                packet.AccountId = client.AccountId;
                packet.Action = ChannelAction.None;
                await delivery.StartSendToChannel(packet, client.FocusedChannelId, client).ConfigureAwait(false);
            }

            if (channelId != default && (channelId != client.FocusedChannelId || action != client.ChannelAction))
            {
                var packet = packets.New<P2CChannelAction>();
                packet.ChannelId = channelId;
                packet.AccountId = client.AccountId;
                packet.Action = action;
                await delivery.StartSendToChannel(packet, channelId, client).ConfigureAwait(false);
            }

            client.FocusedChannelId = channelId;
            client.ChannelAction = action;
        }

        public async Task StartSetActive(IClient client, bool active)
        {
            long[] sessions = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == client.AccountId)
                .Select(s => s.SessionId)
                .ToArrayAsync().ConfigureAwait(false);

            client.SoonActive = active;

            bool notify = true;

            foreach (long sessionId in sessions)
            {
                if (connections.TryGet(sessionId, out IClient _client)
                    && !ReferenceEquals(_client, client)
                    && _client.SoonActive && (_client.Active || !active))
                {
                    // If going online: No need to notify if another session is already online and staying online
                    // If going offline: No need to notify if another session is online or coming online in the meantime
                    notify = false;
                }
            }

            client.Active = active;

            if (!notify) return;

            long accountChannelId = await database.Channels.AsQueryable()
                .Where(c => c.OwnerId == client.AccountId && c.ChannelType == ChannelType.AccountData)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);

            var packet = packets.New<P2BOnlineState>();
            packet.OnlineState = active ? OnlineState.Active : OnlineState.Inactive;
            packet.LastActive = DateTime.Now;
            packet.MessageFlags = MessageFlags.Unencrypted | MessageFlags.NoSenderSync;

            Message message = await injector.CreateMessage(packet, accountChannelId, client.AccountId).ConfigureAwait(false);
            await delivery.StartSendMessage(message, client).ConfigureAwait(false);
        }
    }
}
