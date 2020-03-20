using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x06, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P06CreateSession : Packet
    {
        public string AccountName { get; set; }
        public byte[] KeyHash { get; set; }
        public string FcmRegistrationToken { get; set; }

        public override Packet Create() => new P06CreateSession().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountName = buffer.ReadShortString();
            KeyHash = buffer.ReadRawByteArray(32).ToArray();
            FcmRegistrationToken = buffer.ReadString();
        }

        public override string ToString()
        {
            return $"{{{nameof(P06CreateSession)}: AccountName={AccountName}}}";
        }
    }
}
