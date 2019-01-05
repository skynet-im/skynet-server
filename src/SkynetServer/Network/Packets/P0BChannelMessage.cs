﻿using SkynetServer.Model;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0B, PacketPolicy.Duplex)]
    internal sealed class P0BChannelMessage : ChannelMessage
    {
        public byte PacketVersion { get; set; }
        public byte ContentPacketId { get; set; }
        public byte ContentPacketVersion { get; set; }
        public byte[] ContentPacket { get; set; }

        public override Packet Create() => new P0BChannelMessage().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            PacketVersion = buffer.ReadByte();
            ChannelId = buffer.ReadLong();
            MessageId = buffer.ReadLong();
            MessageFlags = (MessageFlags)buffer.ReadByte();
            if (MessageFlags.HasFlag(MessageFlags.FileAttached))
                FileId = buffer.ReadLong();
            ContentPacketId = buffer.ReadByte();
            ContentPacketVersion = buffer.ReadByte();
            ContentPacket = buffer.ReadByteArray();
            for (int i = 0; i < buffer.ReadUShort(); i++)
            {
                Dependencies.Add(new MessageDependency(buffer.ReadLong(), buffer.ReadLong(), buffer.ReadLong()));
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte(PacketVersion);
            buffer.WriteLong(ChannelId);
            buffer.WriteLong(SenderId);
            buffer.WriteLong(MessageId);
            buffer.WriteLong(SkipCount);
            buffer.WriteDate(DispatchTime);
            buffer.WriteByte((byte)MessageFlags);
            if (MessageFlags.HasFlag(MessageFlags.FileAttached))
                buffer.WriteLong(FileId);
            buffer.WriteByte(ContentPacketId);
            buffer.WriteByte(ContentPacketVersion);
            buffer.WriteByteArray(ContentPacket, true);
            buffer.WriteUShort((ushort)Dependencies.Count);
            foreach (MessageDependency dependency in Dependencies)
            {
                buffer.WriteLong(dependency.AccountId);
                buffer.WriteLong(dependency.ChannelId);
                buffer.WriteLong(dependency.MessageId);
            }
        }
    }
}
