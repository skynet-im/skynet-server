using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2C, PacketPolicy.Send)]
    internal class P2CChannelAction : Packet
    {
        long ChannelId { get; set; }
        long AccountId { get; set; }
        ChannelAction Action { get; set; }

        public override Packet Create() => new P2CChannelAction().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(ChannelId);
            buffer.WriteLong(AccountId);
            buffer.WriteByte((byte)Action);
        }
    }
}
