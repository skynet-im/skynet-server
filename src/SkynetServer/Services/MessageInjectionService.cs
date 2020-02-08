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
    internal class MessageInjectionService
    {
        private readonly DatabaseContext database;
        private readonly PacketService packets;
        private readonly DeliveryService delivery;

        public MessageInjectionService(DatabaseContext database, PacketService packets, DeliveryService delivery)
        {
            this.database = database;
            this.packets = packets;
            this.delivery = delivery;
        }

        public async Task<Message> CreateMessage(ChannelMessage packet, Channel channel, long? senderId)
        {
            packet.ChannelId = channel.ChannelId;
            packet.MessageFlags |= MessageFlags.Unencrypted;

            Message message = new Message()
            {
                ChannelId = channel.ChannelId,
                SenderId = senderId,
                // TODO: Implement skip count
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                PacketId = packet.Id,
                PacketVersion = packet.PacketVersion,
                PacketContent = packet.PacketContent.IsEmpty ? null : packet.PacketContent.ToArray(),
                Dependencies = packet.Dependencies.ToDatabase()
            };

            database.Messages.Add(message);
            await database.SaveChangesAsync().ConfigureAwait(false);
            return message;
        }


        public async Task<Message> CreateDirectChannelUpdate(Channel channel, long aliceId, Message alicePublic, long bobId, Message bobPublic)
        {
            Message alicePrivate = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == alicePublic.MessageId)
                .Select(d => d.Message).SingleAsync().ConfigureAwait(false);

            Message bobPrivate = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == bobPublic.MessageId)
                .Select(d => d.Message).SingleAsync().ConfigureAwait(false);

            var update = packets.New<P1BDirectChannelUpdate>();
            update.MessageFlags = MessageFlags.Unencrypted;
            update.Dependencies.Add(new Dependency(aliceId, alicePrivate.MessageId));
            update.Dependencies.Add(new Dependency(aliceId, bobPublic.MessageId));
            update.Dependencies.Add(new Dependency(bobId, bobPrivate.MessageId));
            update.Dependencies.Add(new Dependency(bobId, alicePublic.MessageId));
            return await CreateMessage(update, channel, null).ConfigureAwait(false);
        }
    }
}
