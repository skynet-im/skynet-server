using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x33, PacketPolicies.Send)]
    internal class P33DeviceListResponse : Packet
    {
        public List<SessionDetails> SessionDetails { get; set; } = new List<SessionDetails>();

        public override Packet Create() => new P33DeviceListResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteUInt16((ushort)SessionDetails.Count);
            foreach (SessionDetails details in SessionDetails)
            {
                buffer.WriteInt64(details.SessionId);
                buffer.WriteDateTime(details.LastConnected);
                buffer.WriteInt32(details.LastVersionCode);
            }
        }
    }
}
