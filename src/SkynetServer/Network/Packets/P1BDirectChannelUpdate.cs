using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1B, PacketPolicy.Send)]
    internal sealed class P1BDirectChannelUpdate : P0BChannelMessage
    {
        public override Packet Create() => new P1BDirectChannelUpdate().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
        }
    }
}
