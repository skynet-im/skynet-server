using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x29, PacketPolicy.Send)]
    internal sealed class P29DeviceList : P0BChannelMessage
    {
        List<SessionInformation> Sessions { get; set; } = new List<SessionInformation>();

        public override Packet Create() => new P29DeviceList().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadMessage(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WriteMessage(PacketBuffer buffer)
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
