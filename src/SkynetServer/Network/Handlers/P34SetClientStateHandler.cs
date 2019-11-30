using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P34SetClientStateHandler : PacketHandler<P34SetClientState>
    {
        public override ValueTask Handle(P34SetClientState packet)
        {
            if (Client.FocusedChannelId != packet.ChannelId || Client.ChannelAction != packet.Action)
                Delivery.OnChannelActionChanged(Client, packet.ChannelId, packet.Action);

            if (Client.Active != (packet.OnlineState == OnlineState.Active))
                Delivery.OnActiveChanged(Client, packet.OnlineState == OnlineState.Active);

            return default;
        }
    }
}
