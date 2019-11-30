using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2D, PacketPolicies.Receive)]
    internal sealed class P2DSearchAccount : Packet
    {
        public string Query { get; set; }

        public override Packet Create() => new P2DSearchAccount().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            Query = buffer.ReadShortString();
        }
    }
}
