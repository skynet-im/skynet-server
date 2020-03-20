using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x20, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.MediaMessage | MessageFlags.ExternalFile)]

    internal sealed class P20ChatMessage : ChannelMessage
    {
        public override Packet Create() => new P20ChatMessage().Init(this);
    }
}
