using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x14, PacketPolicies.Send)]
    internal sealed class P14MailAddress : ChannelMessage
    {
        public string MailAddress { get; set; }

        public override Packet Create() => new P14MailAddress().Init(this);

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteShortString(MailAddress);
        }
    }
}
