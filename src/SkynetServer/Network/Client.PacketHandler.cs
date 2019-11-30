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
        public async Task SendMessages(List<(long channelId, long messageId)> currentState)
        {
            using DatabaseContext ctx = new DatabaseContext();
            Channel[] channels = ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c).ToArray();

            foreach (Channel channel in channels)
            {
                if (!currentState.Any(s => s.channelId == channel.ChannelId))
                {
                    // Notify client about new channels
                    var packet = Packet.New<P0ACreateChannel>();
                    packet.ChannelId = channel.ChannelId;
                    packet.ChannelType = channel.ChannelType;
                    packet.OwnerId = channel.OwnerId ?? 0;
                    if (packet.ChannelType == ChannelType.Direct)
                        packet.CounterpartId = await ctx.ChannelMembers
                            .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Account.AccountId)
                            .Select(m => m.AccountId).SingleAsync();
                    await SendPacket(packet);
                    currentState.Add((channel.ChannelId, 0));
                }
            }

            // Send messages from loopback channel
            Channel loopback = channels.Single(c => c.ChannelType == ChannelType.Loopback);
            long lastLoopbackMessage = currentState.Single(s => s.channelId == loopback.ChannelId).messageId;
            foreach (Message message in ctx.Messages
                .Where(m => m.ChannelId == loopback.ChannelId && m.MessageId > lastLoopbackMessage)
                .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
            {
                await SendPacket(message.ToPacket(Account.AccountId));
            }

            // Send messages from account data channels
            foreach (long channelId in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .Where(c => c.ChannelType == ChannelType.AccountData).Select(c => c.ChannelId))
            {
                long lastMessage = currentState.Single(s => s.channelId == channelId).messageId;
                await SendMessages(channelId, lastMessage);
            }

            // Send messages from direct channels
            foreach (long channelId in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .Where(c => c.ChannelType == ChannelType.Direct).Select(c => c.ChannelId))
            {
                long lastMessage = currentState.Single(s => s.channelId == channelId).messageId;
                await SendMessages(channelId, lastMessage);
            }

            await SendPacket(Packet.New<P0FSyncFinished>());
        }

        private async Task SendMessages(long channelId, long lastMessage)
        {
            using DatabaseContext ctx = new DatabaseContext();
            foreach (Message message in ctx.Messages
                .Where(m => m.ChannelId == channelId && m.MessageId > lastMessage)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == Account.AccountId)
                .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != Account.AccountId)
                .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
            {
                await SendPacket(message.ToPacket(Account.AccountId));
            }
        }

        private async Task ForwardAccountChannels(DatabaseContext ctx, Account alice, Account bob)
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

        private async Task SendAllMessages(Channel channel, Account account)
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
