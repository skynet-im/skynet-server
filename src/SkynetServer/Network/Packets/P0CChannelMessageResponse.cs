using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0C, PacketPolicy.Send)]
    internal sealed class P0CChannelMessageResponse : Packet
    {
        public long ChannelId { get; set; }
        public long TempMessageId { get; set; }
        public MessageSendError ErrorCode { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }

        public override Packet Create() => new P0CChannelMessageResponse().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(ChannelId);
            buffer.WriteLong(TempMessageId);
            buffer.WriteByte((byte)ErrorCode);
            buffer.WriteLong(MessageId);
            buffer.WriteLong(SkipCount);
            buffer.WriteDate(DispatchTime);
        }
    }
}
