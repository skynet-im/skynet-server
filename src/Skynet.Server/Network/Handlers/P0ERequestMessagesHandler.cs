using Skynet.Protocol.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal sealed class P0ERequestMessagesHandler : PacketHandler<P0ERequestMessages>
    {
        public override async ValueTask Handle(P0ERequestMessages packet)
        {
            await Delivery.StartSyncMessages(Client, packet.ChannelId, packet.After, packet.Before, packet.MaxCount).ConfigureAwait(false);
            _ = Client.Enqueue(Packets.New<P0FSyncFinished>());
        }
    }
}
