using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1E, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Unencrypted)]
    internal sealed class P1EGroupChannelUpdate : ChannelMessage
    {
        public long GroupRevision { get; set; }
        public List<(long AccountId, GroupMemberFlags Flags)> Members { get; set; } = new List<(long AccountId, GroupMemberFlags Flags)>();
        public byte[] KeyHistory { get; set; }

        public override Packet Create() => new P1EGroupChannelUpdate().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            GroupRevision = buffer.ReadInt64();
            ushort length = buffer.ReadUInt16();
            for (int i = 0; i < length; i++)
            {
                Members.Add((buffer.ReadInt64(), (GroupMemberFlags)buffer.ReadByte()));
            }
            KeyHistory = buffer.ReadByteArray().ToArray();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteInt64(GroupRevision);
            buffer.WriteUInt16((ushort)Members.Count);
            foreach ((long accountId, GroupMemberFlags flags) in Members)
            {
                buffer.WriteInt64(accountId);
                buffer.WriteByte((byte)flags);
            }
            buffer.WriteByteArray(KeyHistory);
        }
    }
}
