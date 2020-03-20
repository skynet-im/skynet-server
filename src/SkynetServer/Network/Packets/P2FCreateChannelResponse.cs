using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2F, PacketPolicies.Send)]
    internal sealed class P2FCreateChannelResponse : Packet
    {
        public long TempChannelId { get; set; }
        public CreateChannelStatus StatusCode { get; set; }
        public long ChannelId { get; set; }
        public DateTime CreationTime { get; set; }

        public override Packet Create() => new P2FCreateChannelResponse().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteInt64(TempChannelId);
            buffer.WriteByte((byte)StatusCode);
            buffer.WriteInt64(ChannelId);
            buffer.WriteDateTime(CreationTime);
        }

        public override string ToString()
        {
            return $"{{{nameof(P2FCreateChannelResponse)}: TempId={TempChannelId:x8} ErrorCode={StatusCode} ChannelId={ChannelId.ToString("x8")}}}";
        }
    }
}
