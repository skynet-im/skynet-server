using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

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
            buffer.WriteUShort((ushort)SessionDetails.Count);
            foreach (SessionDetails details in SessionDetails)
            {
                buffer.WriteLong(details.SessionId);
                buffer.WriteDate(details.LastConnected);
                buffer.WriteInt(details.LastVersionCode);
            }
        }
    }
}
