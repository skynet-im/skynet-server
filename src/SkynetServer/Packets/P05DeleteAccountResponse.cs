using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x05, PacketPolicy.Send)]
    internal sealed class P05DeleteAccountResponse : Packet
    {
        public DeleteAccountError ErrorCode { get; set; }
        public override Packet Create() => new P05DeleteAccountResponse().Init(this);


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
