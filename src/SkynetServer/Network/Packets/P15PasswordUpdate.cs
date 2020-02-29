﻿using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x15, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Loopback | MessageFlags.Unencrypted)]
    internal sealed class P15PasswordUpdate : ChannelMessage
    {
        public ReadOnlyMemory<byte> LoopbackKeyNotify { get; set; }
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P15PasswordUpdate().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            LoopbackKeyNotify = buffer.ReadByteArray();
            KeyHash = buffer.ReadRawByteArray(32).ToArray();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteRawByteArray(KeyHash);
        }
    }
}
