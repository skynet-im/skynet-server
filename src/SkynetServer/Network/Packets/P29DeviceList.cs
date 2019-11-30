using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x29, PacketPolicies.Send)]
    internal sealed class P29DeviceList : ChannelMessage
    {
        List<SessionInformation> Sessions { get; set; } = new List<SessionInformation>();

        public override Packet Create() => new P29DeviceList().Init(this);

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteUInt16((ushort)Sessions.Count);
            foreach (SessionInformation session in Sessions)
            {
                buffer.WriteInt64(session.SessionId);
                buffer.WriteDateTime(session.CreationTime);
                buffer.WriteShortString(session.ApplicationIdentifier);
            }
        }
    }
}
