using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x0B, PacketPolicy.Duplex)]
    internal sealed class P0BChannelMessage : Packet
    {
        public byte PacketVersion { get; set; }
        public long ChannelId { get; set; }
        public long SenderId { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public long FileId { get; set; }
        public byte ContentPacketId { get; set; }
        public byte ContentPacketVersion { get; set; }
        public byte[] ContentPacket { get; set; }
        public List<MessageDependency> Dependencies { get; set; } = new List<MessageDependency>();

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
