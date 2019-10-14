using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0C, PacketPolicies.Send)]
    internal sealed class P0CChannelMessageResponse : Packet
    {
        public long ChannelId { get; set; }
        public long TempMessageId { get; set; }
        public MessageSendStatus StatusCode { get; set; }
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
            buffer.WriteInt64(ChannelId);
            buffer.WriteInt64(TempMessageId);
            buffer.WriteByte((byte)StatusCode);
            buffer.WriteInt64(MessageId);
            buffer.WriteInt64(SkipCount);
            buffer.WriteDateTime(DispatchTime);
        }

        public override string ToString()
        {
            return $"{{{nameof(P0CChannelMessageResponse)}: ErrorCode={StatusCode}}}";
        }
    }
}
