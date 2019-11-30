using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x09, PacketPolicies.Send)]
    internal sealed class P09RestoreSessionResponse : Packet
    {
        public RestoreSessionStatus StatusCode { get; set; }

        public override Packet Create() => new P09RestoreSessionResponse().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)StatusCode);
        }

        public override string ToString()
        {
            return $"{{{nameof(P09RestoreSessionResponse)}: ErrorCode={StatusCode}}}";
        }
    }
}
