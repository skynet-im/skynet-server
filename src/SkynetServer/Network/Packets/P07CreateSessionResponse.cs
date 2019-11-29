using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Packets
{
    [Packet(0x07, PacketPolicies.Send)]
    internal sealed class P07CreateSessionResponse : Packet
    {
        public CreateSessionStatus StatusCode { get; set; }
        public long AccountId { get; set; }
        public long SessionId { get; set; }
        public byte[] SessionToken { get; set; }
        public string WebToken { get; set; }

        public override Packet Create() => new P07CreateSessionResponse().Init(this);

        public override void ReadPacket(PacketBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)StatusCode);
            buffer.WriteInt64(AccountId);
            buffer.WriteInt64(SessionId);
            buffer.WriteRawByteArray(SessionToken);
            buffer.WriteString(WebToken);
        }

        public override string ToString()
        {
            return $"{{{nameof(P07CreateSessionResponse)}: AccountId={AccountId:x8} SessionId={SessionId:x8} StatusCode={StatusCode}}}";
        }
    }
}
