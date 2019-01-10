using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x30, PacketPolicy.Receive)]
    internal sealed class P30FileUpload : Packet
    {
        public override Packet Create() => new P30FileUpload().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
        }

        public override void WritePacket(PacketBuffer buffer) => throw new NotImplementedException();
    }
}
