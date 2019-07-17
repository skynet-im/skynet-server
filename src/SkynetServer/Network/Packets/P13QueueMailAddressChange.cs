using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x13, PacketPolicy.Receive)]
    [MsgFlags(MessageFlags.Loopback | MessageFlags.Unencrypted)]
    internal sealed class P13QueueMailAddressChange : P0BChannelMessage
    {
        public string NewMailAddress { get; set; }

        public override Packet Create() => new P13QueueMailAddressChange().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            NewMailAddress = buffer.ReadString();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
