using SkynetServer.Model;
using SkynetServer.Network.Attributes;
using SkynetServer.Network.Model;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Packets
{
    [Packet(0x18, PacketPolicies.Duplex)]
    [MessageFlags(MessageFlags.Unencrypted)]
    internal sealed class P18PublicKeys : ChannelMessage
    {
        public KeyFormat SignatureKeyFormat { get; set; }
        public byte[] SignatureKey { get; set; }
        public KeyFormat DerivationKeyFormat { get; set; }
        public byte[] DerivationKey { get; set; }

        public override Packet Create() => new P18PublicKeys().Init(this);

        protected override void ReadMessage(PacketBuffer buffer)
        {
            SignatureKeyFormat = (KeyFormat)buffer.ReadByte();
            SignatureKey = buffer.ReadByteArray().ToArray();
            DerivationKeyFormat = (KeyFormat)buffer.ReadByte();
            DerivationKey = buffer.ReadByteArray().ToArray();
        }

        protected override void WriteMessage(PacketBuffer buffer)
        {
            buffer.WriteByte((byte)SignatureKeyFormat);
            buffer.WriteByteArray(SignatureKey);
            buffer.WriteByte((byte)DerivationKeyFormat);
            buffer.WriteByteArray(DerivationKey);
        }
    }
}
