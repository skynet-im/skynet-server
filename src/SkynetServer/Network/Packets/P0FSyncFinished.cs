using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0F, PacketPolicies.Send)]
    internal sealed class P0FSyncFinished : Packet
    {
        public override Packet Create() => new P0FSyncFinished().Init(this);
    }
}
