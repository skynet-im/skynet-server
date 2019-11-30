using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x27, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.Unencrypted | MessageFlags.MediaMessage | MessageFlags.ExternalFile)]
    internal sealed class P27ProfileImage : ChannelMessage
    {
        public string Caption { get; set; }

        public override Packet Create() => new P27ProfileImage().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            Caption = buffer.ReadString();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(Caption);
        }
    }
}
