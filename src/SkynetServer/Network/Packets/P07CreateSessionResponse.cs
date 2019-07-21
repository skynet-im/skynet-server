using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Packet(0x07, PacketPolicy.Send)]
    internal sealed class P07CreateSessionResponse : Packet
    {
        public long AccountId { get; set; }
        public long SessionId { get; set; }
        public CreateSessionError ErrorCode { get; set; }

        public override Packet Create() => new P07CreateSessionResponse().Init(this);

        public override Task Handle(IPacketHandler handler) => throw new NotImplementedException();

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteLong(AccountId);
            buffer.WriteLong(SessionId);
            buffer.WriteByte((byte)ErrorCode);
        }

        public override string ToString()
        {
            return $"{{{nameof(P07CreateSessionResponse)}: AccountId={AccountId:x8} SessionId={SessionId:x8} ErrorCode={ErrorCode}}}";
        }
    }
}
