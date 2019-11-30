using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x09, PacketPolicies.Send)]
    internal sealed class P09RestoreSessionResponse : Packet
    {
        public RestoreSessionStatus StatusCode { get; set; }

        public override Packet Create() => new P09RestoreSessionResponse().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

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
