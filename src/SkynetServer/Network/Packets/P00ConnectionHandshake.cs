using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x00, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P00ConnectionHandshake : Packet
    {
        public int ProtocolVersion { get; set; }
        public string ApplicationIdentifier { get; set; }
        public int VersionCode { get; set; }

        public override Packet Create() => new P00ConnectionHandshake().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt32();
            ApplicationIdentifier = buffer.ReadShortString();
            VersionCode = buffer.ReadInt32();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
