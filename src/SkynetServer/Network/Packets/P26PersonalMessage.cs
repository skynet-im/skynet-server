using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x26, PacketPolicy.Duplex)]
    internal sealed class P26PersonalMessage : P0BChannelMessage
    {
        public string PersonalMessage { get; set; }

        public override Packet Create() => new P26PersonalMessage().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            PersonalMessage = buffer.ReadString();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(PersonalMessage);
        }
    }
}
