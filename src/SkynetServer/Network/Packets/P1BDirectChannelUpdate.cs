using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1B, PacketPolicies.Send)]
    internal sealed class P1BDirectChannelUpdate : ChannelMessage
    {
        public override Packet Create() => new P1BDirectChannelUpdate().Init(this);

        public override Task<MessageSendStatus> HandleMessage(IPacketHandler handler) => throw new NotImplementedException();

        protected override void ReadMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
        }
    }
}
