using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class PacketAttribute : Attribute
    {
        public PacketAttribute(byte packedId, PacketPolicy packetPolicy)
        {
            PacketId = packedId;
            PacketPolicy = packetPolicy;
        }

        public byte PacketId { get; }
        public PacketPolicy PacketPolicy { get; }
    }
}
