using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1D, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.NoSenderSync)]

    internal sealed class P1DGroupChannelKeyNotify : ChannelMessage
    {
        public override Packet Create() => new P1DGroupChannelKeyNotify().Init(this);
    }
}
