using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x22, PacketPolicy.Duplex)]
    internal sealed class P22MessageReceived : P0BChannelMessage
    {
        public override Packet Create() => new P22MessageReceived().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
        }
    }
}
