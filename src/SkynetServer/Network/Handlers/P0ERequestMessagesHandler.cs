using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal sealed class P0ERequestMessagesHandler : PacketHandler<P0ERequestMessages>
    {
        public override ValueTask Handle(P0ERequestMessages packet)
        {
            _ = Delivery.SyncMessages(Client, packet.ChannelId, packet.After, packet.Before, packet.MaxCount);
            _ = Client.Enqueue(Packets.New<P0FSyncFinished>());
            return default;
        }
    }
}
