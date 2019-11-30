using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1B, PacketPolicies.Send)]
    internal sealed class P1BDirectChannelUpdate : ChannelMessage
    {
        public override Packet Create() => new P1BDirectChannelUpdate().Init(this);
    }
}
