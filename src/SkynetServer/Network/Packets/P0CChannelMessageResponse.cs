using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0C, PacketPolicies.Send)]
    internal sealed class P0CChannelMessageResponse : Packet
    {
        public long ChannelId { get; set; }
        public long TempMessageId { get; set; }
        public MessageSendError ErrorCode { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }

        public override Packet Create() => new P0CChannelMessageResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

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

        public override string ToString()
        {
            return $"{{{nameof(P0CChannelMessageResponse)}: ErrorCode={ErrorCode}}}";
        }
    }
}
