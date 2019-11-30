using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x28, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Loopback | MessageFlags.Unencrypted)]
    internal sealed class P28BlockList : ChannelMessage
    {
        List<long> BlockedAccounts { get; set; } = new List<long>();
        List<long> BlockedConversations { get; set; } = new List<long>();

        public override Packet Create() => new P28BlockList().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            ushort length = buffer.ReadUInt16();
            for (int i = 0; i < length; i++)
            {
                BlockedAccounts.Add(buffer.ReadInt64());
            }

            length = buffer.ReadUInt16();
            for (int i = 0; i < length; i++)
            {
                BlockedConversations.Add(buffer.ReadInt64());
            }
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteUInt16((ushort)BlockedAccounts.Count);
            foreach (long id in BlockedAccounts)
            {
                buffer.WriteInt64(id);
            }

            buffer.WriteUInt16((ushort)BlockedConversations.Count);
            foreach (long id in BlockedConversations)
            {
                buffer.WriteInt64(id);
            }
        }
    }
}
