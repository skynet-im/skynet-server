using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network.Packets
{
    [Message(0x18, PacketPolicy.Duplex)]
    [MessageFlags(MessageFlags.Unencrypted)]
    internal sealed class P18PublicKeys : P0BChannelMessage
    {
        public KeyFormat SignatureKeyFormat { get; set; }
        public byte[] SignatureKey { get; set; }
        public KeyFormat DerivationKeyFormat { get; set; }
        public byte[] DerivationKey { get; set; }

        public override Packet Create() => new P18PublicKeys().Init(this);

        public override Task<MessageSendError> HandleMessage(IPacketHandler handler) => handler.Handle(this);

        public override Task PostHandling(IPacketHandler handler, Database.Entities.Message message) => handler.PostHandling(this, message);

        public override void ReadMessage(PacketBuffer buffer)
        {
            SignatureKeyFormat = (KeyFormat)buffer.ReadByte();
            SignatureKey = buffer.ReadByteArray();
            DerivationKeyFormat = (KeyFormat)buffer.ReadByte();
            DerivationKey = buffer.ReadByteArray();
        }

        public override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)SignatureKeyFormat);
            buffer.WriteByteArray(SignatureKey, true);
            buffer.WriteByte((byte)DerivationKeyFormat);
            buffer.WriteByteArray(DerivationKey, true);
        }
    }
}
