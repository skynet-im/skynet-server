﻿using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x0D, PacketPolicy.Receive)]
    internal sealed class P0DMessageBlock : Packet
    {
        public List<(Byte[] Message)> Messages { get; set; } = new List<(byte[] Message)>(); //???

        public override Packet Create() => new P0DMessageBlock().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            for(int i = 0; i< buffer.ReadByte(); i++)
            {
                Messages.Add(buffer.ReadByteArray()); //???
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
