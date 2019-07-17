using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2E, PacketPolicy.Send)]
    internal sealed class P2ESearchAccountResponse : Packet
    {
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();

        public override Packet Create() => new P2ESearchAccountResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteUShort((ushort)Results.Count);
            foreach (SearchResult result in Results)
            {
                buffer.WriteLong(result.AccountId);
                buffer.WriteString(result.AccountName);
                buffer.WriteUShort((ushort)result.ForwardedPackets.Count);
                foreach ((byte packetId, byte[] packetContent) in result.ForwardedPackets)
                {
                    buffer.WriteByte(packetId);
                    buffer.WriteByteArray(packetContent, true);
                }
            }
        }
    }
}
