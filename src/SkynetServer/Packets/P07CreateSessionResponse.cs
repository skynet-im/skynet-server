using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x07, PacketPolicy.Send)]
    internal sealed class P07CreateSessionResponse : Packet
    {
        public long AccountId { get; set; }
        public long SessionId { get; set; }
        public CreateSessionError ErrorCode { get; set; }

        public override Packet Create() => new P07CreateSessionResponse().Init(this);

        public override void Handle() => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(AccountId);
            buffer.WriteLong(SessionId);
            buffer.WriteByte((byte)ErrorCode);
        }
    }
}
