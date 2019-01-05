﻿using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x06, PacketPolicy.Receive)]
    internal sealed class P06CreateSession : Packet
    {
        public string AccountName { get; set; }
        public byte[] KeyHash { get; set; }
        public string FcmRegistrationToken { get; set; }

        public override Packet Create() => new P06CreateSession().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            AccountName = buffer.ReadString();
            KeyHash = buffer.ReadByteArray(32);
            FcmRegistrationToken = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}