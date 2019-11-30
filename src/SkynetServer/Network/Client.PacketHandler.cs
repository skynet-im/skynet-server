﻿using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        public async Task HandleMessage(ChannelMessage packet)
        {
            if (!packet.MessageFlags.AreValid(packet.RequiredFlags, packet.AllowedFlags))
                throw new ProtocolException($"Invalid MessageFlags{packet.MessageFlags} for content packet ID {packet.Id}");

            if (packet.MessageFlags.HasFlag(MessageFlags.Unencrypted)
                && await packet.HandleMessage(this) != MessageSendStatus.Success)
                    return; // Not all messages can be saved, some return MessageSendError other than Success
            
            using (DatabaseContext ctx = new DatabaseContext())
            {
                Channel channel = await ctx.Channels.SingleOrDefaultAsync(c => c.ChannelId == packet.ChannelId);
                if (channel == null)
                    throw new ProtocolException("Attempted to send a message to a non existent channel");

                switch (channel.ChannelType)
                {
                    case ChannelType.Loopback:
                    case ChannelType.AccountData:
                    case ChannelType.ProfileData:
                        if (channel.OwnerId != Account.AccountId)
                            throw new ProtocolException("Attempted to send a message to a foreign channel");
                        break;
                    case ChannelType.Direct:
                        if (!await ctx.ChannelMembers.AnyAsync(m => m.ChannelId == packet.ChannelId && m.AccountId == Account.AccountId))
                            throw new ProtocolException("Attempted to send a message to a foreign channel");
                        break;
                    case ChannelType.Group:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentException($"Invalid value {channel.ChannelType} for enum {nameof(ChannelType)}");
                }
            }

            Message entity = new Message
            {
                ChannelId = packet.ChannelId,
                SenderId = Account.AccountId,
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                PacketId = packet.Id,
                PacketVersion = packet.PacketVersion,
                PacketContent = packet.PacketContent.IsEmpty ? null : packet.PacketContent.ToArray(),
            };

            entity = await DatabaseHelper.AddMessage(entity, packet.Dependencies.ToDatabase());

            var response = Packet.New<P0CChannelMessageResponse>();
            response.ChannelId = packet.ChannelId;
            response.TempMessageId = packet.MessageId;
            response.StatusCode = MessageSendStatus.Success;
            response.MessageId = entity.MessageId;
            // TODO: Implement skip count
            response.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);
            await SendPacket(response);

            using (DatabaseContext ctx = new DatabaseContext())
            {
                packet.SenderId = Account.AccountId;
                packet.MessageId = entity.MessageId;
                packet.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);

                if (packet.Id == 0x20)
                    await delivery.SendPriorityMessage(entity, exclude: this, excludeFcm: Account);
                else
                    await delivery.SendMessage(entity, exclude: this);
            }

            await packet.PostHandling(this, entity);
        }

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

        private async Task CreateDirectChannelUpdate(DatabaseContext ctx, Channel channel, long aliceId, Message alicePublic, long bobId, Message bobPublic)
        {
            Message alicePrivate = await ctx.MessageDependencies
                .Where(d => d.OwningChannelId == alicePublic.ChannelId && d.OwningMessageId == alicePublic.MessageId)
                .Select(d => d.Message).SingleAsync();

            Message bobPrivate = await ctx.MessageDependencies
                .Where(d => d.OwningChannelId == bobPublic.ChannelId && d.OwningMessageId == bobPublic.MessageId)
                .Select(d => d.Message).SingleAsync();

            var update = Packet.New<P1BDirectChannelUpdate>();
            update.MessageFlags = MessageFlags.Unencrypted;
            update.Dependencies.Add(new Dependency(aliceId, alicePrivate.ChannelId, alicePrivate.MessageId));
            update.Dependencies.Add(new Dependency(aliceId, bobPublic.ChannelId, bobPublic.MessageId));
            update.Dependencies.Add(new Dependency(bobId, bobPrivate.ChannelId, bobPrivate.MessageId));
            update.Dependencies.Add(new Dependency(bobId, alicePublic.ChannelId, alicePublic.MessageId));
            await delivery.CreateMessage(update, channel, null);
        }

        public Task<MessageSendStatus> Handle(P13QueueMailAddressChange packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendStatus> Handle(P15PasswordUpdate packet)
        {
            // TODO: Inject dependency from previous PasswordUpdate to latest LoopbackKeyNotify packet
            throw new NotImplementedException();
        }

        public async Task<MessageSendStatus> Handle(P18PublicKeys packet)
        {
            if (packet.Dependencies.Count != 1)
                throw new ProtocolException($"Packet {nameof(P18PublicKeys)} must reference the matching private keys.");

            Dependency dep = packet.Dependencies[0];
            if (dep.AccountId != Account.AccountId)
                throw new ProtocolException($"The dependency of {nameof(P18PublicKeys)} to private keys must be specific for the sending account.");

            using (DatabaseContext ctx = new DatabaseContext())
            {
                if (!await ctx.Messages.AnyAsync(m => m.ChannelId == dep.ChannelId && m.MessageId == dep.MessageId && m.PacketId == 0x17))
                    throw new ProtocolException($"Could not find the referenced private keys for {nameof(P18PublicKeys)}.");
            }
            return MessageSendStatus.Success;
        }

        public async Task PostHandling(P18PublicKeys packet, Message message) // Alice changes her keypair
        {
            using DatabaseContext ctx = new DatabaseContext();
            // Get all direct channels of Alice
            foreach (Channel channel in ctx.ChannelMembers
                .Where(m => m.AccountId == Account.AccountId)
                .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                .Where(c => c.ChannelType == ChannelType.Direct))
            {
                Account bob = await ctx.ChannelMembers
                    .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Account.AccountId)
                    .Select(m => m.Account).SingleAsync();

                // Get Bob's latest public key packet in this channel and take Alice's new public key

                Message bobPublic = await bob.GetLatestPublicKey(ctx);

                if (bobPublic == null) continue; // The server will create the DirectChannelUpdate when Bob sends his public key

                await CreateDirectChannelUpdate(ctx, channel, Account.AccountId, message, bob.AccountId, bobPublic);
            }
        }

        public Task<MessageSendStatus> Handle(P1EGroupChannelUpdate packet)
        {
            // TODO: Check for concurrency issues before insert
            throw new NotImplementedException();
        }

        public Task<MessageSendStatus> Handle(P28BlockList packet)
        {
            // TODO: What happens with existing channels?
            throw new NotImplementedException();
        }

        public Task Handle(P34SetClientState packet)
        {
            if (FocusedChannelId != packet.ChannelId || ChannelAction != packet.Action)
                delivery.OnChannelActionChanged(this, packet.ChannelId, packet.Action);

            if (Active != (packet.OnlineState == OnlineState.Active))
                delivery.OnActiveChanged(this, packet.OnlineState == OnlineState.Active);

            return Task.CompletedTask;
        }

        public Task Handle(P2DSearchAccount packet)
        {
            using DatabaseContext ctx = new DatabaseContext();
            var results = ctx.MailConfirmations
                .Where(c => c.AccountId != Account.AccountId
                    && c.MailAddress.Contains(packet.Query, StringComparison.Ordinal)
                    && c.ConfirmationTime != default) // Exclude unconfirmed accounts
                .Take(100); // Limit to 100 entries
            var response = Packet.New<P2ESearchAccountResponse>();
            foreach (var result in results)
                response.Results.Add(new SearchResult(result.AccountId, result.MailAddress));
            // Forward public packets to fully implement the Skynet protocol v5
            return SendPacket(response);
        }
    }
}
