﻿using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x1E, PacketPolicy.Duplex)]
    [MsgFlags(MessageFlags.Unencrypted)]
    internal sealed class P1EGroupChannelUpdate : P0BChannelMessage
    {
        public long GroupRevision { get; set; }
        public List<(long AccountId, GroupMemberFlags Flags)> Members { get; set; } = new List<(long AccountId, GroupMemberFlags Flags)>();
        public byte[] KeyHistory { get; set; }

        public override Packet Create() => new P1EGroupChannelUpdate().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            GroupRevision = buffer.ReadLong();
            ushort length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
            {
                Members.Add((buffer.ReadLong(), (GroupMemberFlags)buffer.ReadByte()));
            }
            KeyHistory = buffer.ReadByteArray();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteLong(GroupRevision);
            buffer.WriteUShort((ushort)Members.Count);
            foreach ((long accountId, GroupMemberFlags flags) in Members)
            {
                buffer.WriteLong(accountId);
                buffer.WriteByte((byte)flags);
            }
            buffer.WriteByteArray(KeyHistory, true);
        }
    }
}
