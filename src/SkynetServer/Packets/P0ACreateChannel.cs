using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x0A, PacketPolicy.Send)]
    internal sealed class P0ACreateChannel : Packet
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public //?????

        public override Packet Create() => new P0ACreateChannel().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(ChannelId);
            buffer.WriteByte((byte)ChannelType);
            //?????
        }
    }
}
