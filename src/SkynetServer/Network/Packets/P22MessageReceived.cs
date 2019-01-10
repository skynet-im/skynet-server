using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x22, PacketPolicy.Duplex)]
    internal sealed class P22MessageReceived : ChannelMessage
    {
        public override Packet Create() => new P22MessageReceived().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
        }

        public override void WritePacket(PacketBuffer buffer)
        {
        }
    }
}
