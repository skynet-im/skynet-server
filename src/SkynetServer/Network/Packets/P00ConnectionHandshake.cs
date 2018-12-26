using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x00, PacketPolicy.Receive)]
    internal sealed class P00ConnectionHandshake : Packet
    {
        public int ProtocolVersion { get; set; }
        public string ApplicationIdentifier { get; set; }
        public int VersionCode { get; set; }

        public override Packet Create() => new P00ConnectionHandshake().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt();
            ApplicationIdentifier = buffer.ReadString();
            VersionCode = buffer.ReadInt();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
