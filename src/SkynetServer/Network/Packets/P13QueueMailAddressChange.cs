using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x13, PacketPolicy.Receive)]
    internal sealed class P13QueueMailAddressChange : ChannelMessage
    {
        public string NewMailAddress { get; set; }

        public override Packet Create() => new P13QueueMailAddressChange().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            NewMailAddress = buffer.ReadString();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
