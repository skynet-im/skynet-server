using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x26, PacketPolicy.Duplex)]
    internal sealed class P26PersonalMessage : ChannelMessage
    {
        public string PersonalMessage { get; set; }

        public override Packet Create() => new P26PersonalMessage().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            PersonalMessage = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteString(PersonalMessage);
        }
    }
}
