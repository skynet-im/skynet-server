using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x08, PacketPolicies.Receive | PacketPolicies.Unauthenticated)]
    internal sealed class P08RestoreSession : Packet
    {
        public long AccountId { get; set; }
        public byte[] KeyHash { get; set; }
        public long SessionId { get; set; }
        public List<(long ChannelId, long LastMessageId)> Channels { get; set; } = new List<(long ChannelId, long LastMessageId)>();

        public override Packet Create() => new P08RestoreSession().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountId = buffer.ReadLong();
            KeyHash = buffer.ReadByteArray(32);
            SessionId = buffer.ReadLong();
            ushort length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
            {
                Channels.Add((buffer.ReadLong(), buffer.ReadLong()));
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{{{nameof(P08RestoreSession)}: AccountId={AccountId:x8} SessionId={SessionId.ToString("x8")}}}";
        }
    }
}
