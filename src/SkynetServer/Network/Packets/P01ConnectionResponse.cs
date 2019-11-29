using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x01, PacketPolicies.Send | PacketPolicies.Unauthenticated | PacketPolicies.Uninitialized)]
    internal sealed class P01ConnectionResponse : Packet
    {
        public ConnectionState ConnectionState { get; set; }
        public int LatestVersionCode { get; set; }
        public string LatestVersion { get; set; }

        public override Packet Create() => new P01ConnectionResponse().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)ConnectionState);

            if (ConnectionState != ConnectionState.Valid)
            {
                buffer.WriteInt32(LatestVersionCode);
                buffer.WriteShortString(LatestVersion);
            }
        }
    }
}
