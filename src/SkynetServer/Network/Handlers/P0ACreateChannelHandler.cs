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
    internal class P0ACreateChannelHandler : PacketHandler<P0ACreateChannel>
    {
        private readonly MessageInjectionService injector;

        public P0ACreateChannelHandler(MessageInjectionService injector)
        {
            this.injector = injector;
        }

        public override async ValueTask Handle(P0ACreateChannel packet)
        {
            Channel channel = null;
            var response = Packets.New<P2FCreateChannelResponse>();
            response.TempChannelId = packet.ChannelId;

            switch (packet.ChannelType)
            {
                case ChannelType.Loopback:
                    throw new ProtocolException("Loopback channels cannot be created manually");
                case ChannelType.AccountData:
                    throw new ProtocolException("Account data channels cannot be created manually");
                case ChannelType.Direct:
                    var counterpart = await Database.Accounts.AsQueryable()
                        .SingleOrDefaultAsync(acc => acc.AccountId == packet.CounterpartId)
                        .ConfigureAwait(false);
                    if (counterpart == null)
                    {
                        response.StatusCode = CreateChannelStatus.InvalidCounterpart;
                        await Client.Send(response).ConfigureAwait(false);
                    }
                    else if (await Database.BlockedAccounts.AsQueryable()
                        .AnyAsync(b => b.OwnerId == packet.CounterpartId && b.AccountId == Client.AccountId 
                            || b.OwnerId == Client.AccountId && b.AccountId == packet.CounterpartId)
                        .ConfigureAwait(false))
                    {
                        response.StatusCode = CreateChannelStatus.Blocked;
                        await Client.Send(response).ConfigureAwait(false);
                    }
                    else if (await Database.ChannelMembers.AsQueryable()
                        .Where(m => m.AccountId == packet.CounterpartId)
                        .Join(Database.ChannelMembers.AsQueryable()
                            .Where(m => m.AccountId == Client.AccountId)
                            .Join(Database.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                            .Where(c => c.ChannelType == ChannelType.Direct),
                            m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                        .AnyAsync().ConfigureAwait(false))
                    {
                        response.StatusCode = CreateChannelStatus.AlreadyExists;
                        await Client.Send(response).ConfigureAwait(false);
                    }
                    else
                    {
                        // Create a new direct channel
                        channel = await Database.AddChannel(
                            new Channel
                            {
                                OwnerId = Client.AccountId,
                                ChannelType = ChannelType.Direct
                            },
                            new ChannelMember { AccountId = Client.AccountId },
                            new ChannelMember { AccountId = packet.CounterpartId })
                            .ConfigureAwait(false);

                        // TODO: Check for existing direct channels and delete if another channel was created in the meantime

                        var createAlice = Packets.New<P0ACreateChannel>();
                        createAlice.ChannelId = channel.ChannelId;
                        createAlice.ChannelType = ChannelType.Direct;
                        createAlice.OwnerId = Client.AccountId;
                        createAlice.CounterpartId = packet.CounterpartId;
                        await Delivery.SendPacket(createAlice, Client.AccountId, Client).ConfigureAwait(false);

                        var createBob = Packets.New<P0ACreateChannel>();
                        createBob.ChannelId = channel.ChannelId;
                        createBob.ChannelType = ChannelType.Direct;
                        createBob.OwnerId = Client.AccountId;
                        createBob.CounterpartId = Client.AccountId;
                        await Delivery.SendPacket(createBob, packet.CounterpartId, null).ConfigureAwait(false);

                        response.StatusCode = CreateChannelStatus.Success;
                        response.ChannelId = channel.ChannelId;
                        await Client.Send(response).ConfigureAwait(false);

                        await Client.ForwardAccountChannels(Database, Client.Account, counterpart);

                        Message alicePublic = await Database.GetLatestPublicKey(Client.AccountId).ConfigureAwait(false);
                        Message bobPublic = await Database.GetLatestPublicKey(counterpart.AccountId).ConfigureAwait(false);

                        if (alicePublic != null && bobPublic != null)
                        {
                            var message = await injector
                                .CreateDirectChannelUpdate(channel, Client.AccountId, alicePublic, counterpart.AccountId, bobPublic).ConfigureAwait(false);
                            _ = Delivery.SendMessage(message, null);
                        }
                    }
                    break;
                case ChannelType.Group:
                case ChannelType.ProfileData:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(packet)}.{nameof(P0ACreateChannel.ChannelType)}");
            }
        }
    }
}
