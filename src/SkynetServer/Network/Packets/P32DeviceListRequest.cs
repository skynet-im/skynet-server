using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x32, PacketPolicies.Receive)]
    internal class P32DeviceListRequest : Packet
    {
        public override Packet Create() => new P32DeviceListRequest().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
