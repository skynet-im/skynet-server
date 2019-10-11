using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x11, PacketPolicies.Receive)]
    internal sealed class P11SubscribeChannel : Packet
    {
        public long ChannelId { get; set; }
        public byte PacketId { get; set; }

        public override Packet Create() => new P11SubscribeChannel().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadLong();
            PacketId = buffer.ReadByte();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
