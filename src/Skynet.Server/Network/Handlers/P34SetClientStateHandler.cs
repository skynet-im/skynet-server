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

            // TODO: Make sure that all clients have received their P0ACreateChannel before sending this packet
            _ = await clientState.SetChannelAction(Client, packet.ChannelId, packet.Action).ConfigureAwait(false);
            _ = await clientState.SetActive(Client, packet.OnlineState == OnlineState.Active).ConfigureAwait(false);
        }
    }
}