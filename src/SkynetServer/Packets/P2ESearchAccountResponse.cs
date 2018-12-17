using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x2E, PacketPolicy.Send)]
    internal sealed class P2ESearchAccountResponse : Packet
    {
        public List<(long AccountId, string AccountName, List<(byte PacketId, byte[] PacketContent)> ForwardedPackets)> Result { get; set; } = new List<(long AccountId, string AccountName, List<(byte PacketId, byte[] PacketContent)> ForwardedPackets)>();

        public override Packet Create() => new P2ESearchAccountResponse().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            // TODO: Need help
        }
    }
}
