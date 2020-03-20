using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2B, PacketPolicies.Send)]
    internal class P2BOnlineState : ChannelMessage
    {
        public OnlineState OnlineState { get; set; }
        public DateTime LastActive { get; set; }

        public override Packet Create() => new P2BOnlineState().Init(this);

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)OnlineState);
            if (OnlineState == OnlineState.Inactive)
                buffer.WriteDateTime(LastActive);
        }
    }
}
