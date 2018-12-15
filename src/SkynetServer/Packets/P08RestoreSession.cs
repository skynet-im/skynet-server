using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x08, PacketPolicy.Receive)]

    internal sealed class P08RestoreSession : Packet
    {
        public Int64 AccountId { get; set; }
        public Byte[] KeyHash { get; set; }
        public Int64 SessionId { get; set; }
        public UInt16 Channels //Ich weiß nicht wie es geht!

        public override Packet Create() => new P08RestoreSession().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountId = buffer.ReadLong();
            KeyHash = buffer.ReadByteArray(32);
            SessionId = buffer.ReadLong();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
