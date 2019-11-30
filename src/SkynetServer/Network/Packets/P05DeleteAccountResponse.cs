using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x05, PacketPolicies.Send)]
    internal sealed class P05DeleteAccountResponse : Packet
    {
        public DeleteAccountStatus StatusCode { get; set; }

        public override Packet Create() => new P05DeleteAccountResponse().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)StatusCode);
        }
    }
}
