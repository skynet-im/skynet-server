using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x25, PacketPolicy.Duplex)]
    internal sealed class P25Nickname : ChannelMessage
    {
        public string Nickname { get; set; }

        public override Packet Create() => new P25Nickname().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            Nickname = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteString(Nickname);
        }
    }
}
