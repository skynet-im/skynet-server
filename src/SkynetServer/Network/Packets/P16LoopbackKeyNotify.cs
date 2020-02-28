using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x16, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Loopback)]
    internal sealed class P16LoopbackKeyNotify : ChannelMessage
    {
        public override Packet Create() => new P16LoopbackKeyNotify().Init(this);
    }
}
