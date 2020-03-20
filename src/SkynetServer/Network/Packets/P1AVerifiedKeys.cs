using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1A, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Loopback)]
    internal sealed class P1AVerifiedKeys : ChannelMessage
    {
        public override Packet Create() => new P1AVerifiedKeys().Init(this);
    }
}
