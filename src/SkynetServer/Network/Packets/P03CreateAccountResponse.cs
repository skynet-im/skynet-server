using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x03, PacketPolicies.Send)]
    internal sealed class P03CreateAccountResponse : Packet
    {
        public CreateAccountError ErrorCode { get; set; }

        public override Packet Create() => new P03CreateAccountResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)ErrorCode);
        }

        public override string ToString()
        {
            return $"{{{nameof(P03CreateAccountResponse)}: ErrorCode={ErrorCode}}}";
        }
    }
}
