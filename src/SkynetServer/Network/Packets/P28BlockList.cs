using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x28, PacketPolicy.Duplex)]
    internal sealed class P28BlockList : ChannelMessage
    {
        List<long> BlockedAccounts { get; set; } = new List<long>();
        List<long> BlockedConversations { get; set; } = new List<long>();

        public override Packet Create() => new P28BlockList().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ushort length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
            {
                BlockedAccounts.Add(buffer.ReadLong());
            }

            length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
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
