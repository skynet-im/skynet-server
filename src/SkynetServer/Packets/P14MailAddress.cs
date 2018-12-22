using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    [Packet(0x14, PacketPolicy.Send)]
    internal sealed class P14MailAddress : ChannelMessage
    {
        public string MailAddress { get; set; }

        public override Packet Create() => new P14MailAddress().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteString(MailAddress);
        }
    }
}
