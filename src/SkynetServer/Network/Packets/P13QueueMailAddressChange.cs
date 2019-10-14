using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x13, PacketPolicies.Receive)]
    [MessageFlags(MessageFlags.Loopback | MessageFlags.Unencrypted)]
    internal sealed class P13QueueMailAddressChange : ChannelMessage
    {
        public string NewMailAddress { get; set; }

        public override Packet Create() => new P13QueueMailAddressChange().Init(this);

        public override Task<MessageSendStatus> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            NewMailAddress = buffer.ReadShortString();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
