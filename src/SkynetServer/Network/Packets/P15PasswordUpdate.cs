using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x15, PacketPolicy.Duplex)]
    internal sealed class P15PasswordUpdate : P0BChannelMessage
    {
        public byte[] OldKeyHash { get; set; }
        public byte[] KeyHash { get; set; }

        public override Packet Create() => new P15PasswordUpdate().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override void ReadMessage(PacketBuffer buffer)
        {
            OldKeyHash = buffer.ReadByteArray(32);
            KeyHash = buffer.ReadByteArray(32);
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByteArray(KeyHash, false);
        }
    }
}
