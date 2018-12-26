using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x11, PacketPolicy.Receive)]
    internal sealed class P11SubscribeChannel : Packet
    {
        public long ChannelId { get; set; }
        public byte PacketId { get; set; }

        public override Packet Create() => new P11SubscribeChannel().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

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
