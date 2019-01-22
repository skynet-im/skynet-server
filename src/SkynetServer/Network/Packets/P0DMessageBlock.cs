using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0D, PacketPolicy.Receive)]
    internal sealed class P0DMessageBlock : Packet
    {
        public List<byte[]> Messages { get; set; } = new List<byte[]>();

        public override Packet Create() => new P0DMessageBlock().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ushort length = buffer.ReadUShort();
            for (int i = 0; i < length; i++)
            {
                Messages.Add(buffer.ReadByteArray());
            }
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
