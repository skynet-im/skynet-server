using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0A, PacketPolicies.Duplex)]
    internal sealed class P0ACreateChannel : Packet
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public long OwnerId { get; set; }
        public DateTime CreationTime { get; set; }
        public long CounterpartId { get; set; }

        public override Packet Create() => new P0ACreateChannel().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadInt64();
            ChannelType = (ChannelType)buffer.ReadByte();
            if (ChannelType == ChannelType.Direct)
                CounterpartId = buffer.ReadInt64();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteInt64(ChannelId);
            buffer.WriteByte((byte)ChannelType);
            buffer.WriteInt64(OwnerId);
            buffer.WriteDateTime(CreationTime);
            if (ChannelType == ChannelType.Direct)
                buffer.WriteInt64(CounterpartId);
        }

        public override string ToString()
        {
            return $"{{{nameof(P0ACreateChannel)}: ChannelId={ChannelId:x8} Type={ChannelType} Owner={OwnerId:x8} Counterpart={CounterpartId.ToString("x8")}}}";
        }
    }
}
