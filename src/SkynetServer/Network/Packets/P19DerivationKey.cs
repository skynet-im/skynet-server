using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x19, PacketPolicy.Send)]
    internal sealed class P19DerivationKey : ChannelMessage
    {
        public override Packet Create() => new P19DerivationKey().Init(this);

        public override void Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
        }
    }
}
