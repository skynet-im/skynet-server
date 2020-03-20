using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x13, PacketPolicies.Receive)]
    [MessageFlags(MessageFlags.Loopback | MessageFlags.Unencrypted)]
    internal sealed class P13QueueMailAddressChange : ChannelMessage
    {
        public string NewMailAddress { get; set; }

        public override Packet Create() => new P13QueueMailAddressChange().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            NewMailAddress = buffer.ReadShortString();
        }
    }
}
