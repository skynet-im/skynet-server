using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{{{nameof(P02CreateAccount)}: AccountName={AccountName}}}";
        }
    }
}
