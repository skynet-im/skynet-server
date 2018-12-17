using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x0B, PacketPolicy.)]
    internal sealed class P0BChannelMessage : Packet
    {
        public Byte PacketVersion { get; set; }
        public long ChannelId { get; set; }
        //???
        public long MessageId { get; set; }
        //???
        public MessageFlags MessageFlags { get; set; }
        //???
        public Byte ContentPacketId { get; set; }
        public Byte ContentPacketVersion { get; set; }
        //???
        public List<(long AccountId, long ChannelId, long MessageId)> Dependencies { get; set; } = new List<(long AccountId, long ChannelId, long MessageId)>();


        public override Packet Create() => new P0BChannelMessage().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
