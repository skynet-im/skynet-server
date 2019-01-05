﻿using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x09, PacketPolicy.Send)]
    internal sealed class P09RestoreSessionResponse : Packet
    {
        public RestoreSessionError ErrorCode { get; set; }

        public override Packet Create() => new P09RestoreSessionResponse().Init(this);

        public override void Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)ErrorCode);
        }
    }
}