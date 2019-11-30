using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x2C, PacketPolicies.Send)]
    internal class P2CChannelAction : Packet
    {
        public long ChannelId { get; set; }
        public long AccountId { get; set; }
        public ChannelAction Action { get; set; }

        public override Packet Create() => new P2CChannelAction().Init(this);

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteInt64(ChannelId);
            buffer.WriteInt64(AccountId);
            buffer.WriteByte((byte)Action);
        }
    }
}
