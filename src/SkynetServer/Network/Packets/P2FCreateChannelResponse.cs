using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SkynetServer.Network.Model;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2F, PacketPolicy.Send)]
    internal sealed class P2FCreateChannelResponse : Packet
    {
        public long TempChannelId { get; set; }
        public CreateChannelError ErrorCode { get; set; }
        public long ChannelId { get; set; }

        public override Packet Create() => new P2FCreateChannelResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer) => throw new NotImplementedException();

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(TempChannelId);
            buffer.WriteByte((byte)ErrorCode);
            buffer.WriteLong(ChannelId);
        }

        public override string ToString()
        {
            return $"{{{nameof(P2FCreateChannelResponse)}: TempId={TempChannelId:x8} ErrorCode={ErrorCode} ChannelId={ChannelId.ToString("x8")}}}";
        }
    }
}
