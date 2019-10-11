﻿using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0E, PacketPolicies.Receive)]
    internal sealed class P0ERequestMessages : Packet
    {
        public long ChannelId { get; set; }
        public long FirstKnownMessageId { get; set; }
        public long RequestCount { get; set; }

        public override Packet Create() => new P0ERequestMessages().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadLong();
            FirstKnownMessageId = buffer.ReadLong();
            RequestCount = buffer.ReadLong();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
