using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x34, PacketPolicies.Receive)]
    internal class P34SetClientState : Packet
    {
        public OnlineState OnlineState { get; set; }
        public ChannelAction Action { get; set; }
        public long ChannelId { get; set; }

        public override Packet Create() => new P34SetClientState().Init(this);

        public override Task Handle(IPacketHandler handler) => handler.Handle(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            OnlineState = (OnlineState)buffer.ReadByte();
            Action = (ChannelAction)buffer.ReadByte();
            if (Action != ChannelAction.None)
                ChannelId = buffer.ReadInt64();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
