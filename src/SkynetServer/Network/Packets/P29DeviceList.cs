using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x29, PacketPolicy.Send)]
    internal sealed class P29DeviceList : ChannelMessage
    {
        List<SessionInformation> Sessions { get; set; } = new List<SessionInformation>();

        public override Packet Create() => new P29DeviceList().Init(this);

        public override void Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteUShort((ushort)Sessions.Count);
            foreach (SessionInformation session in Sessions)
            {
                buffer.WriteLong(session.SessionId);
                buffer.WriteDate(session.CreationTime);
                buffer.WriteString(session.ApplicationIdentifier);
            }
        }
    }
}
