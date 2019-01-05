﻿using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0F, PacketPolicy.Send)]
    internal sealed class P0FSyncFinished : Packet
    {
        public override Packet Create() => new P0FSyncFinished().Init(this);

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