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
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class MessageInjectionService
    {
        private readonly DatabaseContext database;
        private readonly DeliveryService delivery;

        public MessageInjectionService(DatabaseContext database, DeliveryService delivery)
        {
            this.database = database;
            this.delivery = delivery;
        }

        public async Task CreateDirectChannelUpdate(Channel channel, long aliceId, Message alicePublic, long bobId, Message bobPublic)
        {
            Message alicePrivate = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == alicePublic.MessageId)
                .Select(d => d.Message).SingleAsync().ConfigureAwait(false);

            Message bobPrivate = await database.MessageDependencies.AsQueryable()
                .Where(d => d.OwningMessageId == bobPublic.MessageId)
                .Select(d => d.Message).SingleAsync().ConfigureAwait(false);

            var update = Packet.New<P1BDirectChannelUpdate>();
            update.MessageFlags = MessageFlags.Unencrypted;
            update.Dependencies.Add(new Dependency(aliceId, alicePrivate.MessageId));
            update.Dependencies.Add(new Dependency(aliceId, bobPublic.MessageId));
            update.Dependencies.Add(new Dependency(bobId, bobPrivate.MessageId));
            update.Dependencies.Add(new Dependency(bobId, alicePublic.MessageId));
            await delivery.CreateMessage(update, channel, null).ConfigureAwait(false);
        }
    }
}
