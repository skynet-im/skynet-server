using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x15, PacketPolicy.Duplex)]
    internal sealed class P15PasswordUpdate : ChannelMessage
    {
        public byte[] OldKeyHash { get; set; }
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P15PasswordUpdate().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            OldKeyHash = buffer.ReadByteArray(32);
            KeyHash = buffer.ReadByteArray(32);
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByteArray(KeyHash, false);
        }
    }
}
