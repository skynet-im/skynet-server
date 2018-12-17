using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x03, PacketPolicy.Send)]
    internal sealed class P03CreateAccountResponse : Packet
    {
        public CreateAccountError ErrorCode { get; set; }

        public override Packet Create() => new P03CreateAccountResponse().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)ErrorCode);
        }
    }
}
