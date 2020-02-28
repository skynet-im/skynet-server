using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2A, PacketPolicies.Duplex)]
    [RequiredFlags(MessageFlags.Loopback)]
    [AllowedFlags(MessageFlags.Loopback | MessageFlags.MediaMessage | MessageFlags.ExternalFile)]
    internal sealed class P2ABackgroundImage : ChannelMessage
    {
        public override Packet Create() => new P2ABackgroundImage().Init(this);
    }
}
