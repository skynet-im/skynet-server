﻿using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal sealed class P34SetClientStateHandler : PacketHandler<P34SetClientState>
    {
        private readonly ClientStateService clientState;

        public P34SetClientStateHandler(ClientStateService clientState)
        {
            this.clientState = clientState;
        }

        public override async ValueTask Handle(P34SetClientState packet)
        {
            if (packet.ChannelId == default && packet.Action != ChannelAction.None)
                throw new ProtocolException("A ChannelAction other than None requires a ChannelId.");

            await clientState.StartSetChannelAction(Client, packet.ChannelId, packet.Action).ConfigureAwait(false);

            bool active = packet.OnlineState == OnlineState.Active;
            if (Client.Active != active)
            {
                await clientState.StartSetActive(Client, active).ConfigureAwait(false);
            }
        }
    }
}
