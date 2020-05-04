﻿using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal class P0ACreateChannelHandler : PacketHandler<P0ACreateChannel>
    {
        private readonly MessageInjectionService injector;

        public P0ACreateChannelHandler(MessageInjectionService injector)
        {
            this.injector = injector;
        }

        public override ValueTask Handle(P0ACreateChannel packet)
        {
            var response = Packets.New<P2FCreateChannelResponse>();
            response.TempChannelId = packet.ChannelId;

            switch (packet.ChannelType)
            {
                case ChannelType.Loopback:
                    throw new ProtocolException("Loopback channels cannot be created manually");
                case ChannelType.AccountData:
                    throw new ProtocolException("Account data channels cannot be created manually");
                case ChannelType.Direct:
                    return CreateDirectChannel(packet.CounterpartId, response);
                case ChannelType.Group:
                case ChannelType.ProfileData:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(packet)}.{nameof(P0ACreateChannel.ChannelType)}");
            }
        }

        private async ValueTask CreateDirectChannel(long counterpartId, P2FCreateChannelResponse response)
        {
            var counterpart = await Database.Accounts.AsQueryable()
                .SingleOrDefaultAsync(acc => acc.AccountId == counterpartId).ConfigureAwait(false);
            if (counterpart == null)
            {
                response.StatusCode = CreateChannelStatus.InvalidCounterpart;
                await Client.Send(response).ConfigureAwait(false);
            }
            else if (await Database.BlockedAccounts.AsQueryable()
                .AnyAsync(b => b.OwnerId == counterpartId && b.AccountId == Client.AccountId
                    || b.OwnerId == Client.AccountId && b.AccountId == counterpartId)
                .ConfigureAwait(false))
            {
                response.StatusCode = CreateChannelStatus.Blocked;
                await Client.Send(response).ConfigureAwait(false);
            }
            else if (await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.Direct
                    && ((c.OwnerId == Client.AccountId && c.CounterpartId == counterpartId) 
                    || (c.OwnerId == counterpartId && c.CounterpartId == Client.AccountId)))
                .AnyAsync().ConfigureAwait(false))
            {
                response.StatusCode = CreateChannelStatus.AlreadyExists;
                await Client.Send(response).ConfigureAwait(false);
            }
            else
            {
                // Create a new direct channel
                Channel channel = await Database.AddChannel(
                    new Channel
                    {
                        OwnerId = Client.AccountId,
                        ChannelType = ChannelType.Direct,
                        CounterpartId = counterpartId
                    },
                    new ChannelMember { AccountId = Client.AccountId },
                    new ChannelMember { AccountId = counterpartId })
                    .ConfigureAwait(false);

                // TODO: Check for existing direct channels and delete if another channel was created in the meantime

                (long aliceChannelId, long bobChannelId) = await AddToAccountChannels(Client.AccountId, counterpartId).ConfigureAwait(false);

                response.StatusCode = CreateChannelStatus.Success;
                response.ChannelId = channel.ChannelId;
                Task responseTask = Client.Send(response);

                // The following actions can be retried and are therefore executed asynchronously for both clients
                var createAlice = Packets.New<P0ACreateChannel>();
                createAlice.ChannelId = channel.ChannelId;
                createAlice.ChannelType = ChannelType.Direct;
                createAlice.OwnerId = Client.AccountId;
                createAlice.CounterpartId = counterpartId;
                await Delivery.StartSendToAccount(createAlice, Client.AccountId, Client).ConfigureAwait(false);

                var createBob = Packets.New<P0ACreateChannel>();
                createBob.ChannelId = channel.ChannelId;
                createBob.ChannelType = ChannelType.Direct;
                createBob.OwnerId = Client.AccountId;
                createBob.CounterpartId = Client.AccountId;
                await Delivery.StartSendToAccount(createBob, counterpartId, null).ConfigureAwait(false);

                // Start messages forwarding before injecting the direct channel update
                // Otherwise clients could not resolve the dependencies to the keys
                await StartForwardAccountChannel(bobChannelId, counterpartId, Client.AccountId).ConfigureAwait(false);
                await StartForwardAccountChannel(aliceChannelId, Client.AccountId, counterpartId).ConfigureAwait(false);

                long alicePublicId = await Database.GetLatestPublicKey(Client.AccountId).ConfigureAwait(false);
                long bobPublicId = await Database.GetLatestPublicKey(counterpart.AccountId).ConfigureAwait(false);

                if (alicePublicId != default && bobPublicId != default)
                {
                    var message = await injector
                        .CreateDirectChannelUpdate(channel.ChannelId, Client.AccountId, alicePublicId, counterpart.AccountId, bobPublicId)
                        .ConfigureAwait(false);
                    await Delivery.StartSendMessage(message, null).ConfigureAwait(false);
                }

                await responseTask.ConfigureAwait(false);
            }
        }

        private async Task<(long aliceChannelId, long bobChannelId)> AddToAccountChannels(long aliceId, long bobId)
        {
            long aliceChannelId = await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == aliceId)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);
            long bobChannelId = await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == bobId)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);

            Database.ChannelMembers.Add(new ChannelMember { ChannelId = aliceChannelId, AccountId = bobId });
            Database.ChannelMembers.Add(new ChannelMember { ChannelId = bobChannelId, AccountId = aliceId });
            await Database.SaveChangesAsync().ConfigureAwait(false);

            return (aliceChannelId, bobChannelId);
        }

        private async Task StartForwardAccountChannel(long channelId, long ownerId, long recipientId)
        {
            var createAlice = Packets.New<P0ACreateChannel>();
            createAlice.ChannelId = channelId;
            createAlice.ChannelType = ChannelType.AccountData;
            createAlice.OwnerId = ownerId;

            // Don't wait for the create channel packet to be sent to make sure that the following send operation is enqueued immediately
            await Delivery.StartSendToAccount(createAlice, recipientId, null).ConfigureAwait(false);
            await Delivery.StartSyncMessages(recipientId, channelId).ConfigureAwait(false);
        }
    }
}
