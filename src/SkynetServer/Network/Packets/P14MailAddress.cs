using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x14, PacketPolicies.Send)]
    internal sealed class P14MailAddress : ChannelMessage
    {
        public string MailAddress { get; set; }

        public override Packet Create() => new P14MailAddress().Init(this);

        public override Task<MessageSendStatus> HandleMessage(IPacketHandler handler) => throw new NotImplementedException();

        protected override void ReadMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteShortString(MailAddress);
        }
    }
}
