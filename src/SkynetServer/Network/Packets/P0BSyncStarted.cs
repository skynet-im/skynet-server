using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0B, PacketPolicies.Send)]
    internal class P0BSyncStarted : Packet
    {
        public int MinCount { get; set; }

        public override Packet Create() => new P0BSyncStarted().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteInt32(MinCount);   
        }
    }
}
