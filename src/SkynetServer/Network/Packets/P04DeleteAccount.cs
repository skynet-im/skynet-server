using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x04, PacketPolicies.Receive)]
    internal sealed class P04DeleteAccount : Packet
    {
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P04DeleteAccount().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            KeyHash = buffer.ReadRawByteArray(32).ToArray();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
