using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2E, PacketPolicies.Send)]
    internal sealed class P2ESearchAccountResponse : Packet
    {
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();

        public override Packet Create() => new P2ESearchAccountResponse().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteUInt16((ushort)Results.Count);
            foreach (SearchResult result in Results)
            {
                buffer.WriteInt64(result.AccountId);
                buffer.WriteShortString(result.AccountName);
                buffer.WriteUInt16((ushort)result.ForwardedPackets.Count);
                foreach ((byte packetId, byte[] packetContent) in result.ForwardedPackets)
                {
                    buffer.WriteByte(packetId);
                    buffer.WriteByteArray(packetContent);
                }
            }
        }
    }
}
