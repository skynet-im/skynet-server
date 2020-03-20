using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1C, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Loopback)]
    internal sealed class P1CDirectChannelCustomization : ChannelMessage
    {
        public override Packet Create() => new P1CDirectChannelCustomization().Init(this);
    }
}
