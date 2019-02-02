using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x27, PacketPolicy.Duplex)]
    internal sealed class P27ProfileImage : ChannelMessage
    {
        public string Caption { get; set; }

        public override Packet Create() => new P27ProfileImage().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            Caption = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteString(Caption);
        }
    }
}
