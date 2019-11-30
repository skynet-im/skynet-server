using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    internal class ChannelMessage : Packet
    {
        public byte PacketVersion { get; set; }
        public long ChannelId { get; set; }
        public long SenderId { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public long FileId { get; set; }
        public ChannelMessageFile File { get; set; }
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        public ReadOnlyMemory<byte> PacketContent { get; set; }

        public MessageFlags RequiredFlags { get; set; } = MessageFlags.None;
        public MessageFlags AllowedFlags { get; set; } = MessageFlags.All;

        public override Packet Create() => new ChannelMessage().Init(this);

        public Task Handle(IPacketHandler handler) => handler.HandleMessage(this);

        public sealed override void ReadPacket(PacketBuffer buffer)
        {
            PacketVersion = buffer.ReadByte();
            ChannelId = buffer.ReadInt64();
            MessageId = buffer.ReadInt64();
            MessageFlags = (MessageFlags)buffer.ReadByte();
            if (MessageFlags.HasFlag(MessageFlags.ExternalFile))
                FileId = buffer.ReadInt64();
            PacketContent = buffer.ReadByteArray();
            if (MessageFlags.HasFlag(MessageFlags.Unencrypted))
            {
                var contentBuffer = new PacketBuffer(PacketContent);
                ReadMessage(contentBuffer);
                if (MessageFlags.HasFlag(MessageFlags.MediaMessage))
                    File = new ChannelMessageFile(contentBuffer, MessageFlags.HasFlag(MessageFlags.ExternalFile));
            }
            ushort length = buffer.ReadUInt16();
            for (int i = 0; i < length; i++)
            {
                Dependencies.Add(new Dependency(buffer.ReadInt64(), buffer.ReadInt64(), buffer.ReadInt64()));
            }
        }

        public sealed override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte(PacketVersion);
            buffer.WriteInt64(ChannelId);
            buffer.WriteInt64(SenderId);
            buffer.WriteInt64(MessageId);
            buffer.WriteInt64(SkipCount);
            buffer.WriteDateTime(DispatchTime);
            buffer.WriteByte((byte)MessageFlags);
            if (MessageFlags.HasFlag(MessageFlags.ExternalFile))
                buffer.WriteInt64(FileId);

            if (GetType() == typeof(ChannelMessage))
            {
                PacketBuffer contentBuffer = new PacketBuffer();
                WriteMessage(contentBuffer);
                if (MessageFlags.HasFlag(MessageFlags.MediaMessage))
                    File.Write(contentBuffer, MessageFlags.HasFlag(MessageFlags.ExternalFile));
                PacketContent = contentBuffer.GetBuffer();
            }

            buffer.WriteByteArray(PacketContent.Span);
            buffer.WriteUInt16((ushort)Dependencies.Count);
            foreach (Dependency dependency in Dependencies)
            {
                buffer.WriteInt64(dependency.AccountId);
                buffer.WriteInt64(dependency.ChannelId);
                buffer.WriteInt64(dependency.MessageId);
            }
        }

        protected ChannelMessage Init(ChannelMessage source)
        {
            Id = source.Id;
            Policy = source.Policy;
            RequiredFlags = source.RequiredFlags;
            AllowedFlags = source.AllowedFlags;
            return this;
        }

        public virtual Task<MessageSendStatus> HandleMessage(IPacketHandler handler)
        {
            return Task.FromResult(MessageSendStatus.Success);
        }

        public virtual Task PostHandling(IPacketHandler handler, Database.Entities.Message message)
        {
            return Task.CompletedTask;
        }

        protected virtual void ReadMessage(PacketBuffer buffer)
        {

        }

        protected virtual void WriteMessage(PacketBuffer buffer)
        {

        }

        public override string ToString()
        {
            return $"{{{GetType().Name}: ChannelId={ChannelId:x8} MessageId={MessageId:x8} Flags={MessageFlags}}}";
        }
    }
}
