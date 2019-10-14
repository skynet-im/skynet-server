using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x22, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Unencrypted)]
    internal sealed class P22MessageReceived : ChannelMessage
    {
        public override Packet Create() => new P22MessageReceived().Init(this);

        //public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
        }
    }
}
