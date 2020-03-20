using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x02, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P02CreateAccount : Packet
    {
        public string AccountName { get; set; }
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P02CreateAccount().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountName = buffer.ReadShortString();
            KeyHash = buffer.ReadRawByteArray(32).ToArray();
        }

        public override string ToString()
        {
            return $"{{{nameof(P02CreateAccount)}: AccountName={AccountName}}}";
        }
    }
}
