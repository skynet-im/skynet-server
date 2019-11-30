using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x0E, PacketPolicies.Receive)]
    internal sealed class P0ERequestMessages : Packet
    {
        public long ChannelId { get; set; }
        public long After { get; set; }
        public long Before { get; set; }
        public ushort MaxCount { get; set; }

        public override Packet Create() => new P0ERequestMessages().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            ChannelId = buffer.ReadInt64();
            After = buffer.ReadInt64();
            Before = buffer.ReadInt64();
            MaxCount = buffer.ReadUInt16();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
