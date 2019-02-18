using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x23, PacketPolicy.Duplex)]
    internal sealed class P23MessageRead : P0BChannelMessage
    {
        public override Packet Create() => new P23MessageRead().Init(this);

        //public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
        }
    }
}
