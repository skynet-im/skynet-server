using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0D, PacketPolicies.Send)]
    internal sealed class P0DDeleteChannel : Packet
    {
        public long ChannelId { get; set; }

        public override Packet Create() => new P0DDeleteChannel().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteInt64(ChannelId);
        }
    }
}
