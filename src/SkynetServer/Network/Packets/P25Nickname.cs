using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x25, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.Unencrypted)]
    internal sealed class P25Nickname : ChannelMessage
    {
        public string Nickname { get; set; }

        public override Packet Create() => new P25Nickname().Init(this);

        //public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            Nickname = buffer.ReadShortString();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteShortString(Nickname);
        }
    }
}
