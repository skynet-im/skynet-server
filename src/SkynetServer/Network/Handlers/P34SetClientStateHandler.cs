using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P34SetClientStateHandler : PacketHandler<P34SetClientState>
    {
        private readonly DeliveryService delivery;

        public P34SetClientStateHandler(DeliveryService delivery)
        {
            this.delivery = delivery;
        }

        public override ValueTask Handle(P34SetClientState packet)
        {
            if (Client.FocusedChannelId != packet.ChannelId || Client.ChannelAction != packet.Action)
                delivery.OnChannelActionChanged(Client, packet.ChannelId, packet.Action);

            if (Client.Active != (packet.OnlineState == OnlineState.Active))
                delivery.OnActiveChanged(Client, packet.OnlineState == OnlineState.Active);

            return default;
        }
    }
}
