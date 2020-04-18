using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol;
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
    internal sealed class MessageInjectionService
    {
        private readonly DatabaseContext database;
        private readonly PacketService packets;

        public MessageInjectionService(DatabaseContext database, PacketService packets)
        {
            this.database = database;
            this.packets = packets;
        }

        public async Task<Message> CreateMessage(ChannelMessage packet, long channelId, long? senderId)
        {
            packet.ChannelId = channelId;
            packet.MessageFlags |= MessageFlags.Unencrypted;

            Message message = new Message()
            {
                ChannelId = channelId,
                SenderId = senderId,
                // TODO: Implement skip count
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                PacketId = packet.Id,
                PacketVersion = packet.PacketVersion,
                PacketContent = packet.PacketContent,
                Dependencies = packet.Dependencies.ToDatabase()
            };

            database.Messages.Add(message);
            await database.SaveChangesAsync().ConfigureAwait(false);
            return message;
        }


        public async Task<Message> CreateDirectChannelUpdate(long channelId, long aliceId, long alicePublicId, long bobId, long bobPublicId)
        {
            long alicePrivateId = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == alicePublicId)
                .Select(d => d.MessageId).SingleAsync().ConfigureAwait(false);

            long bobPrivateId = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == bobPublicId)
                .Select(d => d.MessageId).SingleAsync().ConfigureAwait(false);

            var update = packets.New<P1BDirectChannelUpdate>();
            update.MessageFlags = MessageFlags.Unencrypted;
            update.Dependencies.Add(new Dependency(aliceId, alicePrivateId));
            update.Dependencies.Add(new Dependency(aliceId, bobPublicId));
            update.Dependencies.Add(new Dependency(bobId, bobPrivateId));
            update.Dependencies.Add(new Dependency(bobId, alicePublicId));
            return await CreateMessage(update, channelId, null).ConfigureAwait(false);
        }

        public async Task<Message> CreateDeviceList(long accountId)
        {
            if (accountId == default) 
                throw new ArgumentOutOfRangeException(nameof(accountId), accountId, $"{nameof(accountId)} must not be zero.");

            long loopbackId = await database.Channels.AsQueryable()
                .Where(c => c.OwnerId == accountId && c.ChannelType == ChannelType.Loopback)
                .Select(c => c.ChannelId).SingleAsync().ConfigureAwait(false);

            List<SessionInformation> sessionInformation = await database.Sessions.AsQueryable()
                .Where(s => s.AccountId == accountId)
                .Select(s => new SessionInformation(s.AccountId, s.CreationTime, s.ApplicationIdentifier))
                .ToListAsync().ConfigureAwait(false);

            var deviceList = packets.New<P29DeviceList>();
            deviceList.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
            deviceList.Sessions = sessionInformation;
            return await CreateMessage(deviceList, loopbackId, accountId).ConfigureAwait(false);
        }
    }
}
