using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x08, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P08RestoreSession : Packet
    {
        public long SessionId { get; set; }
        public byte[] SessionToken { get; set; }
        public long LastMessageId { get; set; }
        public List<long> Channels { get; set; } = new List<long>();

        public override Packet Create() => new P08RestoreSession().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            SessionId = buffer.ReadInt64();
            SessionToken = buffer.ReadRawByteArray(32).ToArray();
            ushort length = buffer.ReadUInt16();
            for (int i = 0; i < length; i++)
            {
                Channels.Add(buffer.ReadInt64());
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{{{nameof(P08RestoreSession)}: SessionId={SessionId:x8} LastMessageId={LastMessageId.ToString("x8")}}}";
        }
    }
}
