using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x24, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.MediaMessage | MessageFlags.ExternalFile)]

    internal sealed class P24DaystreamMessage : ChannelMessage
    {
        public override Packet Create() => new P24DaystreamMessage().Init(this);
    }
}
