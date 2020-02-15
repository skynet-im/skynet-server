using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P34SetClientStateHandler : PacketHandler<P34SetClientState>
    {
        public override async ValueTask Handle(P34SetClientState packet)
        {
            if (Client.FocusedChannelId != packet.ChannelId || Client.ChannelAction != packet.Action)
                _ = await Delivery.ChannelActionChanged(Client, packet.ChannelId, packet.Action).ConfigureAwait(false);

            if (Client.Active != (packet.OnlineState == OnlineState.Active))
                await Delivery.ActiveChanged(Client, packet.OnlineState == OnlineState.Active).ConfigureAwait(false);
        }
    }
}
