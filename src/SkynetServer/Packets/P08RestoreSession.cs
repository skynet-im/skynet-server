using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x08, PacketPolicy.Receive)]
    internal sealed class P08RestoreSession : Packet
    {
        public long AccountId { get; set; }
        public byte[] KeyHash { get; set; }
        public long SessionId { get; set; }
        public List<(long ChannelId, long LastMessageId)> Channels { get; set; } = new List<(long ChannelId, long LastMessageId)>();

        public override Packet Create() => new P08RestoreSession().Init(this);

        public override void Handle() => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountId = buffer.ReadLong();
            KeyHash = buffer.ReadByteArray(32);
            SessionId = buffer.ReadLong();
            for (int i = 0; i < buffer.ReadUShort(); i++)
            {
                Channels.Add((buffer.ReadLong(), buffer.ReadLong()));
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
