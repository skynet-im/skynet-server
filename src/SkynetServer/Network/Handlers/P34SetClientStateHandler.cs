using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P34SetClientStateHandler : PacketHandler<P34SetClientState>
    {
        private readonly ClientStateService clientState;

        public P34SetClientStateHandler(ClientStateService clientState)
        {
            this.clientState = clientState;
        }

        public override async ValueTask Handle(P34SetClientState packet)
        {
            if (Client.FocusedChannelId != packet.ChannelId || Client.ChannelAction != packet.Action)
                _ = await clientState.ChannelActionChanged(Client, packet.ChannelId, packet.Action).ConfigureAwait(false);

            if (Client.Active != (packet.OnlineState == OnlineState.Active))
                _ = await clientState.ActiveChanged(Client, packet.OnlineState == OnlineState.Active).ConfigureAwait(false);
        }
    }
}
