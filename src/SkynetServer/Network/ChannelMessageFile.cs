using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    internal sealed class ChannelMessageFile
    {
        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string ThumbnailContentType { get; set; }
        public ReadOnlyMemory<byte> ThumbnailData { get; set; }
        public string ContentType { get; set; }
        public long Length { get; set; }
        public byte[] Key { get; set; }

        public ChannelMessageFile(PacketBuffer buffer, bool external)
        {
            Name = buffer.ReadShortString();
            CreationTime = buffer.ReadDateTime();
            LastWriteTime = buffer.ReadDateTime();
            ThumbnailContentType = buffer.ReadShortString();
            ThumbnailData = buffer.ReadByteArray();

            if (external)
            {
                ContentType = buffer.ReadShortString();
                Length = buffer.ReadInt64();
                Key = buffer.ReadRawByteArray(32).ToArray();
            }
        }

        public void Write(PacketBuffer buffer, bool external)
        {
            buffer.WriteShortString(Name);
            buffer.WriteDateTime(CreationTime);
            buffer.WriteDateTime(LastWriteTime);
            buffer.WriteShortString(ThumbnailContentType);
            buffer.WriteByteArray(ThumbnailData.Span);

            if (external)
            {
                buffer.WriteShortString(ContentType);
                buffer.WriteInt64(Length);
                buffer.WriteRawByteArray(Key);
            }
        }
    }
}
