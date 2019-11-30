using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x19, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Unencrypted)]
    internal sealed class P19ArchiveChannel : ChannelMessage
    {
        public ArchiveMode ArchiveMode { get; set; }

        public override Packet Create() => new P19ArchiveChannel().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            ArchiveMode = (ArchiveMode)buffer.ReadByte();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)ArchiveMode);
        }
    }
}
