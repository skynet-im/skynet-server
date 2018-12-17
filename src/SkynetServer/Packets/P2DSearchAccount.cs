using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x2D, PacketPolicy.Receive)]
    internal sealed class P2DSearchAccount : Packet
    {
        public string Query { get; set; }

        public override Packet Create() => new P2DSearchAccount().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            Query = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
