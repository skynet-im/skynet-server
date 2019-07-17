using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x04, PacketPolicy.Receive)]
    internal sealed class P04DeleteAccount : Packet
    {
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P04DeleteAccount().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            KeyHash = buffer.ReadByteArray(32);
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
