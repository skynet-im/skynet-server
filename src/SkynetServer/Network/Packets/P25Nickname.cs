using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x25, PacketPolicies.Duplex)]
    [AllowedFlags(MessageFlags.Unencrypted)]
    internal sealed class P25Nickname : P0BChannelMessage
    {
        public string Nickname { get; set; }

        public override Packet Create() => new P25Nickname().Init(this);

        //public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            Nickname = buffer.ReadString();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(Nickname);
        }
    }
}
