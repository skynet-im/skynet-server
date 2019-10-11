using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x02, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P02CreateAccount : Packet
    {
        public string AccountName { get; set; }
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P02CreateAccount().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountName = buffer.ReadString();
            KeyHash = buffer.ReadByteArray(32);
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
