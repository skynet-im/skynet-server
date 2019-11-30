using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x32, PacketPolicies.Receive)]
    internal class P32DeviceListRequest : Packet
    {
        public override Packet Create() => new P32DeviceListRequest().Init(this);
    }
}
