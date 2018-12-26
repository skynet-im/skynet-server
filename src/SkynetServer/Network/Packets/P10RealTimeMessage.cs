using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x10, PacketPolicy.Duplex)]
    internal sealed class P10RealTimeMessage : Packet
    {
        public long ChannelId { get; set; }
        public long SenderId { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public byte ContentPacketId { get; set; }
        public byte[] ContentPacket { get; set; }

        public override Packet Create() => new P10RealTimeMessage().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadLong();
            MessageFlags = (MessageFlags)buffer.ReadByte();
            ContentPacketId = buffer.ReadByte();
            ContentPacket = buffer.ReadByteArray();
        }
        
        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(ChannelId);
            buffer.WriteLong(SenderId);
            buffer.WriteByte((byte)MessageFlags);
            buffer.WriteByte(ContentPacketId);
            buffer.WriteByteArray(ContentPacket, true);
        }
    }
}
