using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x23, PacketPolicy.Duplex)]
    internal sealed class P23MessageRead : ChannelMessage
    {
        public override Packet Create() => new P23MessageRead().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
        }

        public override void WritePacket(PacketBuffer buffer)
        {
        }
    }
}
