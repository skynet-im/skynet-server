using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x14, PacketPolicy.Send)]
    internal sealed class P14MailAddress : P0BChannelMessage
    {
        public string MailAddress { get; set; }

        public override Packet Create() => new P14MailAddress().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(MailAddress);
        }
    }
}
