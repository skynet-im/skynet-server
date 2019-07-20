using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0A, PacketPolicy.Duplex)]
    internal sealed class P0ACreateChannel : Packet
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public long OwnerId { get; set; }
        public long CounterpartId { get; set; }

        public override Packet Create() => new P0ACreateChannel().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadLong();
            ChannelType = (ChannelType)buffer.ReadByte();
            OwnerId = buffer.ReadLong();
            if (ChannelType == ChannelType.Direct)
                CounterpartId = buffer.ReadLong();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(ChannelId);
            buffer.WriteByte((byte)ChannelType);
            buffer.WriteLong(OwnerId);
            if (ChannelType == ChannelType.Direct)
                buffer.WriteLong(CounterpartId);
        }

        public override string ToString()
        {
            return $"{{{nameof(P0ACreateChannel)}: ChannelId={ChannelId:x8} Type={ChannelType} Owner={OwnerId:x8} Counterpart={CounterpartId.ToString("x8")}}}";
        }
    }
}
