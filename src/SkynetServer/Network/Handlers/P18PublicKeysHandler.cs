using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P18PublicKeysHandler : MessageHandler<P18PublicKeys>
    {
        private readonly MessageInjectionService injector;

        public P18PublicKeysHandler(MessageInjectionService injector)
        {
            this.injector = injector;
        }

        protected override async ValueTask<MessageSendStatus> Validate(P18PublicKeys packet)
        {
            if (packet.Dependencies.Count != 1)
                throw new ProtocolException($"Packet {nameof(P18PublicKeys)} must reference the matching private keys.");

            Dependency dep = packet.Dependencies[0];
            if (dep.AccountId != Client.AccountId)
                throw new ProtocolException($"The dependency of {nameof(P18PublicKeys)} to private keys must be specific for the sending account.");

            if (!await Database.Messages.AsQueryable()
                .AnyAsync(m => m.MessageId == dep.MessageId && m.PacketId == 0x17))
                throw new ProtocolException($"Could not find the referenced private keys for {nameof(P18PublicKeys)}.");

            return MessageSendStatus.Success;
        }

        protected override async ValueTask PostHandling(P18PublicKeys packet, Message message)
        {
            // Get all direct channels of Alice
            var channels = await Database.ChannelMembers.AsQueryable()
                .Where(m => m.AccountId == Client.AccountId)
                .Join(Database.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .Where(c => c.ChannelType == ChannelType.Direct)
                .ToListAsync().ConfigureAwait(false);

            foreach (Channel channel in channels)
            {
                long bobId = await Database.ChannelMembers.AsQueryable()
                    .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Client.AccountId)
                    .Select(m => m.AccountId).SingleAsync().ConfigureAwait(false);

                // Get Bob's latest public key packet in this channel and take Alice's new public key

                Message bobPublic = await Database.GetLatestPublicKey(bobId).ConfigureAwait(false);

                if (bobPublic == null) continue; // The server will create the DirectChannelUpdate when Bob sends his public key

                var directChannelUpdate = await injector
                    .CreateDirectChannelUpdate(channel, Client.AccountId, message, bobId, bobPublic).ConfigureAwait(false);
                _ = Delivery.SendMessage(directChannelUpdate, null);
            }
        }
    }
}
