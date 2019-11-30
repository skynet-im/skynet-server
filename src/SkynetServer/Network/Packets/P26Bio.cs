using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x26, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.Unencrypted)]
    internal sealed class P26Bio : ChannelMessage
    {
        public string PersonalMessage { get; set; }

        public override Packet Create() => new P26Bio().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            PersonalMessage = buffer.ReadString();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(PersonalMessage);
        }
    }
}
