using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x06, PacketPolicy.Send)]
    internal sealed class P07CreateSessionResponse : Packet
    {
        public Int64 AccountId { get; set; }
        public Int64 SessionId { get; set; }
        public CreateSessionError ErrorCode { get; set; }

        public override Packet Create() => new P07CreateSessionResponse().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong((Int64)AccountId);
            buffer.WriteLong((Int64)SessionId);
            buffer.WriteByte((byte)ErrorCode);
        }
    }
}
