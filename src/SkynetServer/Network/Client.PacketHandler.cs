using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        public async Task SendMessages(long channelId, long lastMessage)
        {
            using DatabaseContext ctx = new DatabaseContext();
            foreach (Message message in ctx.Messages
                .Where(m => m.ChannelId == channelId && m.MessageId > lastMessage)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == Account.AccountId)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != Account.AccountId)
                .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
            {
                await Send(message.ToPacket(Account.AccountId));
            }
        }

        public async Task ForwardAccountChannels(DatabaseContext ctx, Account alice, Account bob)
        {
            Channel aliceChannel = await ctx.Channels.SingleAsync(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == alice.AccountId);
            Channel bobChannel = await ctx.Channels.SingleAsync(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == bob.AccountId);

            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = aliceChannel.ChannelId, AccountId = bob.AccountId });
            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = bobChannel.ChannelId, AccountId = alice.AccountId });
            await ctx.SaveChangesAsync();

            var createAlice = Packet.New<P0ACreateChannel>();
            createAlice.ChannelId = bobChannel.ChannelId;
            createAlice.ChannelType = ChannelType.AccountData;
            createAlice.OwnerId = bob.AccountId;
            await delivery.SendPacket(createAlice, alice.AccountId, null);

            var createBob = Packet.New<P0ACreateChannel>();
            createBob.ChannelId = aliceChannel.ChannelId;
            createBob.ChannelType = ChannelType.AccountData;
            createBob.OwnerId = alice.AccountId;
            await delivery.SendPacket(createBob, bob.AccountId, null);

            await Task.WhenAll(SendAllMessages(bobChannel, alice), SendAllMessages(aliceChannel, bob));
        }

        public async Task SendAllMessages(Channel channel, Account account)
        {
            using DatabaseContext ctx = new DatabaseContext();
            // TODO: Skip accounts with no connected user
            foreach (Message message in ctx.Messages
                .Where(m => m.ChannelId == channel.ChannelId)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == account.AccountId)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != account.AccountId)
                .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
            {
                await delivery.SendPacket(message.ToPacket(account.AccountId), account.AccountId, null);
            }
        }
    }
}
