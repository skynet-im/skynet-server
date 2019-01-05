﻿using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x1B, PacketPolicy.Send)]
    internal sealed class P1BDirectChannelUpdate : ChannelMessage
    {
        public override Packet Create() => new P1BDirectChannelUpdate().Init(this);

        public override void Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
        }
    }
}