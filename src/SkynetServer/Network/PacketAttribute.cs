using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class PacketAttribute : Attribute
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
