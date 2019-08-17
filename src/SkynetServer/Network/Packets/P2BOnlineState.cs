using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x2B, PacketPolicy.Send)]
    internal class P2BOnlineState : P0BChannelMessage
    {
        public OnlineState OnlineState { get; set; }
        public DateTime LastActive { get; set; }

        public override Packet Create() => new P2BOnlineState().Init(this);

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)OnlineState);
            if (OnlineState == OnlineState.Inactive)
                buffer.WriteDate(LastActive);
        }
    }
}
