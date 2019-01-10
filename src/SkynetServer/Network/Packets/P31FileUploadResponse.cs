using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x31, PacketPolicy.Send)]
    internal sealed class P31FileUploadResponse : Packet
    {
        public long FileId { get; set; }

        public override Packet Create() => new P31FileUploadResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer) => throw new NotImplementedException();

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(FileId);
        }
    }
}
