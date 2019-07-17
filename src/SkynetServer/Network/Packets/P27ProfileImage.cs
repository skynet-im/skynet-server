using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x27, PacketPolicy.Duplex)]
    [MinFlags(MessageFlags.FileAttached)]
    [MaxFlags(MessageFlags.Unencrypted | MessageFlags.FileAttached)]
    internal sealed class P27ProfileImage : P0BChannelMessage
    {
        public string Caption { get; set; }

        public override Packet Create() => new P27ProfileImage().Init(this);

        //public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            Caption = buffer.ReadString();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteString(Caption);
        }
    }
}
