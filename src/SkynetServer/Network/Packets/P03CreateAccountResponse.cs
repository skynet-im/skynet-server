using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x03, PacketPolicies.Send)]
    internal sealed class P03CreateAccountResponse : Packet
    {
        public CreateAccountStatus StatusCode { get; set; }

        public override Packet Create() => new P03CreateAccountResponse().Init(this);

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
            return $"{{{nameof(P03CreateAccountResponse)}: ErrorCode={StatusCode}}}";
        }
    }
}
