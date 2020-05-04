using Microsoft.EntityFrameworkCore;
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
    internal sealed class P0ACreateChannelHandler : PacketHandler<P0ACreateChannel>
    {
        private readonly MessageInjectionService injector;

        public P0ACreateChannelHandler(MessageInjectionService injector)
        {
            this.injector = injector;
        }

        public override ValueTask Handle(P0ACreateChannel packet)
        {
            switch (packet.ChannelType)
            {
                case ChannelType.Loopback:
                    throw new ProtocolException("Loopback channels cannot be created manually");
                case ChannelType.AccountData:
                    throw new ProtocolException("Account data channels cannot be created manually");
                case ChannelType.Direct:
                    return CreateDirectChannel(packet.ChannelId, packet.CounterpartId);
                case ChannelType.Group:
                case ChannelType.ProfileData:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(packet)}.{nameof(P0ACreateChannel.ChannelType)}");
            }
        }

        private async ValueTask CreateDirectChannel(long tempChannelId, long counterpartId)
        {
            var counterpart = await Database.Accounts.AsQueryable()
                .SingleOrDefaultAsync(acc => acc.AccountId == counterpartId).ConfigureAwait(false);
            if (counterpart == null)
            {
                await Fail(CreateChannelStatus.InvalidCounterpart, tempChannelId).ConfigureAwait(false);
                return;
            }

            if (await Database.BlockedAccounts.AsQueryable()
                .AnyAsync(b => b.OwnerId == counterpartId && b.AccountId == Client.AccountId
                    || b.OwnerId == Client.AccountId && b.AccountId == counterpartId)
                .ConfigureAwait(false))
            {
                await Fail(CreateChannelStatus.Blocked, tempChannelId).ConfigureAwait(false);
                return;
            }

            if (await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.Direct
                    && ((c.OwnerId == Client.AccountId && c.CounterpartId == counterpartId)
                    || (c.OwnerId == counterpartId && c.CounterpartId == Client.AccountId)))
                .AnyAsync().ConfigureAwait(false))
            {
                await Fail(CreateChannelStatus.AlreadyExists, tempChannelId).ConfigureAwait(false);
                return;
            }
            long aliceChannelId = await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == Client.AccountId)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);
            long bobChannelId = await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == counterpartId)
                .Select(c => c.ChannelId)
                .SingleAsync().ConfigureAwait(false);

            var tcs = new TaskCompletionSource<Channel>();

            // Register send operations before actually creating the channel to ensure
            // that all clients receive their P0ACreateChannel packet before any other messages
            Task responseTask = Client.Send(new CreateChannelEnumerable(Packets, tcs.Task, counterpartId, bobChannelId, tempChannelId));
            await Delivery.StartSendToAccount(new CreateChannelEnumerable(Packets, tcs.Task, counterpartId, bobChannelId), Client.AccountId, Client).ConfigureAwait(false);
            await Delivery.StartSendToAccount(new CreateChannelEnumerable(Packets, tcs.Task, Client.AccountId, aliceChannelId), counterpartId, null).ConfigureAwait(false);

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

            // Check for existing direct channels and delete if another channel was created in the meantime
            Channel first = await Database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.Direct
                    && ((c.OwnerId == Client.AccountId && c.CounterpartId == counterpartId)
                    || (c.OwnerId == counterpartId && c.CounterpartId == Client.AccountId)))
                .OrderBy(c => c.CreationTime)
                .FirstAsync().ConfigureAwait(false);

            if (first.ChannelId != channel.ChannelId)
            {
                Database.Channels.Remove(channel);
                await Database.SaveChangesAsync().ConfigureAwait(false);
                tcs.SetResult(null);
            }
            else // This channel won the race and we continue processing
            {
                // Add channel memberships while send the send queue is locked
                Database.ChannelMembers.Add(new ChannelMember { ChannelId = aliceChannelId, AccountId = counterpartId });
                Database.ChannelMembers.Add(new ChannelMember { ChannelId = bobChannelId, AccountId = Client.AccountId });
                await Database.SaveChangesAsync().ConfigureAwait(false);
                tcs.SetResult(channel);

                // Start messages forwarding before injecting the direct channel update
                // Otherwise clients could not resolve the dependencies to the keys
                await Delivery.StartSyncMessages(Client.AccountId, bobChannelId).ConfigureAwait(false);
                await Delivery.StartSyncMessages(counterpartId, aliceChannelId).ConfigureAwait(false);

                long alicePublicId = await Database.GetLatestPublicKey(Client.AccountId).ConfigureAwait(false);
                long bobPublicId = await Database.GetLatestPublicKey(counterpart.AccountId).ConfigureAwait(false);

                if (alicePublicId != default && bobPublicId != default)
                {
                    var message = await injector
                        .CreateDirectChannelUpdate(channel.ChannelId, Client.AccountId, alicePublicId, counterpart.AccountId, bobPublicId)
                        .ConfigureAwait(false);
                    await Delivery.StartSendMessage(message, null).ConfigureAwait(false);
                }
            }

            await responseTask.ConfigureAwait(false);
        }

        private Task Fail(CreateChannelStatus status, long tempChannelId)
        {
            var response = Packets.New<P2FCreateChannelResponse>();
            response.TempChannelId = tempChannelId;
            response.StatusCode = status;
            return Client.Send(response);
        }
    }
}
