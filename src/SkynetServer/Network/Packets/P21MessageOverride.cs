using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x21, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.None)]

    internal sealed class P21MessageOverride : ChannelMessage
    {
        public override Packet Create() => new P21MessageOverride().Init(this);
    }
}
