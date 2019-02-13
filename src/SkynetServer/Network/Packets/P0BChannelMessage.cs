using SkynetServer.Model;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0B, PacketPolicy.Duplex)]
    internal class P0BChannelMessage : Packet
    {
        public long ChannelId { get; set; }
        public long SenderId { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public long FileId { get; set; }
        public byte[] FileKey { get; set; }
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        public byte PacketVersion { get; set; }
        public PacketPolicy ContentPacketPolicy { get; set; }
        public byte ContentPacketId { get; set; }
        public byte ContentPacketVersion { get; set; }
        public byte[] ContentPacket { get; set; }

        public override Packet Create() => new P0BChannelMessage().Init(this);

        public sealed override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public sealed override void ReadPacket(PacketBuffer buffer)
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
            if (MessageFlags.HasFlag(MessageFlags.Unencrypted | MessageFlags.FileAttached))
                FileKey = buffer.ReadByteArray();
            ushort length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
            {
                Dependencies.Add(new Dependency(buffer.ReadLong(), buffer.ReadLong(), buffer.ReadLong()));
            }
        }

        public sealed override void WritePacket(PacketBuffer buffer)
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
            if (MessageFlags.HasFlag(MessageFlags.Unencrypted | MessageFlags.FileAttached))
                buffer.WriteByteArray(FileKey, true);
            buffer.WriteUShort((ushort)Dependencies.Count);
            foreach (Dependency dependency in Dependencies)
            {
                buffer.WriteLong(dependency.AccountId);
                buffer.WriteLong(dependency.ChannelId);
                buffer.WriteLong(dependency.MessageId);
            }
        }

        protected P0BChannelMessage Init(P0BChannelMessage source)
        {
            Id = source.Id;
            Policy = source.Policy;
            ContentPacketId = source.ContentPacketId;
            ContentPacketPolicy = source.ContentPacketPolicy;
            return this;
        }

        public P0BChannelMessage Create(P0BChannelMessage source)
        {
            P0BChannelMessage target = (P0BChannelMessage)Create();

            target.ChannelId = source.ChannelId;
            target.SenderId = source.SenderId;
            target.MessageId = source.MessageId;
            target.SkipCount = source.SkipCount;
            target.DispatchTime = source.DispatchTime;
            target.MessageFlags = source.MessageFlags;
            target.FileId = source.FileId;
            target.Dependencies = source.Dependencies;

            target.PacketVersion = source.PacketVersion;
            target.ContentPacketPolicy = source.ContentPacketPolicy;
            target.ContentPacketId = source.ContentPacketId;
            target.ContentPacketVersion = source.ContentPacketVersion;
            target.ContentPacket = source.ContentPacket;

            return target;
        }

        public virtual Task<MessageSendError> HandleMessage(IPacketHandler handler)
        {
            return Task.FromResult(MessageSendError.Success);
        }

        public virtual Task PostHandling(IPacketHandler handler, Database.Entities.Message message)
        {
            return Task.CompletedTask;
        }

        public virtual void ReadMessage(PacketBuffer buffer)
        {

        }

        public virtual void WriteMessage(PacketBuffer buffer)
        {

        }

        public override string ToString()
        {
            return $"{{{GetType().Name}: ContentId=0x{ContentPacketId:x2} ChannelId=0x{ChannelId:x8} MessageFlags={MessageFlags}}}";
        }
    }
}
