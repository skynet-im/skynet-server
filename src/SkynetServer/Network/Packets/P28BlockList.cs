using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x28, PacketPolicy.Duplex)]
    internal sealed class P28BlockList : ChannelMessage
    {
        List<long> BlockedAccounts { get; set; } = new List<long>();
        List<long> BlockedConversations { get; set; } = new List<long>();

        public override Packet Create() => new P28BlockList().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            for (int i = 0; i < buffer.ReadUShort(); i++)
            {
                BlockedAccounts.Add(buffer.ReadLong());
            }

            for (int i = 0; i < buffer.ReadUShort(); i++)
            {
                BlockedConversations.Add(buffer.ReadLong());
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteUShort((ushort)BlockedAccounts.Count);
            foreach (long id in BlockedAccounts)
            {
                buffer.WriteLong(id);
            }

            buffer.WriteUShort((ushort)BlockedConversations.Count);
            foreach (long id in BlockedConversations)
            {
                buffer.WriteLong(id);
            }
        }
    }
}
