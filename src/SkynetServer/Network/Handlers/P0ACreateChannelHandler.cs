using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P0ACreateChannelHandler : PacketHandler<P0ACreateChannel>
    {
        private readonly DeliveryService delivery;

        public P0ACreateChannelHandler(DeliveryService delivery)
        {
            this.delivery = delivery;
        }

        public override async ValueTask Handle(P0ACreateChannel packet)
        {
            Channel channel = null;
            var response = Packet.New<P2FCreateChannelResponse>();
            response.TempChannelId = packet.ChannelId;

            switch (packet.ChannelType)
            {
                case ChannelType.Loopback:
                    throw new ProtocolException("Loopback channels cannot be created manually");
                case ChannelType.AccountData:
                    throw new ProtocolException("Account data channels cannot be created manually");
                case ChannelType.Direct:
                    var counterpart = await Database.Accounts.SingleOrDefaultAsync(acc => acc.AccountId == packet.CounterpartId);
                    if (counterpart == null)
                    {
                        response.StatusCode = CreateChannelStatus.InvalidCounterpart;
                        await Client.SendPacket(response);
                    }
                    else if (await Database.BlockedAccounts.AnyAsync(b => b.OwnerId == packet.CounterpartId && b.AccountId == Client.AccountId)
                        || await Database.BlockedAccounts.AnyAsync(b => b.OwnerId == Client.AccountId && b.AccountId == packet.CounterpartId))
                    {
                        response.StatusCode = CreateChannelStatus.Blocked;
                        await Client.SendPacket(response);
                    }
                    else if (await Database.ChannelMembers.Where(m => m.AccountId == packet.CounterpartId)
                        .Join(Database.ChannelMembers.Where(m => m.AccountId == Client.AccountId)
                            .Join(Database.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                            .Where(c => c.ChannelType == ChannelType.Direct),
                            m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                        .AnyAsync())
                    {
                        response.StatusCode = CreateChannelStatus.AlreadyExists;
                        await Client.SendPacket(response);
                    }
                    else
                    {
                        // Create a new direct channel
                        channel = await DatabaseHelper.AddChannel(new Channel
                        {
                            OwnerId = Client.AccountId,
                            ChannelType = ChannelType.Direct
                        });

                        Database.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = Client.AccountId });
                        Database.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = packet.CounterpartId });
                        await Database.SaveChangesAsync();

                        // TODO: Check for existing direct channels and delete if another channel was created in the meantime

                        var createAlice = Packet.New<P0ACreateChannel>();
                        createAlice.ChannelId = channel.ChannelId;
                        createAlice.ChannelType = ChannelType.Direct;
                        createAlice.OwnerId = Client.AccountId;
                        createAlice.CounterpartId = packet.CounterpartId;
                        await delivery.SendPacket(createAlice, Client.AccountId, Client);

                        var createBob = Packet.New<P0ACreateChannel>();
                        createBob.ChannelId = channel.ChannelId;
                        createBob.ChannelType = ChannelType.Direct;
                        createBob.OwnerId = Client.AccountId;
                        createBob.CounterpartId = Client.AccountId;
                        await delivery.SendPacket(createBob, packet.CounterpartId, null);

                        response.StatusCode = CreateChannelStatus.Success;
                        response.ChannelId = channel.ChannelId;
                        await Client.SendPacket(response);

                        await ForwardAccountChannels(Database, Client.Account, counterpart);

                        Message alicePublic = await Client.Account.GetLatestPublicKey(Database);
                        Message bobPublic = await counterpart.GetLatestPublicKey(Database);

                        if (alicePublic != null && bobPublic != null)
                            await CreateDirectChannelUpdate(Database, channel, Client.AccountId, alicePublic, counterpart.AccountId, bobPublic);
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
